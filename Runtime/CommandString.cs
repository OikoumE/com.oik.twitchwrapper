using System;
using System.Linq;

public class CommandString
{
    private string _broadCasterId;
    private readonly float _cooldown;
    private DateTime _lastCommandTime;
    private bool _restricted;
    public string[] Commands;

    public CommandString(string command, float coolDown = 0, bool restrictedToBroadcaster = false,
        string broadCasterId = "")
    {
        ThrowIfInvalidRestriction(restrictedToBroadcaster, broadCasterId);
        Commands = new[] { command };
        _cooldown = coolDown;
    }

    public CommandString(string command, bool restrictedToBroadcaster = false, string broadCasterId = "")
    {
        ThrowIfInvalidRestriction(restrictedToBroadcaster, broadCasterId);
        Commands = new[] { command };
    }

    public CommandString(string command, float coolDown = 0)
    {
        Commands = new[] { command };
        _cooldown = coolDown;
    }

    public CommandString(string command)
    {
        Commands = new[] { command };
    }

    public CommandString(string[] commands, float coolDown = 0, bool restrictedToBroadcaster = false,
        string broadCasterId = "")
    {
        ThrowIfInvalidRestriction(restrictedToBroadcaster, broadCasterId);
        Commands = commands;
        _cooldown = coolDown;
    }

    public CommandString(string[] commands, bool restrictedToBroadcaster = false, string broadCasterId = "")
    {
        ThrowIfInvalidRestriction(restrictedToBroadcaster, broadCasterId);
        Commands = commands;
    }

    public CommandString(string[] commands, float coolDown = 0)
    {
        Commands = commands;
        _cooldown = coolDown;
    }

    public CommandString(string[] commands)
    {
        Commands = commands;
    }

    private void ThrowIfInvalidRestriction(bool restrictedToBroadcaster, string broadCasterId)
    {
        if (restrictedToBroadcaster && string.IsNullOrEmpty(broadCasterId))
            throw new Exception("Broadcaster ID required when restriction is enabled");
        _restricted = restrictedToBroadcaster;
        _broadCasterId = broadCasterId;
    }

    public bool IsValid(ChatCommand command)
    {
        if (_restricted && command.ChatterUserId != _broadCasterId)
            return false;

        if (_cooldown > 0 && DateTime.Now - _lastCommandTime < TimeSpan.FromSeconds(_cooldown))
            return false;

        _lastCommandTime = DateTime.Now;
        return Commands.Contains(command.CommandText);
    }
}