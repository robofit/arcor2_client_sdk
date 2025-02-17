using Arcor2.ClientSdk.Communication;

namespace Ignore.Test.Output;

public class ConsoleLogger : IArcor2Logger
{
    public void LogInfo(string message) => ConsoleEx.WriteLineColor(message, ConsoleColor.DarkGray);

    public void LogError(string message)
    {
        ConsoleEx.WriteColor("ERROR: ", ConsoleColor.Red);
        Console.WriteLine(message);
    }

    public void LogWarning(string message)
    {
        ConsoleEx.WriteColor("WARN: ", ConsoleColor.Yellow);
        Console.WriteLine(message);
    }
}