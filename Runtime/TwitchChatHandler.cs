using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TwitchChatHandler
{
    private const string FileName = "commands.json";
    private readonly EventSubWebsocket _client;
    private readonly Dictionary<CommandString, Action<ChatCommand>> _commands;

    private readonly string[] _ignoreNames;

    private Dictionary<string, string> _customCommands;
    private Dictionary<CommandString, Action<ChatCommand>> _defaultCommands;

    public TwitchChatHandler(EventSubWebsocket client,
        Dictionary<CommandString, Action<ChatCommand>> commands, string[] ignoreNames)
    {
        Debugs.Log("Initializing TwitchChatHandler");
        if (ignoreNames != null)
            _ignoreNames = ignoreNames.Select(x => x.ToLower()).ToArray();
        _client = client;
        _commands = commands;
        SetupCommands();
        AppendCustomCommands();
    }

    private void SetupCommands()
    {
        _defaultCommands = new Dictionary<CommandString, Action<ChatCommand>>
        {
            {
                new CommandString(new[] { "hello", "hi" }),
                c => TwitchApi.SendChatMessage($"Hello {c.ChatterUserName}!")
            },
            {
                new CommandString("about"),
                _ => TwitchApi.SendChatMessage("I am a Twitch bot running on Oik.TwitchWrapper!")
            },
            { new CommandString(new[] { "command", "commands", "cmd", "cmds" }), AvailableCommands },
            { new CommandString(new[] { "cmdadd", "cmdAdd", "addcmd", "addCmd" }), AddCommand },
            { new CommandString(new[] { "cmdedit", "cmdEdit", "editcmd", "editCmd" }), EditCommand }
        };
    }

    private void AppendCustomCommands()
    {
        _customCommands = LoadFromJson();
        foreach (var (key, value) in _customCommands)
            _defaultCommands.Add(new CommandString(key), _ => TwitchApi.SendChatMessage(value));
    }

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    public void SaveToJson()
    {
        var wrapper = new SerializationWrapper
        {
            keys = _customCommands.Keys.ToList(),
            values = _customCommands.Values.ToList()
        };
        var json = JsonUtility.ToJson(wrapper);
        File.WriteAllText(GetPath(), json);
    }

    public Dictionary<string, string> LoadFromJson()
    {
        Debugs.Log("Attempting to load CustomCommands: " + Application.persistentDataPath);
        var path = GetPath();
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<SerializationWrapper>(json)?.ToDictionary()
                   ?? new Dictionary<string, string>();
        }

        Debugs.LogError("Could not find CustomCommands!");
        return new Dictionary<string, string>();
    }

    private void EditCommand(ChatCommand obj)
    {
        var messageText = obj.MessageText;
        var newResponseText = string.Join(" ", messageText.Split(" ")[1..]);
        var commandToChange = messageText.Split(" ")[0].Replace(obj.Identifier, "");
        Debugs.Log($"CommandToChange {commandToChange}");
        if (!_customCommands.TryGetValue(commandToChange, out var response))
        {
            var invalidCommand = "Could not find command or command is invalid: " + commandToChange;
            Debugs.LogWarning(invalidCommand);
            TwitchApi.SendChatMessage(invalidCommand);
            return;
        }

        _customCommands[commandToChange] = newResponseText;
        TwitchApi.SendChatMessage($"{commandToChange} successfully edited!");
        SaveToJson();
    }

    private void AddCommand(ChatCommand obj)
    {
        var messageText = obj.MessageText;
        var commandToAdd = messageText.Split(" ")[0].Replace(obj.Identifier, "");
        Debugs.Log($"CommandToAdd: {commandToAdd}");
        if (_customCommands.TryGetValue(commandToAdd, out var response))
        {
            TwitchApi.SendChatMessage($"{commandToAdd} Already exists! did you mean !cmdedit?");
            return;
        }

        var newResponseText = string.Join(" ", messageText.Split(" ")[1..]);
        _customCommands[commandToAdd] = newResponseText;
        var success = $"{commandToAdd} Successfully added!";
        Debugs.Log(success);
        TwitchApi.SendChatMessage(success);
        SaveToJson();
    }


    public void OnChatMessage(JObject payload)
    {
        /*
            {
            "subscription":
                 "event": {
                     "chatter_user_id": "93645775",
                     "chatter_user_login": "itsoik",
                     "chatter_user_name": "itsOiK",
                     "message": {"text": "d"}
                }
            }
        */
        var msg = ParseChatMessagePayload(payload);
        if (msg.MessageText.StartsWith("!"))
        {
            OnChatCommand(new ChatCommand(msg));
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");
        Debugs.Log($"{time} - {msg.ChatterUserName}: {msg.MessageText}");
    }

    private void OnChatCommand(ChatCommand chatCommand)
    {
        var displayName = chatCommand.ChatterUserName;
        if (_ignoreNames != null && _ignoreNames.Contains(displayName.ToLower()))
        {
            Debugs.Log($"Ignoring {displayName}");
            return;
        }

        var commandText = chatCommand.CommandText;
        foreach (var (command, action) in _defaultCommands)
            if (command.Commands.Contains(commandText.ToLower()))
                action.Invoke(chatCommand);
        if (_commands is not { Count: > 0 }) return;
        foreach (var (command, action) in _commands)
            if (command.Commands.Contains(commandText.ToLower()))
                action.Invoke(chatCommand);
    }

    private void AvailableCommands(ChatCommand _)
    {
        var defaultCommands = _defaultCommands.Keys.Select(x => x.Commands[0]).ToArray();
        var commands = _commands.Keys.Select(x => x.Commands[0]).ToArray();
        var reply = string.Join(", ", defaultCommands);
        reply += ", " + string.Join(", ", commands);
        TwitchApi.SendChatMessage(reply);
    }


    public static ChatMessage ParseChatMessagePayload(JObject payload)
    {
        var eventNotification = payload?["event"];
        var message = eventNotification?["message"];

        var messageText = message?["text"]?.ToString();
        var chatterUserId = eventNotification?["chatter_user_id"]?.ToString();
        var chatterUserLogin = eventNotification?["chatter_user_login"]?.ToString();
        var chatterUserName = eventNotification?["chatter_user_name"]?.ToString();

        return new ChatMessage(messageText, chatterUserId, chatterUserLogin, chatterUserName);
    }

    [Serializable]
    private class SerializationWrapper
    {
        public List<string> keys;
        public List<string> values;

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < keys.Count; i++)
                dict[keys[i]] = values[i];
            return dict;
        }
    }
}