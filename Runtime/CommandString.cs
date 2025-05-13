using System.Linq;

public class CommandString
{
    public string[] Commands;

    public CommandString(string command)
    {
        Commands = new[] { command };
    }

    public CommandString(string[] commands)
    {
        Commands = commands;
    }

    public bool IsTarget(string command)
    {
        return Commands.Contains(command);
    }
}