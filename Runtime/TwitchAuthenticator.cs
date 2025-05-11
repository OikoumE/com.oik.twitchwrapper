using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

public class TwitchAuthenticator
{
    // check/revoke tokens
    //https://barrycarlyon.github.io/twitch_misc/examples/token_checker/

    private readonly HttpClient _client = new();
    private readonly string _clientId;
    private readonly string _scopes;

    public TwitchAuthenticator(string clientId, string scopes)
    {
        Debug.Log("Initializing TwitchAuthenticator");
        _clientId = clientId;
        _scopes = scopes;
    }

    public async Task<TokenResponse> RunDeviceFlowAsync(float timeoutSeconds, CancellationToken ct)
    {
        //https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#device-code-grant-flow
        var tokenResponse = TokenWrapper.LoadFromJson();
        // if no token, create
        if (tokenResponse == null)
        {
            Debug.LogError("No AccessToken stored");
            return await DeviceFlow(timeoutSeconds, ct);
        }

        // validate token
        var isValid = await TwitchApi.ValidateToken(tokenResponse, ct);
        Debug.Log($"TokenResponse valid: <color={(isValid ? "green" : "red")}>{isValid}</color>");
        // if token valid
        if (isValid) return tokenResponse;
        // if not, refresh token
        return await Handle401(timeoutSeconds, _clientId, tokenResponse, ct);
    }

    private async Task<TokenResponse> DeviceFlow(float timeoutSeconds, CancellationToken ct)
    {
        var deviceCodeResponse = RequestDeviceCode(ct);
        if (deviceCodeResponse == null) return null;

        Debug.LogWarning("opening browser!");
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
        Debug.Log("Requesting device code");
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

        Debug.Log($"Waiting for token: timeout after {timeoutSeconds}sec");
        while (!ct.IsCancellationRequested)
        {
            if (stopwatch.Elapsed.Seconds > timeoutSeconds)
            {
                Debug.LogError("Auth timed out!");
                break;
            }

            var resp = await _client.PostAsync(url, payload, ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = resp.Content.ReadAsStringAsync().Result;
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                TokenWrapper.SaveToJson(tokenResponse);
                Debug.Log("Got token");
                return tokenResponse;
            }

            await Task.Delay(delay, ct);
        }

        return null;
    }

    public async Task<TokenResponse> Handle401(float timeoutSeconds, string clientId, TokenResponse tokenResponse,
        CancellationToken ct)
    {
        Debug.Log("Attempting to refresh token");
        var newToken = new TokenResponse();
        var result = RefreshAccessToken(tokenResponse.RefreshToken, clientId, ct);
        if (result == null)
        {
            newToken = await DeviceFlow(timeoutSeconds, ct);
        }
        else
        {
            if (result == null)
                throw new Exception("Failed to refresh token");
            var json = JObject.Parse(result);

            newToken.AccessToken = "" + json["access_token"];
            newToken.RefreshToken = "" + json["refresh_token"];
        }

        return newToken;
    }

    private string RefreshAccessToken(string refreshToken, string clientId, CancellationToken ct)
    {
        // refresh token
        // https://dev.twitch.tv/docs/authentication/refresh-tokens/
        using var client = new HttpClient();
        var values = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", Uri.EscapeDataString(refreshToken) },
            { "client_id", clientId }
        };
        var content = new FormUrlEncodedContent(values);
        var response = client.PostAsync("https://id.twitch.tv/oauth2/token", content, ct).Result;
        if (!response.IsSuccessStatusCode) return null;
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