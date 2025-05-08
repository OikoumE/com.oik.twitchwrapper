using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TwitchChatHandler
{
    private readonly EventSubWebsocket _client;
    private readonly Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> _commands;

    private readonly string[] _ignoreNames;
    private Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> _defaultCommands;

    public TwitchChatHandler(EventSubWebsocket client,
        Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> commands, string[] ignoreNames = null)
    {
        if (ignoreNames != null)
            _ignoreNames = ignoreNames.Select(x => x.ToLower()).ToArray();

        _client = client;
        _commands = commands;
        SetupCommands();
    }

    private void SetupCommands()
    {
        _defaultCommands = new Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>>
        {
            {
                new CommandString("hello", new[] { "hi" }),
                (c, cx) => cx.Api.SendChatMessage($"Hello {c.ChatterUserName}!")
            },
            {
                new CommandString("about"),
                (_, cx) => cx.Api.SendChatMessage("I am a Twitch bot running on Oik.TwitchWrapper!")
            },
            { new CommandString("command", new[] { "commands", "cmd", "cmds" }), AvailableCommands }
        };
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
        Debug.Log($"{time} - {msg.ChatterUserName}: {msg.MessageText}");
        // GlobalEventBus.Publish(new OnChatMessage(e));
    }

    private void OnChatCommand(ChatCommand chatCommand)
    {
        var displayName = chatCommand.ChatterUserName;
        if (_ignoreNames != null && _ignoreNames.Contains(displayName))
            return;

        var commandText = chatCommand.CommandText;
        foreach (var (command, action) in _commands)
            if (command.Command == commandText || command.Aliases.Contains(commandText))
                action.Invoke(chatCommand, _client);
        foreach (var (command, action) in _defaultCommands)
            if (command.Command == commandText || command.Aliases.Contains(commandText))
                action.Invoke(chatCommand, _client);
    }

    private void AvailableCommands(ChatCommand _, EventSubWebsocket __)
    {
        var defaultCommands = _defaultCommands.Keys.Select(x => x.Command).ToArray();
        var commands = _commands.Keys.Select(x => x.Command).ToArray();

        var reply = string.Join(", ", defaultCommands);
        reply += ", " + string.Join(", ", commands);
        _client.Api.SendChatMessage(reply);
    }


    private static ChatMessage ParseChatMessagePayload(JObject payload)
    {
        var eventNotification = payload?["event"];
        var message = eventNotification?["message"];

        var messageText = message?["text"]?.ToString();
        var chatterUserId = eventNotification?["chatter_user_id"]?.ToString();
        var chatterUserLogin = eventNotification?["chatter_user_login"]?.ToString();
        var chatterUserName = eventNotification?["chatter_user_name"]?.ToString();

        return new ChatMessage(messageText, chatterUserId, chatterUserLogin, chatterUserName);
    }
}