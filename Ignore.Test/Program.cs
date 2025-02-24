using Arcor2.ClientSdk.ClientServices;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Ignore.Test.Models;
using Ignore.Test.Output;
using Newtonsoft.Json;

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
                    catch (Arcor2Exception ex) {
                        ConsoleEx.WriteLineColor($"> SERVER ERROR: {ex.Message}", ConsoleColor.Red);
                    }
                    catch (Exception ex) {
                        ConsoleEx.WriteLineColor($"> INTERNAL ERROR: {ex.Message}", ConsoleColor.Red);
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

    private static async Task<Options?> StartUpAsync() {
        ConsoleEx.WriteLinePrefix("This is a simple showcase client for the Arcor2.ClientSdk.ClientServices library.");
        ConsoleEx.WriteLinePrefix("See the source code for insight on usage.");
        ConsoleEx.WriteLinePrefix("Enable debug logger? (Y/N)");

        // Use a timeout for the initial setup question
        var response = await Task.Run(ConsoleEx.ReadLinePrefix);
        if(response == null) {
            return null;
        }

        var enableLogger = response.ToLower().FirstOrDefault() is 'y';
        ConsoleEx.WriteLinePrefix($"Proceeding with logger {(enableLogger ? "enabled" : "disabled")}.");

        return new Options {
            EnableConsoleLogger = enableLogger
        };
    }

    /// <summary>
    /// Initializes a session (connection, gets username, registers needed handlers).
    /// </summary>
    public static async Task<bool> InitSession() {
        await session.ConnectAsync("127.0.0.1", 6789);
        ConsoleEx.WriteLinePrefix("Connected to localhost:6789.");
        var info = await session.InitializeAsync();
        ConsoleEx.WriteLinePrefix($"Session initialized. Server v{info.VarVersion}, API v{info.ApiVersion}.");

        ConsoleEx.WriteLinePrefix("Username?");
        string? name = await Task.Run(ConsoleEx.ReadLinePrefix);
        ArgumentNullException.ThrowIfNull(name);
        await session.RegisterAsync(name);
        ConsoleEx.WriteLinePrefix($"Registered as '{name}'");

        var shutdownTcs = new TaskCompletionSource<bool>();

        session.OnConnectionClosed += (sender, args) => {
            ConsoleEx.WriteLinePrefix("Session closed.");
            shutdownTcs.TrySetResult(true);
            Environment.Exit(0);
        };

        return true;
    }

    private static async Task ParseCommand(string text) {
        var parts = text.Split(" ");
        var command = parts[0];
        var args = parts[1..];

        switch (command) {
            // Util commands
            case "!help":
                PrintHelp();
                break;
            // Read commands
            case "!ns" or "!navigation_state":
                Console.WriteLine(
                    $"{Enum.GetName(session.NavigationState)} {(session.NavigationId is null ? "" : "(" + session.NavigationId + ")")}");
                break;
            case "!ot" or "!object_types":
                foreach (var type in session.ObjectTypes) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(type, objectName: type.Meta.Type));
                }

                break;
            case "!s" or "!scenes":
                foreach (var sceneData in session.Scenes) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(sceneData, objectName: sceneData.Meta.Name));
                }

                break;
            case "!p" or "!projects":
                foreach (var projectData in session.Projects) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(projectData, objectName: projectData.Meta.Name));
                }

                break;
            // Object Type RPC commands
            case "!aot" or "!add_object_type":
                await session.CreateObjectTypeAsync(
                    JsonConvert.DeserializeObject<ObjectTypeMeta>(string.Join(' ', args))!);
                break;
            case "!rot" or "!remove_object_type":
                await session.ObjectTypes.FirstOrDefault(s => s.Meta.Type == args[0])!.DeleteAsync();
                break;
            case "!update_object_model_box":
                await session.ObjectTypes.FirstOrDefault(s => s.Meta.Type == args[0])!.UpdateObjectModel(
                    new BoxCollisionModel(Convert.ToDecimal(args[1]), Convert.ToDecimal(args[2]),
                        Convert.ToDecimal(args[3])));
                break;
            // Scene RPC commands
            case "!action_objects":
                var actionObjects = session.NavigationState == NavigationState.Scene
                    ? session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!
                        .ActionObjects!
                    : session.Projects.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!
                        .ParentScene.ActionObjects;

                foreach (var actionObject in actionObjects!) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(actionObject, objectName: actionObject.Data.Type));
                }

                break;
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
                await session.CreateSceneAsync(args[0], args.Length > 1 ? args[1] : string.Empty);
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
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.CloseAsync(
                    args.Length > 0 && args[0] == "force");
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
            // Action Object RPCs
            case "!aac" or "!add_action_object":
                if (args.Length > 9) {
                    await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.AddActionObjectAsync(
                        args[0],
                        args[1],
                        new Pose(
                            new Position(Convert.ToDecimal(args[2]), Convert.ToDecimal(args[3]), Convert.ToDecimal(args[4])),
                            new Orientation(Convert.ToDecimal(args[5]), Convert.ToDecimal(args[6]), Convert.ToDecimal(args[7]), Convert.ToDecimal(args[8]))),
                        JsonConvert.DeserializeObject<List<Parameter>>(args[9])!);
                }
                else {
                    await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.AddActionObjectAsync(
                        args[0],
                        args[1],
                        new Pose(
                            new Position(Convert.ToDecimal(args[2]), Convert.ToDecimal(args[3]), Convert.ToDecimal(args[4])),
                            new Orientation(Convert.ToDecimal(args[5]), Convert.ToDecimal(args[6]), Convert.ToDecimal(args[7]), Convert.ToDecimal(args[8]))));
                }
                break;
            case "!remove_action_object":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.ActionObjects!
                    .FirstOrDefault(o => o.Data.Id == args[0])!.RemoveAsync(args.Length > 1 && args[1] == "force");
                break;
            case "!add_virtual_box":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!
                    .AddVirtualCollisionBoxAsync(
                        args[0],
                        new Pose(
                            new Position(Convert.ToDecimal(args[1]), Convert.ToDecimal(args[2]),
                                Convert.ToDecimal(args[3])),
                            new Orientation(Convert.ToDecimal(args[4]), Convert.ToDecimal(args[5]),
                                Convert.ToDecimal(args[6]), Convert.ToDecimal(args[7]))),
                        new BoxCollisionModel(Convert.ToDecimal(args[8]), Convert.ToDecimal(args[9]), Convert.ToDecimal(args[10])));
                break;
            case "!add_virtual_cylinder":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!
                    .AddVirtualCollisionCylinderAsync(
                        args[0],
                        new Pose(
                            new Position(Convert.ToDecimal(args[1]), Convert.ToDecimal(args[2]),
                                Convert.ToDecimal(args[3])),
                            new Orientation(Convert.ToDecimal(args[4]), Convert.ToDecimal(args[5]),
                                Convert.ToDecimal(args[6]), Convert.ToDecimal(args[7]))),
                        new CylinderCollisionModel(Convert.ToDecimal(args[8]), Convert.ToDecimal(args[9])));
                break;
            case "!add_virtual_sphere":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!
                    .AddVirtualCollisionSphereAsync(
                        args[0],
                        new Pose(
                            new Position(Convert.ToDecimal(args[1]), Convert.ToDecimal(args[2]),
                                Convert.ToDecimal(args[3])),
                            new Orientation(Convert.ToDecimal(args[4]), Convert.ToDecimal(args[5]),
                                Convert.ToDecimal(args[6]), Convert.ToDecimal(args[7]))),
                        new SphereCollisionModel(Convert.ToDecimal(args[8])));
                break;
            case "!add_virtual_mesh":
                //...
                break;
            case "!update_action_object_pose" or "!uaop":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.ActionObjects!
                    .FirstOrDefault(o => o.Data.Id == args[0])!.UpdatePoseAsync(new Pose(
                        new Position(Convert.ToDecimal(args[1]), Convert.ToDecimal(args[2]),
                            Convert.ToDecimal(args[3])),
                        new Orientation(Convert.ToDecimal(args[4]), Convert.ToDecimal(args[5]),
                            Convert.ToDecimal(args[6]), Convert.ToDecimal(args[7]))));
                break;
            case "!rename_action_object":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.ActionObjects!
                    .FirstOrDefault(o => o.Data.Id == args[0])!.RenameAsync(args[1]);
                break;
            case "!update_action_object_parameters":
                await session.Scenes.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.ActionObjects!
                    .FirstOrDefault(o => o.Data.Id == args[0])!.UpdateParametersAsync(JsonConvert.DeserializeObject<List<Parameter>>(string.Join(' ', args.Skip(1)))!);
                break;

            // Project RPC commands
            case "!rp" or "!reload_projects":
                await session.ReloadProjectsAsync();
                break;
            case "!new_project":
                await session.CreateProjectAsync(args[0], args[1], args.Length > 3 ? args[3] : string.Empty, Convert.ToBoolean(args[2]));
                break;
            case "!upd" or "!update_project_desc":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.UpdateDescriptionAsync(args[1]);
                break;
            case "!rename_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.RenameAsync(args[1]);
                break;
            case "!duplicate_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.DuplicateAsync(args[1]);
                break;
            case "!remove_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.RemoveAsync();
                break;
            case "!op" or "!open_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.OpenAsync();
                break;
            case "!lp" or "!load_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.LoadAsync();
                break;
            case "!cp" or "!close_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.CloseAsync(args.Length > 0 && args[0] == "force");
                break;
            case "!sp" or "!save_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.SaveAsync();
                break;
            case "!start_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.StartAsync();
                break;
            case "!stop_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == session.NavigationId)!.StopAsync();
                break;
            case "!set_project_has_logic":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.SetHasLogic(Convert.ToBoolean(args[1]));
                break;
            case "!build_project":
                await session.Projects.FirstOrDefault(s => s.Meta.Id == args[0])!.BuildIntoPackage(args[1]);
                break;
        }
    }

    private static void PrintHelp() {
        ConsoleEx.WriteLinePrefix(
            """
            The following commands are available:
            - Base -
            !help - Prints this message.
            !navigation_state - Gets the current navigation object (menu/scene/etc...)
            !object_types - List all object types and their actions.
            !scenes - Lists all in-memory scenes.
            !projects - Lists all in-memory projects.
            
            - Object Type -
            !add_object_type <META_AS_JSON> - Adds a new object type.
            !remove_object_type <TYPE> - Removes an object type.
            !update_object_model_box <TYPE> <X> <Y> <Z> - Updates objects model to a box.
            
            - Scene -
            !action_objects - Lists scene objects.
            !reload_scenes - Loads scenes.
            !rename_scene <ID> <NEW_NAME> - Renames a scene.
            !new_scene <NAME> [DESCRIPTION] - Creates a new scene.
            !update_scene_desc <ID> <NEW_DESCRIPTION> - Updates a description of a scene.
            !duplicate_scene <ID> <NEW_NAME> - Duplicates a scene.
            !remove_scene <ID> - Deletes a scene.
            !open_scene <ID> - Opens a scene.
            !load_scene <ID> - Loads all information about a scene.
            !close_scene ["force"] - Closes a scene.
            !save_scene - Saves the currently opened scene.
            !start_scene - Starts the currently opened scene.
            !stop_scene - Stops the currently opened scene.
            
            - Action Object  -
            !add_action_object <TYPE> <NAME> 
                               <POSX> <POSY> <POSZ> 
                               <ORIENTX> <ORIENTY> <ORIENTZ> <ORIENTW>
                               [PARAM_LIST_AS_JSON]
                               - Adds a new action object. Will use default parameters if not supplied.
            !add_virtual_box <NAME> 
                             <POSX> <POSY> <POSZ> 
                             <ORIENTX> <ORIENTY> <ORIENTZ> <ORIENTW>
                             <SIZEX> <SIZEY> <SIZEZ>
                             - Adds a box collision object.
            !add_virtual_cylinder   <NAME> 
                                    <POSX> <POSY> <POSZ> 
                                    <ORIENTX> <ORIENTY> <ORIENTZ> <ORIENTW>
                                    <RADIUS> <LEN>
                                     - Adds a cylinder collision object.
            !add_virtual_sphere     <NAME> 
                                    <POSX> <POSY> <POSZ> 
                                    <ORIENTX> <ORIENTY> <ORIENTZ> <ORIENTW>
                                    <RADIUS>
                                     - Adds a sphere collision object.
            !remove_action_object <ID> ["force"] - Removes an action object from scene.
            !update_action_object_pose <ID> <POSX> <POSY> <POSZ> 
                                       <ORIENTX> <ORIENTY> <ORIENTZ> <ORIENTW>
                                       - Updates a pose of an action object.
            !update_action_object_parameters <ID> <PARAM_LIST_AS_JSON>
                                        - Updates a list of parameters of an action object.
            !rename_action_object <ID> <NEW_NAME> - Renames an action object.
            
            - Project -
            !reload_projects - Loads projects.
            !new_project <SCENE_ID> <NAME> <HAS_LOGIC> [DESCRIPTION]
            !rename_project <ID> <NEW_NAME> - Renames a project.
            !update_project_desc <ID> <NEW_DESCRIPTION> - Updates a description of a project.
            !duplicate_project <ID> <NEW_NAME> - Duplicates a project.
            !remove_project <ID> - Deletes a project.
            !open_project <ID> - Opens a project.
            !load_project <ID> - Loads all information about a project.
            !close_project ["force"] - Closes a project.
            !save_project - Saves the currently opened project.
            !start_project - Starts the currently opened project.
            !stop_project - Stops the currently opened project.
            !set_project_has_logic <ID> <"true"/"false"> - Sets if the project should have logic.
            !build_project <ID> <PACKAGE_NAME> - Builds a project into package.
            """);
    }

    private static void ConfigureGracefulTermination() {
        AppDomain.CurrentDomain.ProcessExit += (_, args) => {
            // For ProcessExit we can block since the process is shutting down
            CloseSessionAsync().GetAwaiter().GetResult();
        };
        return;

        // Termination function
        async Task CloseSessionAsync() {
            if(Interlocked.CompareExchange(ref _isTerminating, 1, 0) == 0) {
                if(session != null! && session.ConnectionState == Arcor2SessionState.Open) {
                    ConsoleEx.WriteLinePrefix("Closing the session...");
                    await session.CloseAsync();
                }
            }
        }
    }
}