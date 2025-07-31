using System;

public class ChatMessage
{
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
    ///     The Text of the Message
    /// </summary>
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

    /// <summary>
    ///     Int version of ChatterUserId
    /// </summary>
    public int userId => int.Parse(ChatterUserId);

    public string Timestamp(string format = "HH:mm:ss")
    {
        return _timestamp.ToString(format);
    }
}