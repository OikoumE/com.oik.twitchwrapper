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