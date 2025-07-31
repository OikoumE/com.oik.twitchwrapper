using System;
using UnityEngine;

public class ChatCommand
{
    /// <summary>
    ///     TimeStamp of when we received the command
    /// </summary>
    private readonly DateTime _timestamp;

    /// <summary>
    ///     Chat color of who sent the command
    /// </summary>
    public readonly string ChatterColor;

    /// <summary>
    ///     UserId of who sent the command
    /// </summary>
    public readonly string ChatterUserId;

    /// <summary>
    ///     UserLogin of who sent the command
    /// </summary>
    public readonly string ChatterUserLogin;

    /// <summary>
    ///     UserName of who sent the command
    /// </summary>
    public readonly string ChatterUserName;

    /// <summary>
    ///     The Text of the command (e.g "raid") without the identifier (e.g "!")
    /// </summary>
    public readonly string CommandText;

    /// <summary>
    ///     The identifier for the command (usually "!")
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    ///     MessageText is stripped of CommandText and Identifier
    /// </summary>
    public readonly string MessageText;

    public ChatCommand(ChatMessage msg)
    {
        var cmd = msg.MessageText.Split(" ");
        Identifier = cmd[0][..1]; // grab first char in first word
        CommandText = cmd[0][1..]; // grab first word, except char[0]
        var startIndex = Mathf.Clamp(cmd[0].Length + 1, 0, msg.MessageText.Length);
        MessageText = msg.MessageText[startIndex..]; // grab rest of text without identifier & commandText
        ChatterUserId = msg.ChatterUserId;
        ChatterUserLogin = msg.ChatterUserLogin;
        ChatterUserName = msg.ChatterUserName;
        ChatterColor = msg.ChatterColor;
        _timestamp = DateTime.Now;
    }

    public (string identifier, string cmd, string msg,
        int userID, string userName, string displayName, string chatterColor)
        Deconstruct()
    {
        return (Identifier, CommandText, MessageText,
            int.Parse(ChatterUserId), ChatterUserLogin, ChatterUserName, ChatterColor);
    }

    public string Timestamp(string format = "HH:mm:ss")
    {
        return _timestamp.ToString(format);
    }
}