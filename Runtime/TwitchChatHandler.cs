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

    private readonly string[] _ignoreNames;
    private Dictionary<CommandString, Action<ChatCommand>> _commands;

    private Dictionary<string, string> _customCommands;

    public TwitchChatHandler(EventSubWebsocket client,
        Dictionary<CommandString, Action<ChatCommand>> commands, string[] ignoreNames)
    {
        Debugs.Log("Initializing TwitchChatHandler");
        if (ignoreNames != null)
            _ignoreNames = ignoreNames.Select(x => x.ToLower()).ToArray();
        _client = client;
        SetupCommands(commands);
    }

    #region SETUP COMMANDS

    private void SetupCommands(Dictionary<CommandString, Action<ChatCommand>> commands)
    {
        _commands = new Dictionary<CommandString, Action<ChatCommand>>
        {
            #region SILLY COMMANDS

            {
                new CommandString(new[] { "hello", "hi" }),
                c => TwitchApi.SendChatMessage($"Hello {c.ChatterUserName}!")
            },
            {
                new CommandString("about"),
                _ => TwitchApi.SendChatMessage("I am a Twitch bot running on Oik.TwitchWrapper!")
            },
            { new CommandString(new[] { "command", "commands", "cmd", "cmds" }), AvailableCommands },

            #endregion

            #region CUSTOM COMMAND HANDLER

            { new CommandString(new[] { "cmdadd", "cmdAdd", "addcmd", "addCmd" }), AddCommand },
            { new CommandString(new[] { "cmdedit", "cmdEdit", "editcmd", "editCmd" }), EditCommand },
            {
                new CommandString(new[]
                {
                    "cmddelete", "cmdDelete", "deletecmd", "deleteCmd",
                    "cmdremove", "cmdRemove", "Removecmd", "removeCmd"
                }),
                RemoveCommand
            }

            #endregion
        };
        foreach (var (key, value) in commands)
            _commands.Add(key, value);
        AppendCustomCommands();
    }

    private void AppendCustomCommands()
    {
        _customCommands = LoadFromJson();
        foreach (var (commandString, responseString) in _customCommands.ToList())
            _commands.Add(new CommandString(commandString), _ => TwitchApi.SendChatMessage(responseString));
    }

    #endregion

    #region SAVE/LOAD CUSTOM COMMANDS

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

    #region SERIALIZATION WRAPPER

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

    #endregion

    #endregion

    #region CUSTOM COMMANDS

    private void EditCommand(ChatCommand obj)
    {
        if (obj.ChatterUserName.ToLower() != "itsoik") return;
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


        var asd = _commands.FirstOrDefault(x =>
            x.Key.Commands.Contains(commandToChange));
        if (asd is { Key: not null, Value: not null })
        {
            _commands.Remove(asd.Key);
            _commands.Add(asd.Key, _ => TwitchApi.SendChatMessage(newResponseText));
        }

        _customCommands[commandToChange] = newResponseText;
        TwitchApi.SendChatMessage($"{commandToChange} successfully edited!");
        SaveToJson();
    }

    private void AddCommand(ChatCommand obj)
    {
        if (obj.ChatterUserName.ToLower() != "itsoik") return;
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
        _commands.Add(new CommandString(commandToAdd), _ => TwitchApi.SendChatMessage(newResponseText));

        var success = $"{commandToAdd} Successfully added!";
        Debugs.Log(success);
        TwitchApi.SendChatMessage(success);
        SaveToJson();
    }

    private void RemoveCommand(ChatCommand obj)
    {
        if (obj.ChatterUserName.ToLower() != "itsoik") return;
        var messageText = obj.MessageText;
        var commandToRemove = messageText.Split(" ")[0].Replace(obj.Identifier, "");
        Debugs.Log($"CommandToRemove {commandToRemove}");
        if (!_customCommands.ContainsKey(commandToRemove))
        {
            var fail = $"{commandToRemove} is not a valid command.";
            Debugs.LogWarning(fail);
            TwitchApi.SendChatMessage(fail);
            return;
        }

        var success = $"{commandToRemove} removed successfully.";
        Debugs.Log(success);
        TwitchApi.SendChatMessage(success);
        _customCommands.Remove(commandToRemove);
        var command = _commands.FirstOrDefault(x => x.Key.Commands.Contains(commandToRemove));
        if (command is { Key: not null, Value: not null })
            _commands.Remove(command.Key);
    }

    #endregion

    #region HANDLE INCOMING COMMAND

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

    private void OnChatCommand(ChatCommand chatCommand)
    {
        var displayName = chatCommand.ChatterUserName;
        if (_ignoreNames != null && _ignoreNames.Contains(displayName.ToLower()))
        {
            Debugs.Log($"Ignoring {displayName}");
            return;
        }

        var commandText = chatCommand.CommandText;
        foreach (var (command, action) in _commands.ToList())
            if (command.Commands.Contains(commandText.ToLower()))
                action.Invoke(chatCommand);
    }

    private void AvailableCommands(ChatCommand _)
    {
        var defaultCommands = _commands.Keys.Select(x => x.Commands[0]).ToArray();
        var reply = string.Join(", ", defaultCommands);
        TwitchApi.SendChatMessage(reply);
    }

    #endregion
}