﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

// validate token:
// https://dev.twitch.tv/docs/authentication/validate-tokens/
// check if need to refresh token.

//TODO 
// PingPong:
// when ping, do pong, independent of notif and keepalive
// notif == subscribed event
// https://dev.twitch.tv/docs/eventsub/websocket-reference/#notification-message

//* old connection gets 4004 if dont disconnect old within timeframe

// messages. track message_id to avoid dupes
// messages. dont accept message_timestamp older than 600s and unique message_id
public class EventSubWebsocket
{
    private static TokenResponse _tokenResponse;
    private static CancellationTokenSource _cts;
    private static string _broadcasterId;
    private static string _broadcasterName;
    private readonly TwitchAuthenticator _authenticator;
    private readonly string _clientId;
    private readonly Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> _eventHandlers;
    private readonly int _keepAlive;
    private readonly StringCollection _messageIds = new();

    private bool _isConnecting;
    private string _sessionId;
    private float _timeOfLastKeepAlive;

    private float _timeoutSeconds;

    private ClientWebSocket _ws;

    public TwitchChatHandler ChatHandler;
    public Action OnClose;
    public Action<bool, string> OnConnected;

    //OLD
    public EventSubWebsocket(string clientId,
        Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> eventHandlers,
        Dictionary<CommandString, Action<ChatCommand>> chatCommands = null,
        string[] ignoreChatCommandFrom = null,
        int keepAlive = 30)
    {
        Debugs.Log("Initializing EventSubWebsocket");
        _clientId = clientId;
        _eventHandlers = eventHandlers;
        _keepAlive = keepAlive;
        var apiScopes = TwitchEventSubScopes.GetUrlScopes(_eventHandlers.Keys.ToArray());

        _authenticator = new TwitchAuthenticator(clientId, apiScopes);
        SetupChatHandler(chatCommands, ignoreChatCommandFrom);
        instance = this;
    }

    //NEW
    public EventSubWebsocket(string clientId,
        Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> eventHandlers,
        Dictionary<CommandString, Action<ChatCommand>> chatCommands = null,
        string[] ignoreChatCommandFrom = null,
        TwitchApiScopes.EScope[] extraScopes = null,
        int keepAlive = 30)
    {
        Debugs.Log("Initializing EventSubWebsocket");
        _clientId = clientId;
        _eventHandlers = eventHandlers;
        _keepAlive = keepAlive;
        var apiScopes = TwitchEventSubScopes.GetUrlScopes(_eventHandlers.Keys.ToArray());
        apiScopes = apiScopes.Concat(TwitchApiScopes.GetUrlScopes(extraScopes)).ToArray();
        //TODO add extra scopes
        _authenticator = new TwitchAuthenticator(clientId, apiScopes);
        SetupChatHandler(chatCommands, ignoreChatCommandFrom);
        instance = this;
    }

    public static EventSubWebsocket instance { get; private set; }

    public static CancellationTokenSource GetCancellationTokenSource()
    {
        return _cts ??= new CancellationTokenSource();
    }

    public static TokenResponse GetTokenResponse()
    {
        return _tokenResponse;
    }

    private void SetupChatHandler(
        Dictionary<CommandString, Action<ChatCommand>> chatCommands,
        string[] ignoreChatCommandFrom)
    {
        ChatHandler = new TwitchChatHandler(this, chatCommands, ignoreChatCommandFrom);
        var chatScope = TwitchEventSubScopes.EScope.ChannelChatMessage;
        if (!_eventHandlers.TryGetValue(chatScope, out var handler))
        {
            _eventHandlers.Add(chatScope, ChatHandler.OnChatMessage);
            return;
        }

        handler += ChatHandler.OnChatMessage;
        _eventHandlers[chatScope] = handler;
    }

    public void RevokeToken()
    {
        _authenticator.RevokeToken(_tokenResponse, _cts.Token);
    }

    public void Close()
    {
        if (_cts?.IsCancellationRequested ?? true) return;
#if !UNITY_EDITOR
        if (_ws?.State==WebSocketState.Open)
            TwitchApi.SendChatMessage("Disconnecting from Websocket!");
#endif
        _cts.Cancel();
        OnClose?.Invoke();
        try
        {
            _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token).Wait();
        }
        catch (Exception)
        {
            /* swallow if socket is already closing/closed */
        }

        OnClose = null;
        OnConnected = null;

        Debugs.Log($"Websocket closed {_ws?.State}");
    }

    private Uri CreateUri(int keepAlive)
    {
        keepAlive = keepAlive switch
        {
            //keepalive range 10-600
            < 10 => 10,
            > 600 => 600,
            _ => keepAlive
        };
        return new Uri($"wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds={keepAlive}");
    }

    public void Connect(float timeoutSeconds)
    {
        _ = ConnectAsync(timeoutSeconds);
    }

    public async Task ConnectAsync(float timeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
        if (_isConnecting)
        {
            Debugs.LogError("Already connecting");
            return;
        }

        Debugs.Log("Connecting EventSubWebsocket");

        Debugs.Log("Getting User DeviceToken");
        _tokenResponse = await _authenticator.RunDeviceFlowAsync(_timeoutSeconds);
        if (_tokenResponse == null)
            throw new Exception("Error when Authorizing");

        // dispose and create ws/cts
        (_ws, _cts) = PrepareConnection();
        //! twitch API require cts (PrepareConnection();)
        TwitchApi.Init(_clientId, out _broadcasterName, out _broadcasterId);

        var uri = CreateUri(_keepAlive);
        await ConnectAndHandleMessages(uri);
    }

    private async Task ConnectAndHandleMessages(Uri uri)
    {
        _isConnecting = true;
        await _ws.ConnectAsync(uri, _cts.Token);
        var connected = _ws.State == WebSocketState.Open;
        var status = "<color=green>Connected</color>";
        if (!connected) status = "<color=red>Failed to Connect</color>";
        Debugs.Log($"WebSocket Status: {status}");
        SendSuccessToChat();
        await Task.WhenAny(HandleMessageAsync(), Task.Delay(-1, _cts.Token));
        _isConnecting = false;
    }

    private void SendSuccessToChat()
    {
#if !UNITY_EDITOR
        try
        {
            TwitchApi.SendChatMessage("Connected to WebSocket!");
         //   TwitchApi.SendChatAnnouncement("Connected to WebSocket!");
        }
        catch (Exception e)
        {
            Debugs.LogError(e);
            throw;
        }
#endif
    }

    private (ClientWebSocket ws, CancellationTokenSource cts) PrepareConnection()
    {
        _cts?.Dispose();
        var cts = new CancellationTokenSource();
        _ws?.Dispose();
        var ws = new ClientWebSocket();
        return (ws, cts);
    }

    private async Task ReconnectAsync(string newUri)
    {
        Debugs.LogWarning("Attempting reconnect!");
        try
        {
            if (_ws != null)
            {
                Debugs.Log("Websocket was not null");
                if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    Debugs.Log($"Websocket state: {_ws.State} - closing for reconnect");
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", _cts.Token);
                }

                _cts.Cancel();
                _ws.Dispose();
            }

            (_ws, _cts) = PrepareConnection();
            Debugs.Log("Websocket & CancellationTokenSource Reinitialized");
            await ConnectAndHandleMessages(new Uri(newUri));

            // await _ws.ConnectAsync(new Uri(newUri), _cts.Token);
            // await Task.WhenAny(HandleMessageAsync(), Task.Delay(-1, _cts.Token));
        }
        catch (Exception e)
        {
            Debugs.LogError("Reconnect failed: " + e);
            throw;
        }

        Debugs.Log("Websocket Reconnected");
    }

    private async Task HandleMessageAsync()
    {
        Debugs.Log("<color=green>Listening</color> to messages");
        var buffer = new byte[4096];
        while (_ws.State == WebSocketState.Open && _cts.Token.IsCancellationRequested == false)
            try
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleIncomingMessage(msg);
            }
            catch (OperationCanceledException e)
            {
                Debugs.LogError($"Websocket was cancelled - {e.Message}");
                /* expected on cancel */
            }
            catch (Exception ex)
            {
                Debugs.LogError(ex);
            }


        Debugs.LogWarning($"Socket status: {_ws.State}");
        // if 4007 received, do full reconnect
        if (_ws.CloseStatus == (WebSocketCloseStatus)4007)
        {
            Debugs.LogError("Reconnect failed (4007), starting new session");
            await ConnectAsync(_timeoutSeconds); // full reconnect: new token/session/subs
            return;
        }

        if (_ws.State == WebSocketState.CloseReceived)
            Debugs.LogError($"closeStatus {_ws.CloseStatus} - {_ws.CloseStatusDescription}");
        /*
         4000, "Internal server error"
         4001, "Client sent inbound traffic"
         4002, "Client failed ping-pong"
         4003, "Connection unused"
         4004, "Reconnect grace time expired"
         4005, "Network timeout"
         4006, "Network error"
         4007, "Invalid reconnect"
        */
        OnClose?.Invoke();
    }

    private void HandleIncomingMessage(string msg)
    {
        JObject json;
        try
        {
            if (string.IsNullOrEmpty(msg))
            {
                Debugs.LogError("empty msg received. why? idfk! TODO: reconnect event?");
                return;
            }

            json = JObject.Parse(msg);
        }
        catch (Exception e)
        {
            Debugs.LogError($"EventSubWebSocket : Error when parsing json; original message: {msg}");
            Console.WriteLine(e);
            throw;
        }

        var metaData = json["metadata"];
        var messageId = metaData?["message_id"]?.ToString();
        if (_messageIds.Contains(messageId))
        {
            Debugs.LogError("<color=red>Message id is already in use</color>, discarding message");
            return;
        }

        _messageIds.Add(messageId);
        var type = metaData?["message_type"]?.ToString();
        switch (type)
        {
            case "session_welcome":
                HandleWelcome(json);
                break;
            case "session_keepalive":
                HandleKeepAlive(json);
                break;
            case "notification":
                HandleEvent(json);
                break;
            case "session_reconnect":
                HandleReconnect(json);
                break;
            default:
                Debugs.LogWarning($"Unhandled event: {type}");
                break;
        }
    }

    private void HandleReconnect(JObject json)
    {
        //https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#reconnect-message
        //TODO reconnect
        Debugs.LogWarning("RECONNECT message received");
        var newUri = json?["payload"]?["session"]?["reconnect_url"]?.ToString();
        _ = ReconnectAsync(newUri);
    }

    private void HandleEvent(JObject json)
    {
        // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#notification-message
        var payload = json["payload"];
        var eventType = payload?["subscription"]?["type"]?.ToString();
        // Debugs.Log($"<color=yellow>New event</color>: {eventType}");
        var eEventType = TwitchEventSubScopes.GetScope(eventType);
        if (!_eventHandlers.TryGetValue(eEventType, out var handler))
            return;
        handler?.Invoke(payload as JObject ?? throw new InvalidOperationException());
    }


    private void HandleKeepAlive(JObject json)
    {
        // Debugs.Log($"<color=green>Keep alive</color>, time since last: {Time.time - _timeOfLastKeepAlive}");
        _timeOfLastKeepAlive = Time.time;
        // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#keepalive-message
        //TODO 
        // keepalive:
        // keepalive_timeout_seconds from welcome
        // no event/notification within keepalive_timeout, reconnect
        // session_keepalive
    }

    private void HandleWelcome(JObject json)
    {
        //https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#welcome-message
        var payload = json["payload"];
        var sessionId = payload?["session"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(sessionId))
            throw new NullReferenceException("session id is null");
        _sessionId = $"{sessionId}";
        //! 10 sec limit to respond
        foreach (var scope in _eventHandlers.Keys)
            _ = SubscribeEvent(scope);
        OnConnected?.Invoke(true, _broadcasterName);
    }

    public async Task Handle401()
    {
        Debugs.LogError("Expired token, refreshing token");
        _tokenResponse = await _authenticator.Handle401(_timeoutSeconds, _clientId, _tokenResponse);
        if (_tokenResponse == null)
            throw new Exception("Failed to refresh token");
        Debugs.Log($"Refreshed token expires:{_tokenResponse.ExpiresIn}");
    }


    private async Task SubscribeEvent(TwitchEventSubScopes.EScope scope)
    {
        var subscriptionData = GetSubscriptionCondition(scope);
        var response = TwitchApi.SubscribeToEvents(subscriptionData);
        var isSuccess = response.IsSuccessStatusCode;
        Debugs.Log($"<color=#00FFFF>Subscribing</color> to event: {scope}," +
                   $" OK:<color={(isSuccess ? "green" : "red")}>{isSuccess}</color>," +
                   $" status: {(int)response.StatusCode}-{response.StatusCode}");
        if (response.StatusCode == (HttpStatusCode)401)
        {
            // handle expired token
            Debugs.LogError($"Expired token, refreshing token and retrying to subscribe to scope {scope}");
            // _tokenResponse = await _authenticator.Handle401(_timeoutSeconds, _clientId, _tokenResponse);
            // if (_tokenResponse == null)
            //     throw new Exception("Failed to refresh token");
            await Handle401();
            Debugs.Log($"Refreshed token, retrying subscribing to scope {scope}");
            await SubscribeEvent(scope);
        }
        else if (!isSuccess)
        {
            // Handle error
            var responseBody = response.Content.ReadAsStringAsync().Result;
            Debugs.LogError($"Error when subscribing:{response.StatusCode}, {responseBody}");
        }
    }

    public static (string broadcasterName, string broadcasterId) GetBroadcaster()
    {
        return (_broadcasterName, _broadcasterId);
    }

    private object GetSubscriptionCondition(TwitchEventSubScopes.EScope scope)
    {
        var eventSubScope = TwitchEventSubScopes.GetApiVersion(scope, _broadcasterId, out var subCondition);
        var subscriptionData = new
        {
            type = eventSubScope.ApiName,
            version = eventSubScope.Version,
            condition = subCondition,
            transport = new
            {
                method = "websocket",
                session_id = _sessionId
            }
        };
        return subscriptionData;
    }
}