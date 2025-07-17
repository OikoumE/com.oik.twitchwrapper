using System;

public class ChatMessage
{
    private readonly DateTime _timestamp;
    public readonly string ChatterColor;
    public readonly string ChatterUserId;
    public readonly string ChatterUserLogin;
    public readonly string ChatterUserName;
    public readonly string MessageText;

    public ChatMessage(string messageText, string chatterUserId, string chatterUserLogin,
        string chatterUserName, string color)
    {
        ChatterColor = color;
        MessageText = messageText;
        ChatterUserId = chatterUserId;
        ChatterUserLogin = chatterUserLogin;
        ChatterUserName = chatterUserName;
        _timestamp = DateTime.Now;
    }

    public int userId => int.Parse(ChatterUserId);

    public string Timestamp(string format = "HH:mm:ss")
    {
        return _timestamp.ToString(format);
    }
}