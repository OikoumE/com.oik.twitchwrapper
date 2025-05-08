using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
    private readonly string _botId;
    private readonly string _broadcasterId;
    private readonly string _clientId;
    private readonly CancellationTokenSource _cts = new();

    private readonly Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> _eventHandlers;
    private readonly int _keepAlive;
    private readonly StringCollection _messageIds = new();

    private readonly Action<bool> _onConnected;

    private readonly ClientWebSocket _ws = new();

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

    public EventSubWebsocket(int broadcasterId, string clientId,
        Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> eventHandlers,
        string botId, Action<bool> onConnected = null, int keepAlive = 30)
        : this(broadcasterId.ToString(),
            clientId, eventHandlers, botId, onConnected, keepAlive)
    {
    }

    public EventSubWebsocket(string broadcasterId,
        string clientId,
        Dictionary<TwitchEventSubScopes.EScope, Action<JObject>> eventHandlers,
        string botId, Action<bool> onConnected = null, int keepAlive = 30)
    {
        if (!int.TryParse(broadcasterId, out _))
        {
            // get twitch ID from name 
            Debug.Log($"BroadcasterId {broadcasterId} is not an integer, trying to fetch client id");
            var client = new HttpClient();
            broadcasterId = client.GetStringAsync("https://decapi.me/twitch/id/" + broadcasterId).Result;
        }

        Debug.Log($"Using BroadcasterId {broadcasterId}");

        _clientId = clientId;
        _eventHandlers = eventHandlers;
        _botId = botId;
        _onConnected = onConnected;
        _broadcasterId = broadcasterId;
        _keepAlive = keepAlive;
        var apiScopes = TwitchEventSubScopes.GetUrlScopes(_eventHandlers.Keys.ToArray());
        _authenticator = new TwitchAuthenticator(clientId, apiScopes);
    }

    public void RevokeToken()
    {
        _authenticator.RevokeToken(_tokenResponse);
    }

    public void Close()
    {
        _cts.Cancel();
        if (_ws.State != WebSocketState.Open) return;
        try
        {
            _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
        }
        catch (Exception)
        {
            /* swallow if socket is already closing/closed */
        }
    }

    public void SendChatMessage(string chatMessage)
    {
        using var client = new HttpClient();
        //TODO to send msg's as bot, we need a token for bot...
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _tokenResponse.AccessToken);
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        var payload = new
        {
            broadcaster_id = _broadcasterId,
            sender_id = _broadcasterId,
            message = chatMessage
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = client.PostAsync("https://api.twitch.tv/helix/chat/messages", content).Result;

        if (response.IsSuccessStatusCode)
        {
            Debug.Log("Sent chat message: " + chatMessage);
        }
        else
        {
            var error = response.Content.ReadAsStringAsync().Result;
            Debug.LogError("Failed to send chat message");
            Debug.LogError(error);
        }
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

    public void Connect()
    {
        Debug.Log("Getting User DeviceToken");
        _tokenResponse = _authenticator.RunDeviceFlowAsync();
        // Debug.Log($"\nDANGEROUS DEBUG: User DeviceToken {_tokenResponse?.AccessToken}");
        var uri = CreateUri(_keepAlive);
        _ws.ConnectAsync(uri, CancellationToken.None).Wait();
        var connected = _ws.State == WebSocketState.Open;
        var status = "<color=green>Connected</color>";
        if (!connected) status = "<color=red>Failed to Connect</color>";
        Debug.Log($"{status} to WebSocket");
        Task.WhenAny(HandleMessageAsync(), Task.Delay(-1, _cts.Token));

        try
        {
            _onConnected?.Invoke(connected);
            SendChatMessage("Connected to WebSocket!");
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
            while (_ws.State == WebSocketState.Open)
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
        foreach (var eventHandlersKey in _eventHandlers.Keys)
            SubscribeToEvents(eventHandlersKey);
    }

    private object GetSubscriptionCondition(TwitchEventSubScopes.EScope scope)
    {
        var eventSubScope = TwitchEventSubScopes.GetApiVersion(scope);
        var subscriptionData = new
        {
            type = eventSubScope.ApiName,
            version = eventSubScope.Version,

            condition = GetScopeCondition(scope),
            transport = new
            {
                method = "websocket",
                session_id = _sessionId
            }
        };
        return subscriptionData;

        object GetScopeCondition(TwitchEventSubScopes.EScope s)
        {
            switch (s)
            {
                case TwitchEventSubScopes.EScope.ChannelRaid:
                    return new
                    {
                        to_broadcaster_user_id = _broadcasterId
                        //from_broadcaster_user_id = _broadcasterId,
                    };
                case TwitchEventSubScopes.EScope.ChannelUnbanRequestCreate:
                case TwitchEventSubScopes.EScope.ChannelUnbanRequestResolve:
                case TwitchEventSubScopes.EScope.ChannelGuestStarGuestUpdate:
                case TwitchEventSubScopes.EScope.ChannelGuestStarSessionBegin:
                case TwitchEventSubScopes.EScope.ChannelGuestStarSessionEnd:
                case TwitchEventSubScopes.EScope.ChannelGuestStarSettingsUpdate:
                case TwitchEventSubScopes.EScope.ChannelFollow:
                    return new
                    {
                        broadcaster_user_id = _broadcasterId,
                        moderator_user_id = _broadcasterId
                    };
                case TwitchEventSubScopes.EScope.ChannelPointsCustomRewardAdd:
                case TwitchEventSubScopes.EScope.ChannelPointsCustomRewardRedemptionAdd:
                case TwitchEventSubScopes.EScope.ChannelPointsCustomRewardRedemptionUpdate:
                case TwitchEventSubScopes.EScope.ChannelPointsCustomRewardRemove:
                case TwitchEventSubScopes.EScope.ChannelPointsCustomRewardUpdate:
                    return new
                    {
                        broadcaster_user_id = _broadcasterId
                        //optionally reward_id
                    };
                default:
                    return new
                    {
                        //TODO moderator - conditions
                        broadcaster_user_id = _broadcasterId,
                        user_id = _broadcasterId
                    };
            }
        }
    }

    private void SubscribeToEvents(TwitchEventSubScopes.EScope scope)
    {
        using var client = new HttpClient();

        var subscriptionData = GetSubscriptionCondition(scope);
        var jsonBody = JsonConvert.SerializeObject(subscriptionData);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_tokenResponse.AccessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        var uri = "https://api.twitch.tv/helix/eventsub/subscriptions";
        var response = client.PostAsync(uri, content).Result;
        var msg = response.Content.ReadAsStringAsync().Result;
        var isSuccess = response.IsSuccessStatusCode;
        Debug.Log($"<color=#00FFFF>Subscribing</color> to event: {scope}," +
                  $" OK:<color={(isSuccess ? "green" : "red")}>{isSuccess}</color>," +
                  $" status: {(int)response.StatusCode}-{response.StatusCode}");
        if (response.StatusCode == (HttpStatusCode)401)
        {
            // handle expired token
            Debug.LogError($"Expired token, refreshing token and retrying to subscribe to scope {scope}");
            _tokenResponse = _authenticator.Handle401(_clientId, _tokenResponse);
            if (_tokenResponse == null)
                throw new Exception("Failed to refresh token");
            Debug.Log($"Refreshed token, retrying subscribing to scope {scope}");
            SubscribeToEvents(scope);
        }
        else if (!isSuccess)
        {
            // Handle error
            var responseBody = response.Content.ReadAsStringAsync().Result;
            Debug.LogError($"Error when subscribing:{response.StatusCode}, {responseBody}");
        }
    }
}