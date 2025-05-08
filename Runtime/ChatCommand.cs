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