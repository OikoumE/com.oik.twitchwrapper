using System;
using System.Linq;

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