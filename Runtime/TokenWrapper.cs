using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class TokenWrapper
{
    private const string FileName = "Token.json";
    private static readonly string FilePath = Directory.GetCurrentDirectory();

    public static void OpenFolder()
    {
        Process.Start("explorer.exe", "/select,\"" + Application.persistentDataPath.Replace("/", "\\") + "\"");
    }

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
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
            return null;

        Debug.Log("Loaded AccessToken from: " + path);
        var token = new TokenResponse();
        JsonConvert.PopulateObject(File.ReadAllText(path), token);
        return token;
    }
}