using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TwitchChatHandler
{
    private readonly EventSubWebsocket _client;
    private readonly Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> _commands;
    private Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> _defaultCommands;

    public TwitchChatHandler(EventSubWebsocket client,
        Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> commands)
    {
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
                (c, cx) => cx.SendChatMessage($"Hello {c.ChatterUserName}!")
            },
            {
                new CommandString("about"),
                (_, cx) => cx.SendChatMessage("I am a Twitch bot running on Oik.TwitchWrapper!")
            },
            { new CommandString("command", new[] { "commands", "cmd", "cmds" }), AvailableCommands }
        };
    }

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
    private void OnChatCommand(ChatCommand chatCommand)
    {
        var displayName = chatCommand.ChatterUserName;
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
        var commands = _defaultCommands.Keys.Select(x => x.Command).ToArray();
        var reply = string.Join(", ", commands);
        _client.SendChatMessage(reply);
    }


    public void OnChatMessage(JObject payload)
    {
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