using System;
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
    private readonly TwitchAuthenticator _authenticator;
    private readonly string _clientId;
    private readonly Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> _eventHandlers;
    private readonly int _keepAlive;
    private readonly StringCollection _messageIds = new();
    private string _broadcasterId;

    private string _broadcasterName;
    private CancellationTokenSource _cts;

    private bool _isConnecting;
    private string _sessionId;
    private float _timeOfLastKeepAlive;
    private TokenResponse _tokenResponse;

    private Dictionary<int, string> _websocketCloseStatusCode = new()
    {
        { 4000, "Internal server error" },
        { 4001, "Client sent inbound traffic" },
        { 4002, "Client failed ping-pong" },
        { 4003, "Connection unused" },
        { 4004, "Reconnect grace time expired" },
        { 4005, "Network timeout" },
        { 4006, "Network error" },
        { 4007, "Invalid reconnect" }
    };

    private ClientWebSocket _ws;

    public TwitchApi Api;
    public TwitchChatHandler ChatHandler;

    public Action OnClose;
    public Action<bool, string> OnConnected;

    public EventSubWebsocket(string clientId,
        Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> eventHandlers,
        Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> chatCommands = null,
        int keepAlive = 30)
    {
        _clientId = clientId;
        _eventHandlers = eventHandlers;
        _keepAlive = keepAlive;

        if (chatCommands != null || eventHandlers.ContainsKey(TwitchEventSubScopes.EScope.ChannelChatMessage))
            ChatHandler = new TwitchChatHandler(this, chatCommands);

        var apiScopes = TwitchEventSubScopes.GetUrlScopes(_eventHandlers.Keys.ToArray());
        _authenticator = new TwitchAuthenticator(clientId, apiScopes);
    }

    public void RevokeToken()
    {
        _authenticator.RevokeToken(_tokenResponse);
    }

    public void Close()
    {
        if (_cts?.IsCancellationRequested ?? true) return;
        Api?.SendChatMessage("Disconnecting from Websocket!");
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

        Debug.Log($"Websocket closed {_ws?.State}");
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

    public async Task Connect()
    {
        if (_isConnecting)
        {
            Debug.LogError("Already connecting");
            return;
        }

        _isConnecting = true;
        Debug.Log("Getting User DeviceToken");
        _tokenResponse = await _authenticator.RunDeviceFlowAsync();

        Api = new TwitchApi(_clientId, _tokenResponse);
        (_broadcasterId, _broadcasterName) = Api.GetBroadcaster();
        Debug.Log($"Using BroadcasterId {_broadcasterId}");

        var uri = CreateUri(_keepAlive);
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _ws?.Dispose();
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(uri, _cts.Token);
        var connected = _ws.State == WebSocketState.Open;
        var status = "<color=green>Connected</color>";
        if (!connected) status = "<color=red>Failed to Connect</color>";
        Debug.Log($"{status} to WebSocket");

        _isConnecting = false;

        await Task.WhenAny(HandleMessageAsync(), Task.Delay(-1, _cts.Token));

        try
        {
            Api?.SendChatMessage("Connected to WebSocket!");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }


    private async Task HandleMessageAsync()
    {
        Debug.Log("<color=green>Listening</color> to messages");
        var buffer = new byte[4096];
        try
        {
            while (_ws.State == WebSocketState.Open && _cts.Token.IsCancellationRequested == false)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var json = JObject.Parse(msg);
                HandleIncomingMessage(json);
            }
        }
        catch (OperationCanceledException)
        {
            /* expected on cancel */
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }

        Debug.LogWarning($"Socket status: {_ws.State}");
        OnClose?.Invoke();
    }

    private void HandleIncomingMessage(JObject json)
    {
        var metaData = json["metadata"];
        var messageId = metaData?["message_id"]?.ToString();
        if (_messageIds.Contains(messageId))
        {
            Debug.LogError("<color=red>Message id is already in use</color>, discarding message");
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
        }
    }

    private void HandleEvent(JObject json)
    {
        // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#notification-message
        var payload = json["payload"];
        var eventType = payload?["subscription"]?["type"]?.ToString();
        // Debug.Log($"<color=yellow>New event</color>: {eventType}");
        var eEventType = TwitchEventSubScopes.GetScope(eventType);
        if (!_eventHandlers.TryGetValue(eEventType, out var handler))
            return;
        handler?.Invoke(payload as JObject ?? throw new InvalidOperationException());
    }


    private void HandleKeepAlive(JObject json)
    {
        // Debug.Log($"<color=green>Keep alive</color>, time since last: {Time.time - _timeOfLastKeepAlive}");
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

    private async Task SubscribeEvent(TwitchEventSubScopes.EScope scope)
    {
        var subscriptionData = GetSubscriptionCondition(scope);
        var response = Api.SubscribeToEvents(subscriptionData);
        var isSuccess = response.IsSuccessStatusCode;
        Debug.Log($"<color=#00FFFF>Subscribing</color> to event: {scope}," +
                  $" OK:<color={(isSuccess ? "green" : "red")}>{isSuccess}</color>," +
                  $" status: {(int)response.StatusCode}-{response.StatusCode}");
        if (response.StatusCode == (HttpStatusCode)401)
        {
            // handle expired token
            Debug.LogError($"Expired token, refreshing token and retrying to subscribe to scope {scope}");
            _tokenResponse = await _authenticator.Handle401(_clientId, _tokenResponse);
            if (_tokenResponse == null)
                throw new Exception("Failed to refresh token");
            Debug.Log($"Refreshed token, retrying subscribing to scope {scope}");
            await SubscribeEvent(scope);
        }
        else if (!isSuccess)
        {
            // Handle error
            var responseBody = response.Content.ReadAsStringAsync().Result;
            Debug.LogError($"Error when subscribing:{response.StatusCode}, {responseBody}");
        }
    }

    private object GetSubscriptionCondition(TwitchEventSubScopes.EScope scope)
    {
        var eventSubScope = TwitchEventSubScopes.GetApiVersion(scope, _broadcasterId, out var condition);
        var subscriptionData = new
        {
            type = eventSubScope.ApiName,
            version = eventSubScope.Version,
            condition,
            transport = new
            {
                method = "websocket",
                session_id = _sessionId
            }
        };
        return subscriptionData;
    }
}