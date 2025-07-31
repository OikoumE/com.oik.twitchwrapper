using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TwitchAuthenticator
{
    // check/revoke tokens
    //https://barrycarlyon.github.io/twitch_misc/examples/token_checker/

    private readonly HttpClient _client = new();
    private readonly string _clientId;
    private readonly string _scopes;

    public TwitchAuthenticator(string clientId, string[] scopes)
    {
        Debugs.Log("Initializing TwitchAuthenticator");
        _clientId = clientId;
        _scopes = string.Join(" ", scopes.Distinct());
    }

    public async Task<TokenResponse> RunDeviceFlowAsync(float timeoutSeconds)
    {
        //https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#device-code-grant-flow
        var tokenResponse = TokenWrapper.LoadFromJson();
        // if no token, create
        if (tokenResponse == null)
        {
            Debugs.LogError("No AccessToken stored");
            return await DeviceFlow(timeoutSeconds);
        }

        // validate token
        var isValid = await TwitchApi.ValidateToken(tokenResponse);
        Debugs.Log($"TokenResponse valid: <color={(isValid ? "green" : "red")}>{isValid}</color>");
        // if token valid
        if (isValid) return tokenResponse;
        // if not, refresh token
        return await Handle401(timeoutSeconds, _clientId, tokenResponse);
    }

    private async Task<TokenResponse> DeviceFlow(float timeoutSeconds)
    {
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var deviceCodeResponse = RequestDeviceCode(ct);
        if (deviceCodeResponse == null) return null;

        Debugs.LogWarning("opening browser!");
        // Open the URL in the default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = deviceCodeResponse.VerificationUri,
            UseShellExecute = true
        });
        return await PollForTokens(timeoutSeconds, deviceCodeResponse, ct);
    }

    private DeviceCodeResponse RequestDeviceCode(CancellationToken ct)
    {
        // 1. The first step starts a device authorization grant code flow for your client.
        var content = new MultipartFormDataContent
        {
            { new StringContent(_clientId), "client_id" },
            { new StringContent(_scopes), "scopes" }
        };
        Debugs.Log("Requesting device code");
        var response = _client.PostAsync("https://id.twitch.tv/oauth2/device", content, ct).Result;
        if (!response.IsSuccessStatusCode) return null;
        //5. deserialize
        var result = response.Content.ReadAsStringAsync().Result;
        return JsonConvert.DeserializeObject<DeviceCodeResponse>(result);
    }

    private async Task<TokenResponse> PollForTokens(float timeoutSeconds,
        DeviceCodeResponse deviceResp
        , CancellationToken ct
    )
    {
        var payload = new MultipartFormDataContent
        {
            { new StringContent(_clientId), "client_id" },
            { new StringContent(_scopes), "scopes" },
            { new StringContent(deviceResp.DeviceCode), "device_code" },
            { new StringContent("urn:ietf:params:oauth:grant-type:device_code"), "grant_type" }
        };

        var url = "https://id.twitch.tv/oauth2/token";
        var delay = TimeSpan.FromSeconds(deviceResp.Interval);
        var stopwatch = Stopwatch.StartNew();

        Debugs.Log($"Waiting for token: timeout after {timeoutSeconds}sec");
        while (!ct.IsCancellationRequested)
        {
            if (stopwatch.ElapsedMilliseconds > timeoutSeconds * 1000)
            {
                Debugs.LogError("Auth timed out!");
                break;
            }

            var resp = await _client.PostAsync(url, payload, ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = resp.Content.ReadAsStringAsync().Result;
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                TokenWrapper.SaveToJson(tokenResponse);
                Debugs.Log("Got token");
                return tokenResponse;
            }

            await Task.Delay(delay, ct);
        }

        return null;
    }

    public async Task<TokenResponse> Handle401(float timeoutSeconds, string clientId, TokenResponse tokenResponse)
    {
        var newToken = new TokenResponse();
        var result = RefreshAccessToken(tokenResponse.RefreshToken, clientId);
        if (result == null)
        {
            newToken = await DeviceFlow(timeoutSeconds);
        }
        else
        {
            if (result == null)
                throw new Exception("Failed to refresh token");
            var json = JObject.Parse(result);
            newToken.AccessToken = "" + json["access_token"];
            newToken.RefreshToken = "" + json["refresh_token"];
            TokenWrapper.SaveToJson(newToken);
        }

        return newToken;
    }

    private string RefreshAccessToken(string refreshToken, string clientId)
    {
        // refresh token
        // https://dev.twitch.tv/docs/authentication/refresh-tokens/
        Debugs.Log("<color=blue>Attempting</color> to refresh token");
        using var client = new HttpClient();
        var values = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", Uri.EscapeDataString(refreshToken) },
            { "client_id", clientId }
        };
        var content = new FormUrlEncodedContent(values);
        var ct = EventSubWebsocket.GetCancellationTokenSource().Token;
        var response = client.PostAsync("https://id.twitch.tv/oauth2/token", content, ct).Result;
        var success = response.IsSuccessStatusCode;
        Debugs.Log($"Refresh token result: {success}");
        if (!success) return null;
        var json = response.Content.ReadAsStringAsync().Result;
        return json; // contains new access and refresh tokens
    }

    public void RevokeToken(TokenResponse token, CancellationToken ct)
    {
        if (token == null) return;
        using var client = new HttpClient();
        var values = HttpUtility.ParseQueryString(string.Empty);
        values["client_id"] = _clientId;
        values["token"] = token.AccessToken;

        var content = new StringContent(values.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = client.PostAsync("https://id.twitch.tv/oauth2/revoke", content, ct).Result;

        if (!response.IsSuccessStatusCode)
            throw new Exception(response.Content.ReadAsStringAsync().Result);
        TokenWrapper.RevokeToken();
    }
}