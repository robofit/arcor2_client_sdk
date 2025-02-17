using Arcor2.ClientSdk.ClientServices;
using Ignore.Test.Models;
using Ignore.Test.Output;

namespace Ignore.Test;

internal class Program {
    public static Arcor2Session session = null!;

    private static int _isTerminating;

    static async Task Main(string[] args) {
        ConfigureGracefulTermination();
        // Startup I/O
        var options = StartUp();
        if(options == null) {
            return;
        }

        // Init connection, init messages, login..
        var logger = options.EnableConsoleLogger ? new ConsoleLogger() : null;
        session = new Arcor2Session(logger);
        await InitSession();

        // The command loop
        while(true) {
            var command = ConsoleEx.ReadLinePrefix();
            if(command == null) {
                break;
            }

            #if DEBUG
                await ParseCommand(command);
            #else
                try {
                    await ParseCommand(command);
                }
                catch(Exception ex) {
                    ConsoleEx.WriteLineColor($"> {ex.Message}", ConsoleColor.Red);
                }
            #endif
        }

        // Gracefully Terminate
        await session.CloseAsync();
    }

    private static void ConfigureGracefulTermination() {
        // SIGINT
        Console.CancelKeyPress += (_, args) => {
            args.Cancel = true;
            CloseSession();
        };
        // SIGTERM
        AppDomain.CurrentDomain.ProcessExit += (_, args) => {
            CloseSession();

        };
        return;
        // Termination function
        void CloseSession() {
            if(Interlocked.CompareExchange(ref _isTerminating, 1, 0) == 0) {
                ConsoleEx.WriteLinePrefix("Closing the session...");
                if(session != null! && session.State == Arcor2SessionState.Open) {
                    session.CloseAsync().GetAwaiter().GetResult();
                }
            }
        }
    }

    private static Options? StartUp() {
        ConsoleEx.WriteLinePrefix("This is a simple showcase client for the Arcor2.ClientSdk.ClientServices library.");
        ConsoleEx.WriteLinePrefix("See the source code for insight on usage.");
        ConsoleEx.WriteLinePrefix("Enable debug logger? (Y/N)");
        var enableLoggerResponse = ConsoleEx.ReadLinePrefix();
        if(enableLoggerResponse == null) {
            return null;
        }
        var enableLogger = enableLoggerResponse.ToLower().First() is 'y';
        ConsoleEx.WriteLinePrefix($"Proceeding with logger {(enableLogger ? "enabled" : "disabled")}.");

        return new Options {
            EnableConsoleLogger = enableLogger
        };
    }

    private static async Task ParseCommand(string text) {
        var parts = text.Split(" ");
        var command = parts[0];
        var args = parts[1..];
        switch(command) {
            case "!object_types":
                foreach(var type in session.ObjectTypes) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(type));
                }
                break;
            case "!scenes":
                await session.LoadScenes();
                foreach(var type in session.Scenes) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(type));
                }
                break;
            case "!rename_scene":
                await session.RenameScene(args[0], args[1]);
                break;
        }
    }

    /// <summary>
    /// Initializes a session (connection, gets username, registers needed handlers).
    /// </summary>
    public static async Task InitSession() {
        await session.ConnectAsync("127.0.0.1", 6789);
        ConsoleEx.WriteLinePrefix("Connected to localhost:6789. Input your username:");
        var name = ConsoleEx.ReadLinePrefix();
        var info = await session.InitializeAsync(name!);
        ConsoleEx.WriteLinePrefix($"Registered successfully. Server v{info.VarVersion}, API v{info.ApiVersion}.");

        session.OnConnectionClosed += (sender, args) => {
            ConsoleEx.WriteLinePrefix("Session closed.");
            Environment.Exit(0);
        };
    }
}