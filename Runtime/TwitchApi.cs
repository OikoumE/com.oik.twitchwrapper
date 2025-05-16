using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class TwitchApi
{
    private static readonly HttpClient HttpClient = new();
    private static string _clientId;
    private static string _broadcasterId;
    private static string _broadcasterName;
    private static bool _initialized;

    public static (string broadcasterId, string broadcasterName) Init(string clientId)
    {
        Debug.Log("Initializing TwitchApi");
        _clientId = clientId;
        _initialized = true;
        return SetBroadcaster(clientId);
    }

    private static (string broadcasterId, string broadcasterName) SetBroadcaster(string clientId)
    {
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        var result = GetUsers(tokenResponse, ct, clientId);
        (_broadcasterId, _broadcasterName) = ParseBroadcasterUser(result);
        Debug.Log($"Setting Broadcaster id: {_broadcasterId}, name: {_broadcasterName}");
        return (_broadcasterId, _broadcasterName);
    }

    private static (string Id, string Name) ParseBroadcasterUser(string result)
    {
        var json = JObject.Parse(result);
        var metaData = json["data"]?[0];
        var broadcasterId = metaData?["id"]?.ToString();
        var broadcasterName = metaData?["display_name"]?.ToString();
        return (broadcasterId, broadcasterName);
    }

    private static void ThrowIfNotInitialized()
    {
        if (!_initialized)
            throw new Exception("TwitchApi not initialized, call Init() first");
    }

    public static HttpResponseMessage SubscribeToEvents(object subscriptionData, string clientId)
    {
        var uri = "https://api.twitch.tv/helix/eventsub/subscriptions";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("Authorization", $"Bearer {EventSubWebsocket.GetTokenResponse()}");
        request.Headers.Add("Client-Id", clientId);

        var jsonBody = JsonConvert.SerializeObject(subscriptionData);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var ct = EventSubWebsocket.GetCancellationTokenSource();
        return HttpClient.SendAsync(request, ct.Token).Result;
    }


    public static void SendChatMessage(string chatMessage)
    {
        ThrowIfNotInitialized();
        //TODO to send msg's as bot, we need a token for bot...
        var uri = "https://api.twitch.tv/helix/chat/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("Authorization", $"Bearer {EventSubWebsocket.GetTokenResponse()}");
        request.Headers.Add("Client-Id", _clientId);

        var payload = new
        {
            broadcaster_id = _broadcasterId,
            sender_id = _broadcasterId,
            message = chatMessage
        };

        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var ct = EventSubWebsocket.GetCancellationTokenSource();
        var response = HttpClient.SendAsync(request, ct.Token).Result;
        if (response.IsSuccessStatusCode)
        {
            Debug.Log("Sent chat message: " + chatMessage);
        }
        // handle 401 // and update token everywhere
        else if (!EventSubWebsocket.TryHandle401(response).Result)
        {
            var error = response.Content.ReadAsStringAsync().Result;
            Debug.LogError("Failed to send chat message");
            Debug.LogError(error);
        }
    }

    public static string GetUsers(CancellationToken ct, int[] ids)
    {
        var query = "";
        if (ids is { Length: > 0 })
            query = string.Join("&", ids[..99].Select(i => $"id={i}"));
        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        return GetUsers(tokenResponse, ct, query);
    }

    public static string GetUsers(CancellationToken ct, string[] logins)
    {
        var query = "";
        if (logins is { Length: > 0 })
            query = string.Join("&", logins[..99].Select(l => $"login={l}"));
        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        return GetUsers(tokenResponse, ct, query);
    }

    private static string GetUsers(TokenResponse token, CancellationToken ct, string clientId = "", string query = "")
    {
        var uri = "https://api.twitch.tv/helix/users";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
        request.Headers.Add("Client-Id", clientId);

        if (!string.IsNullOrEmpty(query)) uri += $"?{query}";
        using var client = new HttpClient();
        var response = client.SendAsync(request, ct).Result;
        // handle 401 // and update token everywhere
        if (!EventSubWebsocket.TryHandle401(response).Result)
            Debug.LogError("Error getting users");
        return response.Content.ReadAsStringAsync().Result;
    }

    public static async Task<bool> ValidateToken(TokenResponse tokenResponse, CancellationToken ct)
    {
        if (tokenResponse == null)
        {
            Debug.LogWarning("Invalid token!");
            return false;
        }

        // Set Authorization header with Bearer token
        var uri = "https://id.twitch.tv/oauth2/validate";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        // Send the GET request
        using var client = new HttpClient();
        var response = await client.SendAsync(request, ct);
        // If status code is 200, the token is valid
        var isValid = response.IsSuccessStatusCode;
        Debug.Log($"Token is valid: {isValid}");
        return isValid;
    }
}