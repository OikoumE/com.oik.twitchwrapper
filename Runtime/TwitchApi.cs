using System;
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

    #region RAID

    public static void ExecuteRaid(string targetId)
    {
        //https://dev.twitch.tv/docs/api/raids/#to-raid-another-broadcaster
        var (_, broadcasterId) = EventSubWebsocket.GetBroadcaster();
        var uri = $"https://api.twitch.tv/helix/raids?from_broadcaster_id={broadcasterId}&to_broadcaster_id={targetId}";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);


        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var result = HttpClient.SendAsync(request, ct).Result;
        if (result.IsSuccessStatusCode)
            return;
        Debugs.LogError(result.StatusCode);
    }

    #endregion

    private static HttpRequestMessage CreateDefaultRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);

        var tokenResponse = EventSubWebsocket.GetTokenResponse();
        request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        request.Headers.Add("Client-Id", _clientId);
        return request;
    }

    #region WEBSOCKET

    public static HttpResponseMessage SubscribeToEvents(SubscriptionData subscriptionData)
    {
        var uri = "https://api.twitch.tv/helix/eventsub/subscriptions";
        var request = CreateDefaultRequest(HttpMethod.Post, uri);
        var jsonBody = JsonConvert.SerializeObject(subscriptionData);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var result = HttpClient.SendAsync(request, ct).Result;
        if (result.IsSuccessStatusCode)
            return result;
        Debugs.LogError(
            $"Failed to subscribe to event {subscriptionData.type}, statuc: {result.StatusCode}, reason: {result.ReasonPhrase}");
        Debug.LogWarning("Ensure you have correct condition for event");
        throw new HttpRequestException("Failed to subscribe to event");
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
        var cts = EventSubWebsocket.GetCancellationTokenSource();
        var response = await HttpClient.SendAsync(request, cts.Token);
        // If status code is 200, the token is valid
        var isValid = response.IsSuccessStatusCode;
        Debugs.Log($"Token is valid: {isValid}");
        return isValid;
    }

    #endregion

    #region GET STREAM/CATEGORY

//TODO search:
// channels : https://dev.twitch.tv/docs/api/reference/#search-channels
// teams : https://dev.twitch.tv/docs/api/reference/#get-teams
    private const string SoftwareAndGameDevId = "1469308723";

    public static StreamData[] GetAllDevStreams()
    {
        //software & gameDevelopment
        var response = GetStreamsByGameId(new[] { SoftwareAndGameDevId });
        if (response != null) return response;
        Debugs.LogError("No streams found");
        return null;
    }


    public static StreamData[] GetStreamsByUserId(string[] userIds)
    {
        // A user ID used to filter the list of streams.
        // Returns only the streams of those users that are broadcasting.
        // You may specify a maximum of 100 IDs.
        // To specify multiple IDs, include the user_id parameter for each user.
        // For example, &user_id=1234&user_id=5678.
        var query = "";
        foreach (var userLogin in userIds)
            query += $"&user_id={userLogin}";
        var max = userIds.Length;
        var response = GetStreams(query, Mathf.Clamp(max, 1, 100));
        if (response != null) return TryDoPagination(max, response);
        Debugs.LogError("No streams found");
        return null;
    }


    public static StreamData[] GetStreamsByUserLogin(string[] userLogins)
    {
        // A user login name used to filter the list of streams.
        // Returns only the streams of those users that are broadcasting.
        // You may specify a maximum of 100 login names.
        // To specify multiple names, include the user_login parameter for each user.
        // For example, &user_login=foo&user_login=bar.
        var query = "";
        foreach (var userLogin in userLogins)
            query += $"&user_login={userLogin}";

        var max = userLogins.Length;
        var response = GetStreams(query, Mathf.Clamp(max, 1, 100));
        if (response != null) return TryDoPagination(max, response);
        Debugs.LogError("No streams found");
        return null;
    }

    public static StreamData[] GetStreamsByGameId(string[] gameIds)
    {
        // A game (category) ID used to filter the list of streams.
        // Returns only the streams that are broadcasting the game (category).
        // You may specify a maximum of 100 IDs.
        // To specify multiple IDs, include the game_id parameter for each game.
        // For example, &game_id=9876&game_id=5432.
        var query = "";
        foreach (var gameId in gameIds)
            query += $"&game_id={gameId}";
        var max = 100;
        var response = GetStreams(query, max);
        if (response != null) return TryDoPagination(max, response);
        Debugs.LogError("No streams found");
        return null;
    }

    private static StreamData[] TryDoPagination(int max, StreamDataResponse response)
    {
        List<StreamData> streams = new();
        streams.AddRange(response.data);

        var maxIt = 100;
        var currIt = 0;
        while (!string.IsNullOrEmpty(response?.pagination?.cursor) && streams.Count < max)
        {
            currIt++;
            if (currIt >= maxIt)
            {
                Debugs.LogError("Reached max iterations");
                break;
            }

            response = GetStreams("", max, response.pagination.cursor);
            if (response == null) break;
            streams.AddRange(response.data);
        }

        return streams.ToArray();
    }

    public static StreamDataResponse GetStreams(string query, int first = 100, string cursor = "")
    {
        // GetStreams : https://dev.twitch.tv/docs/api/reference/#get-streams

        if (!string.IsNullOrEmpty(query)) query = "&" + query;

        var uri = $"https://api.twitch.tv/helix/streams?first={first}{query}";

        if (!string.IsNullOrEmpty(cursor)) uri += "&after=" + cursor;

        var request = CreateDefaultRequest(HttpMethod.Get, uri);
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = HttpClient.SendAsync(request, ct).Result;

        if (response.IsSuccessStatusCode)
        {
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<StreamDataResponse>(json);
            if (result != null)
                return result;
        }
        else
        {
            Debugs.LogError("error: " + response.StatusCode);
        }

        return null;
    }


    public static void GetCategory(string categoryName = "")
    {
        // https://dev.twitch.tv/docs/api/reference/#search-categories
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
        //* Name: Software and Game Development
        //* ID: 1469308723
        //* BoxArtURL: https://static-cdn.jtvnw.net/ttv-boxart/1469308723-52x72.jpg
        if (string.IsNullOrEmpty(categoryName))
            categoryName = "development";
        var uri = $"https://api.twitch.tv/helix/search/categories?query={categoryName}";
        var request = CreateDefaultRequest(HttpMethod.Get, uri);

        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = HttpClient.SendAsync(request, ct).Result;

        if (response.IsSuccessStatusCode)
        {
            var json = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<CategoryResponse>(json);
            if (result?.data != null)
                foreach (var categoryData in result.data)
                    Debugs.Log(categoryData);
            else
                Debugs.LogError("error: fucking serialized shit");
        }
        else
        {
            Debugs.LogError("error: " + response.StatusCode);
        }
    }

    #endregion

    #region CHAT

    private static DateTime _lastAnnouncementSent;

    public static void SendChatAnnouncement(string announcement,
        AnnouncementColor announcementColor = AnnouncementColor.Primary, int attempt = 0)
    {
        if (DateTime.Now - _lastAnnouncementSent < TimeSpan.FromSeconds(2))
        {
            SendChatMessage("Announcement is on cooldown");
            return;
        }

        //! Rate Limits: One announcement may be sent every 2 seconds.
        //https://dev.twitch.tv/docs/api/reference/#send-chat-announcement
        //-d '{"message":"Hello chat!","color":"purple"}'
        //TODO
        var uri =
            $"https://api.twitch.tv/helix/chat/announcements?broadcaster_id={_broadcasterId}&moderator_id={_broadcasterId}";
        var request = CreateDefaultRequest(HttpMethod.Post, uri);
        var payload = new
        {
            broadcaster_id = _broadcasterId,
            sender_id = _broadcasterId,
            message = announcement,
            color = announcementColor.ToString().ToLower()
        };
        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = HttpClient.SendAsync(request, ct).Result;
        if (response.IsSuccessStatusCode)
        {
            _lastAnnouncementSent = DateTime.Now;
            Debugs.Log("Sent chat announcement: " + announcement);
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (attempt > 3)
                throw new AuthenticationException("failed to refresh token and resend message");
            Debugs.LogError("Failed to send message, attempting to refresh token");
            _ = EventSubWebsocket.instance.Handle401();
            Debugs.LogWarning("Token refreshed, attempting to resend chat message");
            attempt += 1;
            SendChatAnnouncement(announcement, announcementColor, attempt);
        }
        else
        {
            var error = response.Content.ReadAsStringAsync().Result;
            Debugs.LogError(error);
        }
    }

    public static void SendChatMessage(string chatMessage, int attempt = 0)
    {
        //https://dev.twitch.tv/docs/api/reference/#send-chat-message
        //TODO to send msg's as bot, we need a token for bot...
        var uri = "https://api.twitch.tv/helix/chat/messages";
        var request = CreateDefaultRequest(HttpMethod.Post, uri);

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
            if (attempt > 3)
                throw new AuthenticationException("failed to refresh token and resend message");
            Debugs.LogError("Failed to send message, attempting to refresh token");
            _ = EventSubWebsocket.instance.Handle401();
            Debugs.LogWarning("Token refreshed, attempting to resend chat message");
            attempt += 1;
            SendChatMessage(chatMessage, attempt);
        }
        else
        {
            var error = response.Content.ReadAsStringAsync().Result;
            Debugs.LogError(error);
        }
    }

    #endregion

    #region GET USERS

    private static string GetUsers(string query = "")
    {
        var uri = "https://api.twitch.tv/helix/users";
        if (!string.IsNullOrEmpty(query)) uri += $"?{query}";
        var request = CreateDefaultRequest(HttpMethod.Get, uri);
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = HttpClient.SendAsync(request, ct).Result;
        return response.Content.ReadAsStringAsync().Result;
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

    #endregion

    #region TYPES

    public enum AnnouncementColor
    {
        Blue,
        Green,
        Orange,
        Purple,
        Primary // (default)
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

    public class StreamDataResponse
    {
        [JsonProperty("data")] public List<StreamData> data { get; set; }

        [JsonProperty("pagination")] public Pagination pagination { get; set; }
    }

    public class StreamData
    {
        [JsonProperty("id")] public string id { get; set; }
        [JsonProperty("user_id")] public string userId { get; set; }
        [JsonProperty("user_login")] public string userLogin { get; set; }
        [JsonProperty("user_name")] public string userName { get; set; }
        [JsonProperty("game_id")] public string gameId { get; set; }
        [JsonProperty("game_name")] public string gameName { get; set; }
        [JsonProperty("type")] public string type { get; set; }
        [JsonProperty("title")] public string title { get; set; }
        [JsonProperty("tags")] public List<string> tags { get; set; }
        [JsonProperty("viewer_count")] public int viewerCount { get; set; }
        [JsonProperty("started_at")] public DateTime startedAt { get; set; }
        [JsonProperty("language")] public string language { get; set; }
        [JsonProperty("thumbnail_url")] public string thumbnailUrl { get; set; }
        [JsonProperty("tag_ids")] public List<string> tagIds { get; set; }
        [JsonProperty("is_mature")] public bool isMature { get; set; }
    }

    #endregion
}