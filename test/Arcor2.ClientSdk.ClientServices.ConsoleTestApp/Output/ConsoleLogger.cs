﻿using Arcor2.ClientSdk.Communication;

namespace Arcor2.ClientSdk.ClientServices.ConsoleTestApp.Output;

public class ConsoleLogger : IArcor2Logger {
    public void LogInfo(string message) {
        // Something to make the logs a bit useful
        if(message.StartsWith("Received a new ARCOR2 message:\n{\"event\":\"RobotJoints\",\"data\":") ||
           message.StartsWith("Received a new ARCOR2 message:\n{\"event\":\"RobotEef\",\"data\":")) {
            if(Random.Shared.NextDouble() > 0.0001) {
                return;
            }
        }

        ConsoleEx.WriteLineColor(message, ConsoleColor.DarkGray);
    }

    public void LogError(string message) {
        ConsoleEx.WriteColor("ERROR: ", ConsoleColor.Red);
        Console.WriteLine(message);
    }

    public void LogWarn(string message) {
        ConsoleEx.WriteColor("WARN: ", ConsoleColor.Yellow);
        Console.WriteLine(message);
    }
}