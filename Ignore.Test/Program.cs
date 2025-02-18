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
        var options = await StartUpAsync(); // Make this async too
        if(options == null) {
            return;
        }

        // Init connection, init messages, login..
        var logger = options.EnableConsoleLogger ? new ConsoleLogger() : null;
        session = new Arcor2Session(logger);
        try {
            await InitSession();
        }
        catch(ArgumentNullException ex) {
            await session.CloseAsync();
            return;
        }

        // Console.ReadLine() is infamous for being a blocking operation
        // Make sure to never block the main thread (in this case), so ping-pongs and messages
        // in general keep working
        using var cts = new CancellationTokenSource();
        var commandLoopTask = Task.Run(async () => {
            while(!cts.Token.IsCancellationRequested) {
                if(Console.KeyAvailable) {
                    var command = ConsoleEx.ReadLinePrefix();
                    if(command == null) {
                        await cts.CancelAsync();
                        break;
                    }
                    try {
                        await ParseCommand(command);
                    }
                    catch(Arcor2Exception ex) {
                        ConsoleEx.WriteLineColor($"> {ex.Message}", ConsoleColor.Red);
                    }
                }
                await Task.Delay(50, cts.Token); // Small delay to prevent CPU spinning
            }
        });

        try {
            await commandLoopTask;
        }
        finally {
            await session.CloseAsync();
        }
    }

    // Make StartUp async as well
    private static async Task<Options?> StartUpAsync() {
        ConsoleEx.WriteLinePrefix("This is a simple showcase client for the Arcor2.ClientSdk.ClientServices library.");
        ConsoleEx.WriteLinePrefix("See the source code for insight on usage.");
        ConsoleEx.WriteLinePrefix("Enable debug logger? (Y/N)");

        // Use a timeout for the initial setup question
        var response = await Task.Run(() => ConsoleEx.ReadLinePrefix());
        if(response == null) {
            return null;
        }

        var enableLogger = response.ToLower().First() is 'y';
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
            // Util commands
            case "!help":
                PrintHelp();
                break;
            // Read commands
            case "!ns" or "!navigation_state":
                Console.WriteLine($"{Enum.GetName(session.NavigationState)} {(session.NavigationId is null ? "" : "(" + session.NavigationId + ")")}");
                break;
            case "!ot" or "!object_types":
                foreach(var type in session.ObjectTypes) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(type, objectName: type.Meta.Type));
                }
                break;
            case "!s" or "!scenes":
                foreach(var sceneData in session.Scenes) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(sceneData, objectName: sceneData.Meta.Name));
                }
                break;
            // RPC commands
            case "!rs" or "!reload_scenes":
                await session.ReloadScenesAsync();
                break;
            case "!usd" or "!update_scene_desc":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == args[0])!.UpdateDescriptionAsync(args[1]);
                break;
            case "!rename_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == args[0])!.RenameAsync(args[1]);
                break;
            case "!duplicate_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == args[0])!.DuplicateAsync(args[1]);
                break;
            case "!new_scene":
                await session.AddNewSceneAsync(args[0], args.Length > 1 ? args[1] : string.Empty);
                break;
            case "!remove_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == args[0])!.RemoveAsync();
                break;
            case "!os" or "!open_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == args[0])!.OpenAsync();
                break;
            case "!ls" or "!load_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == args[0])!.LoadAsync();
                break;
            case "!cs" or "!close_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.CloseAsync(args.Length > 0  && args[0] == "force");
                break;
            case "!ss" or "!save_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.SaveAsync();
                break;
            case "!start_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.StartAsync();
                break;
            case "!stop_scene":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.StopAsync();
                break;

        }
    }

    private static void PrintHelp() {
        ConsoleEx.WriteLinePrefix(
            """
            The following commands are available:
            !help - Prints this message.
            !navigation_state - Gets the current navigation object (menu/scene/etc...)
            !object_types - List all object types and their actions.
            !scenes - Lists all in-memory scenes.
            !reload_scenes - Loads scenes.
            !rename_scene <ID> <NEW_NAME> - Renames a scene.
            !new_scene <NAME> [DESCRIPTION] - Creates a new scene.
            !update_scene_desc <ID> <NEW_DESCRIPTION> - Updates a description of the scene.
            !duplicate_scene <ID> <NEW_NAME> - Duplicates a scene.
            !remove_scene <ID> - Deletes a scene.
            !open_scene <ID> - Opens a scene.
            !load_scene <ID> - Loads all information about a scene.
            !close_scene ["force"] - Closes a scene.
            !save_scene - Saves the currently opened scene.
            !start_scene - Starts the currently opened scene.
            !stop_scene - Stops the currently opened scene.
            """);
    }

    private static void ConfigureGracefulTermination() {
        // SIGINT
        Console.CancelKeyPress += async (_, args) => {
            args.Cancel = true;
            await CloseSessionAsync();
        };

        // SIGTERM
        AppDomain.CurrentDomain.ProcessExit += (_, args) => {
            // For ProcessExit we need to block since the process is shutting down
            CloseSessionAsync().GetAwaiter().GetResult();
        };
        return;

        // Termination function
        async Task CloseSessionAsync() {
            if(Interlocked.CompareExchange(ref _isTerminating, 1, 0) == 0) {
                ConsoleEx.WriteLinePrefix("Closing the session...");
                if(session != null! && session.ConnectionState == Arcor2SessionState.Open) {
                    await session.CloseAsync();
                }
            }
        }
    }

    /// <summary>
    /// Initializes a session (connection, gets username, registers needed handlers).
    /// </summary>
    public static async Task<bool> InitSession() {
        await session.ConnectAsync("127.0.0.1", 6789);
        ConsoleEx.WriteLinePrefix("Connected to localhost:6789. Input your username:");

        string? name = await Task.Run(() => ConsoleEx.ReadLinePrefix());
        ArgumentNullException.ThrowIfNull(name);

        var info = await session.InitializeAsync(name);
        ConsoleEx.WriteLinePrefix($"Registered successfully. Server v{info.VarVersion}, API v{info.ApiVersion}.");

        var shutdownTcs = new TaskCompletionSource<bool>();

        session.OnConnectionClosed += (sender, args) => {
            ConsoleEx.WriteLinePrefix("Session closed.");
            shutdownTcs.TrySetResult(true);
            Environment.Exit(0);
        };

        return true;
    }
}