using System;
using System.Linq;

public class CommandString
{
    private string _broadCasterId;
    private bool _restricted;
    public string[] Commands;

    public CommandString(string command, bool restrictedToBroadcaster = false, string broadCasterId = "")
    {
        ThrowIfInvalidRestriction(restrictedToBroadcaster, broadCasterId);
        Commands = new[] { command };
    }

    public CommandString(string[] commands, bool restrictedToBroadcaster = false, string broadCasterId = "")
    {
        ThrowIfInvalidRestriction(restrictedToBroadcaster, broadCasterId);
        Commands = commands;
    }

    private void ThrowIfInvalidRestriction(bool restrictedToBroadcaster, string broadCasterId)
    {
        if (restrictedToBroadcaster && string.IsNullOrEmpty(broadCasterId))
            throw new Exception("Broadcaster ID required when restriction is enabled");
        _restricted = restrictedToBroadcaster;
        _broadCasterId = broadCasterId;
    }

    public bool IsTarget(ChatCommand command)
    {
        if (_restricted && command.ChatterUserId != _broadCasterId)
            return false;
        return Commands.Contains(command.CommandText);
    }
}