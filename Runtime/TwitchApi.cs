using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TwitchApi
{
    private readonly HttpClient _client = new();
    private readonly string _clientId;
    private readonly TokenResponse _tokenResponse;
    private string _broadcasterId;
    private string _broadcasterName;

    public TwitchApi(string clientId, TokenResponse tokenResponse)
    {
        _clientId = clientId;
        _tokenResponse = tokenResponse;
        SetBroadcaster();
    }

    private void SetBroadcaster()
    {
        var result = GetUsers();
        (_broadcasterId, _broadcasterName) = ParseBroadcasterUser(result);
        Debug.Log($"Setting Broadcaster id: {_broadcasterId}, name: {_broadcasterName}");
    }

    private static (string Id, string Name) ParseBroadcasterUser(string result)
    {
        var json = JObject.Parse(result);
        var metaData = json["data"]?[0];
        var broadcasterId = metaData?["id"]?.ToString();
        var broadcasterName = metaData?["display_name"]?.ToString();
        return (broadcasterId, broadcasterName);
    }

    public static (string Id, string Name) GetBroadcaster(string clientId,
        TokenResponse tokenResponse)
    {
        var result = GetUsers(tokenResponse, clientId);
        return ParseBroadcasterUser(result);
    }

    public (string Id, string Name) GetBroadcaster()
    {
        return (Id: _broadcasterId, Name: _broadcasterName);
    }


    public HttpResponseMessage SubscribeToEvents(object subscriptionData)
    {
        var uri = "https://api.twitch.tv/helix/eventsub/subscriptions";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("Authorization", $"Bearer {_tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);

        var jsonBody = JsonConvert.SerializeObject(subscriptionData);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        return _client.SendAsync(request).Result;
    }


    public void SendChatMessage(string chatMessage)
    {
        //TODO to send msg's as bot, we need a token for bot...
        var uri = "https://api.twitch.tv/helix/chat/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("Authorization", $"Bearer {_tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);

        var payload = new
        {
            broadcaster_id = _broadcasterId,
            sender_id = _broadcasterId,
            message = chatMessage
        };

        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = _client.SendAsync(request).Result;

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

    public string GetUsers(int[] ids)
    {
        var query = "";
        if (ids is { Length: > 0 })
            query = string.Join("&", ids[..99].Select(i => $"id={i}"));
        return GetUsers(query);
    }

    public string GetUsers(string[] logins)
    {
        var query = "";
        if (logins is { Length: > 0 })
            query = string.Join("&", logins[..99].Select(l => $"login={l}"));
        return GetUsers(query);
    }

    private string GetUsers(string query = "")
    {
        return GetUsers(_tokenResponse, _clientId, query);
    }

    public static bool ValidateToken(TokenResponse tokenResponse)
    {
        if (tokenResponse == null) return false;
        // Set Authorization header with Bearer token
        var uri = "https://id.twitch.tv/oauth2/validate";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        // Send the GET request
        using var client = new HttpClient();
        var response = client.SendAsync(request).Result;
        // If status code is 200, the token is valid
        var isValid = response.IsSuccessStatusCode;
        Debug.Log($"Token is valid: {isValid}");
        return isValid;
    }

    private static string GetUsers(TokenResponse token, string clientId = "", string query = "")
    {
        var uri = "https://api.twitch.tv/helix/users";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
        request.Headers.Add("Client-Id", clientId);

        if (!string.IsNullOrEmpty(query)) uri += $"?{query}";
        using var client = new HttpClient();
        var response = client.SendAsync(request).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
}