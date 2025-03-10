namespace Ignore.Test.Output;

public class ConsoleEx {
    public static void WriteLineColor(string message, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteColor(string message, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ResetColor();
    }

    public static void WriteLinePrefix(string message) {
        WriteColor("> ", ConsoleColor.Green);
        Console.WriteLine(message);
    }

    public static string? ReadLinePrefix() {
        WriteColor("> ", ConsoleColor.Green);
        return Console.ReadLine();
    }
}