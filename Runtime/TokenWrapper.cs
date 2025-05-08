using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

public static class TokenWrapper
{
    private const string FileName = "Token.json";
    private static readonly string FilePath = Directory.GetCurrentDirectory();

    public static void OpenFolder()
    {
        Process.Start("explorer.exe", "/select,\"" + GetPath().Replace("/", "\\") + "\"");
    }

    private static string GetPath()
    {
        return Path.Combine(FilePath, FileName);
    }

    public static void SaveToJson(TokenResponse token)
    {
        var path = GetPath();
        Debug.Log("Saving: " + path);
        var json = JsonConvert.SerializeObject(token);
        File.WriteAllText(path, json);
    }

    public static void RevokeToken()
    {
        var path = GetPath();
        Debug.Log("Revoking: " + path);
        if (File.Exists(path)) File.Delete(path);
    }


    public static TokenResponse LoadFromJson()
    {
        var path = GetPath();
        if (!File.Exists(path))
        {
            Debug.LogError("Failed to load AccessToken, proceeding with DCF auth");
            return null;
        }

        Debug.Log("Loaded AccessToken from: " + path);
        var token = new TokenResponse();
        JsonConvert.PopulateObject(File.ReadAllText(path), token);
        return token;
    }
}


// Token container
public class TokenResponse
{
    [JsonProperty("access_token")] //
    public string AccessToken { get; set; }

    [JsonProperty("expires_in")] //
    public int ExpiresIn { get; set; }

    [JsonProperty("refresh_token")] //
    public string RefreshToken { get; set; }

    [JsonProperty("scope")] //
    public string[] Scope { get; set; }

    [JsonProperty("token_type")] //
    public string TokenType { get; set; }
}

public class DeviceCodeResponse
{
    [JsonProperty("device_code")] //
    public string DeviceCode { get; set; }

    [JsonProperty("expires_in")] //
    public int ExpiresIn { get; set; }

    [JsonProperty("interval")] //
    public int Interval { get; set; }

    [JsonProperty("user_code")] //
    public string UserCode { get; set; }

    [JsonProperty("verification_uri")] //
    public string VerificationUri { get; set; }
}