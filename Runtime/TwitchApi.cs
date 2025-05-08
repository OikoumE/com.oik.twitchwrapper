using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TwitchApi
{
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
        using var client = new HttpClient();
        var jsonBody = JsonConvert.SerializeObject(subscriptionData);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_tokenResponse.AccessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        var uri = "https://api.twitch.tv/helix/eventsub/subscriptions";
        var response = client.PostAsync(uri, content).Result;
        return response;
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

    private static string GetUsers(TokenResponse token, string clientId = "", string query = "")
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.Add("Client-Id", clientId);
        var url = "https://api.twitch.tv/helix/users";
        if (!string.IsNullOrEmpty(query)) url += $"?{query}";
        var response = client.GetAsync(url).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
}