using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TwitchChatHandler
{
    private readonly EventSubWebsocket _client;
    private Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>> _commands;

    public TwitchChatHandler(EventSubWebsocket client)
    {
        _client = client;
        SetupCommands();
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

        switch (commandText)
        {
            case "hello":
                _client.SendChatMessage($"Hello {displayName}!");
                return;
            case "about":
                _client.SendChatMessage("I am a Twitch bot running on Oik.TwitchWrapper!");
                return;
            case "command":

                return;
        }

        foreach (var (command, action) in _commands)
            if (command.Command == commandText || command.Aliases.Contains(commandText))
                action.Invoke(chatCommand, _client);
    }

    private void AvailableCommands(ChatCommand _, EventSubWebsocket __)
    {
        var commands = _commands.Keys.Select(x => x.Command).ToArray();
        var reply = string.Join(", ", commands);
        _client.SendChatMessage(reply);
    }

    private void SetupCommands()
    {
        _commands = new Dictionary<CommandString, Action<ChatCommand, EventSubWebsocket>>
        {
            {
                new CommandString("hello", new[] { "hi" }),
                (c, cx) => cx.SendChatMessage($"Hello {c.ChatterUserName}!")
            },
            {
                new CommandString("hello", new[] { "hi" }),
                (_, cx) => cx.SendChatMessage("I am a Twitch bot running on Oik.TwitchWrapper!")
            },
            { new CommandString("hello", new[] { "hi" }), AvailableCommands }
        };
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

    public class ChatMessage
    {
        public readonly string ChatterUserName;
        public readonly string MessageText;
        public string ChatterUserId;
        public string ChatterUserLogin;

        public ChatMessage(string messageText, string chatterUserId, string chatterUserLogin,
            string chatterUserName)
        {
            MessageText = messageText;
            ChatterUserId = chatterUserId;
            ChatterUserLogin = chatterUserLogin;
            ChatterUserName = chatterUserName;
        }

        public int userId => int.Parse(ChatterUserId);
    }

    public class CommandString
    {
        public string[] Aliases;
        public string Command;

        public CommandString(string command, string[] aliases = null)
        {
            Command = command;
            Aliases = aliases ?? Array.Empty<string>();
        }

        public bool IsTarget(string command)
        {
            return Command == command || Aliases.Contains(command);
        }
    }

    public class ChatCommand
    {
        public readonly string ChatterUserId;
        public readonly string ChatterUserLogin;
        public readonly string ChatterUserName;
        public readonly string CommandText;
        public readonly string Identifier;
        public readonly string MessageText;

        public ChatCommand(ChatMessage msg)
        {
            var cmd = msg.MessageText.Split(" ");
            CommandText = cmd[0][1..];
            Identifier = cmd[0][..1];
            MessageText = string.Join(" ", cmd[1..]);
            ChatterUserId = msg.ChatterUserId;
            ChatterUserLogin = msg.ChatterUserLogin;
            ChatterUserName = msg.ChatterUserName;
        }

        public (string identifier, string cmd, string msg, int userID, string userName, string displayName)
            Deconstruct()
        {
            return (Identifier,
                CommandText,
                MessageText,
                int.Parse(ChatterUserId),
                ChatterUserLogin,
                ChatterUserName);
        }
    }
}