using System;

public class ChatCommand
{
    private readonly DateTime _timestamp;
    public readonly string ChatterColor;
    public readonly string ChatterUserId;
    public readonly string ChatterUserLogin;
    public readonly string ChatterUserName;
    public readonly string CommandText;
    public readonly string Identifier;
    public readonly string MessageText;

    public ChatCommand(ChatMessage msg)
    {
        var cmd = msg.MessageText.Split(" ");
        Identifier = cmd[0][..1]; // grab first char in first word
        CommandText = cmd[0][1..]; // grab first word, except char[0]
        MessageText = msg.MessageText[(cmd[0].Length + 1)..]; // grab rest of text without identifier & commandText
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