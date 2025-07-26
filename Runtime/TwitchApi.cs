using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
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

    public static void Init(string clientId,
        out string broadcasterName,
        out string broadcasterId)
    {
        Debugs.Log("Initializing TwitchApi");
        _clientId = clientId;
        SetBroadcaster();
        broadcasterName = _broadcasterName;
        broadcasterId = _broadcasterId;
    }


    private static void SetBroadcaster()
    {
        var result = GetUsers();
        (_broadcasterId, _broadcasterName) = ParseBroadcasterUser(result);
        Debugs.Log($"Setting Broadcaster id: {_broadcasterId}, name: {_broadcasterName}");
    }

    private static (string Id, string Name) ParseBroadcasterUser(string result)
    {
        var json = JObject.Parse(result);
        var metaData = json["data"]?[0];
        var broadcasterId = metaData?["id"]?.ToString();
        var broadcasterName = metaData?["display_name"]?.ToString();
        return (broadcasterId, broadcasterName);
    }

    public static (string Id, string Name) GetBroadcaster(string clientId, TokenResponse tokenResponse)
    {
        var result = GetUsers(tokenResponse, clientId);
        return ParseBroadcasterUser(result);
    }

    public static (string Id, string Name) GetBroadcaster()
    {
        return (Id: _broadcasterId, Name: _broadcasterName);
    }


    public static HttpResponseMessage SubscribeToEvents(object subscriptionData)
    {
        var uri = "https://api.twitch.tv/helix/eventsub/subscriptions";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);

        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);

        var jsonBody = JsonConvert.SerializeObject(subscriptionData);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        return HttpClient.SendAsync(request, ct).Result;
    }
//TODO search:
// channels : https://dev.twitch.tv/docs/api/reference/#search-channels
// streams : https://dev.twitch.tv/docs/api/reference/#get-streams
// teams : https://dev.twitch.tv/docs/api/reference/#get-teams
//TODO get category ID
// https://dev.twitch.tv/docs/api/reference/#search-categories

    public static void GetCategory(string categoryName = "")
    {
        /* example response
        {
            "data": [
                {
                    "id": "33214",
                    "name": "Fortnite",
                    "box_art_url": "https://static-cdn.jtvnw.net/ttv-boxart/33214-52x72.jpg"
                },
                ...
            ],
            "pagination": {
                "cursor": "eyJiIjpudWxsLCJhIjp7IkN"
            }
        }
        */


        if (string.IsNullOrEmpty(categoryName))
            categoryName = "Software%20and%20game%20development";
        var uri = $"https://api.twitch.tv/helix/search/categories?query={categoryName}";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        var tokenResponse = EventSubWebsocket.GetTokenResponse();

        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = HttpClient.SendAsync(request, ct).Result;

        if (response.IsSuccessStatusCode)
        {
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JsonUtility.FromJson<CategoryResponse>(json);
            foreach (var categoryData in result.data)
                Debugs.Log(categoryData);
        }
        else
        {
            Debugs.LogError("error: " + response.StatusCode);
        }
    }

    public static void SendChatMessage(string chatMessage, int attempt = 0)
    {
        //TODO to send msg's as bot, we need a token for bot...
        var uri = "https://api.twitch.tv/helix/chat/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        var tokenResponse = EventSubWebsocket.GetTokenResponse();

        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);

        var payload = new
        {
            broadcaster_id = _broadcasterId,
            sender_id = _broadcasterId,
            message = chatMessage
        };

        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = HttpClient.SendAsync(request, ct).Result;

        if (response.IsSuccessStatusCode)
        {
            Debugs.Log("Sent chat message: " + chatMessage);
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (attempt > 10)
                throw new AuthenticationException("failed to refresh token and resend message");
            Debugs.LogError("Failed to send message, attempting to refresh token");
            _ = EventSubWebsocket.instance.Handle401();
            Debug.LogWarning("Token refreshed, attempting to resend chat message");
            attempt += 1;
            SendChatMessage(chatMessage, attempt);
        }
        else
        {
            var error = response.Content.ReadAsStringAsync().Result;
            Debugs.LogError(error);
        }
    }

    public static string GetUsers(int[] ids)
    {
        var query = "";
        if (ids is { Length: > 0 })
            query = string.Join("&", ids[..Mathf.Min(ids.Length, 99)].Select(i => $"id={i}"));
        return GetUsers(query);
    }

    public static string GetUsers(string[] logins)
    {
        var query = "";
        if (logins is { Length: > 0 })
            query = string.Join("&", logins[..Mathf.Min(logins.Length, 99)].Select(l => $"login={l}"));
        return GetUsers(query);
    }

    private static string GetUsers(string query = "")
    {
        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        return GetUsers(tokenResponse, _clientId, query);
    }

    public static async Task<bool> ValidateToken(TokenResponse tokenResponse)
    {
        if (tokenResponse == null)
        {
            Debugs.LogWarning("Invalid token!");
            return false;
        }

        // Set Authorization header with Bearer token
        var uri = "https://id.twitch.tv/oauth2/validate";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        // Send the GET request
        using var client = new HttpClient();
        var cts = EventSubWebsocket.GetCancellationTokenSource();
        var response = await client.SendAsync(request, cts.Token);
        // If status code is 200, the token is valid
        var isValid = response.IsSuccessStatusCode;
        Debugs.Log($"Token is valid: {isValid}");
        return isValid;
    }

    private static string GetUsers(TokenResponse token, string clientId = "", string query = "")
    {
        var uri = "https://api.twitch.tv/helix/users";
        if (!string.IsNullOrEmpty(query)) uri += $"?{query}";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
        request.Headers.Add("Client-Id", clientId);

        using var client = new HttpClient();
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = client.SendAsync(request, ct).Result;
        return response.Content.ReadAsStringAsync().Result;
    }

    public class CategoryResponse
    {
        [JsonProperty("data")] public List<CategoryData> data { get; set; }

        [JsonProperty("pagination")] public Pagination pagination { get; set; }
    }

    public class Pagination
    {
        [JsonProperty("cursor")] public string cursor { get; set; }
    }

    public class CategoryData
    {
        [JsonProperty("box_art_url")] public string BoxArtURL;

        [JsonProperty("id")] public string ID;

        [JsonProperty("name")] public string Name;
    }
}