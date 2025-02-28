using System.Collections.ObjectModel;
using Arcor2.ClientSdk.ClientServices;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Ignore.Test.Models;
using Ignore.Test.Output;
using Newtonsoft.Json;
using Joint = Arcor2.ClientSdk.Communication.OpenApi.Models.Joint;

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
        catch(ArgumentNullException) {
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
        await session.RegisterAndSubscribeAsync(name);
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

        // Helper method to get the current scene
        SceneManager GetCurrentScene() =>
            session.NavigationState == NavigationState.Scene
                ? session.Scenes.FirstOrDefault(s => s.Id == session.NavigationId)! 
                : session.NavigationState == NavigationState.Project 
                ? session.Projects.FirstOrDefault(s => s.Id == session.NavigationId)!.Scene
                : session.NavigationState == NavigationState.Package 
                ? session.Packages.FirstOrDefault(s => s.Id == session.NavigationId)!.Project.Scene
                : throw new Exception("Bad navigation state.");

        // Helper method to get action objects for current scene/project
        ObservableCollection<ActionObjectManager> GetActionObjects() =>
            session.NavigationState == NavigationState.Scene
                ? session.Scenes.FirstOrDefault(s => s.Id == session.NavigationId)!.ActionObjects
                : session.NavigationState == NavigationState.Project
                    ? session.Projects.FirstOrDefault(s => s.Id == session.NavigationId)!.Scene.ActionObjects
                    : session.NavigationState == NavigationState.Package
                        ? session.Packages.FirstOrDefault(s => s.Id == session.NavigationId)!.Project.Scene.ActionObjects
                        : throw new Exception("Bad navigation state.");

        // Helper method to get the current project
        ProjectManager GetCurrentProject() =>
            session.Projects.FirstOrDefault(s => s.Id == session.NavigationId)!;

        // Helper method to create a Position from args
        Position ParsePosition(int startIndex) =>
            new(
                Convert.ToDecimal(args[startIndex]),
                Convert.ToDecimal(args[startIndex + 1]),
                Convert.ToDecimal(args[startIndex + 2]));

        // Helper method to create an Orientation from args
        Orientation ParseOrientation(int startIndex) =>
            new(
                Convert.ToDecimal(args[startIndex]),
                Convert.ToDecimal(args[startIndex + 1]),
                Convert.ToDecimal(args[startIndex + 2]),
                Convert.ToDecimal(args[startIndex + 3]));

        // Helper method to create a Pose from args
        Pose ParsePose(int startIndex) => new(ParsePosition(startIndex), ParseOrientation(startIndex + 3));

        switch(command) {
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
                foreach(var type in session.ObjectTypes) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(type, objectName: type.Data.Meta.Type));
                }
                break;

            case "!s" or "!scenes":
                foreach(var sceneData in session.Scenes) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(sceneData, objectName: sceneData.Data.Name));
                }
                break;

            case "!p" or "!projects":
                foreach(var projectData in session.Projects) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(projectData, objectName: projectData.Data.Name));
                }
                break;

            // Object Type RPC commands
            case "!aot" or "!add_object_type":
                await session.CreateObjectTypeAsync(
                    JsonConvert.DeserializeObject<ObjectTypeMeta>(string.Join(' ', args))!);
                break;

            case "!rot" or "!remove_object_type":
                await session.ObjectTypes.FirstOrDefault(s => s.Id == args[0])!.DeleteAsync();
                break;

            case "!update_object_model_box":
                await session.ObjectTypes.FirstOrDefault(s => s.Id == args[0])!.UpdateObjectModel(
                    new BoxCollisionModel(Convert.ToDecimal(args[1]), Convert.ToDecimal(args[2]),
                        Convert.ToDecimal(args[3])));
                break;

            // Scene RPC commands
            case "!action_objects":
                foreach(var actionObject in GetActionObjects()) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(actionObject, objectName: actionObject.Data.Meta.Type));
                }
                break;

            case "!rs" or "!reload_scenes":
                await session.ReloadScenesAsync();
                break;

            case "!usd" or "!update_scene_desc":
                await session.Scenes.FirstOrDefault(s => s.Id == args[0])!.UpdateDescriptionAsync(args[1]);
                break;

            case "!rename_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == args[0])!.RenameAsync(args[1]);
                break;

            case "!duplicate_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == args[0])!.DuplicateAsync(args[1]);
                break;

            case "!new_scene":
                await session.CreateSceneAsync(args[0], args.Length > 1 ? args[1] : string.Empty);
                break;

            case "!remove_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
                break;

            case "!os" or "!open_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == args[0])!.OpenAsync();
                break;

            case "!ls" or "!load_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == args[0])!.LoadAsync();
                break;

            case "!cs" or "!close_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == session.NavigationId)!.CloseAsync(
                    args.Length > 0 && args[0] == "force");
                break;

            case "!ss" or "!save_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == session.NavigationId)!.SaveAsync();
                break;

            case "!start_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == session.NavigationId)!.StartAsync();
                break;

            case "!stop_scene":
                await session.Scenes.FirstOrDefault(s => s.Id == session.NavigationId)!.StopAsync();
                break;

            // Action Object RPCs
            case "!aac" or "!add_action_object":
                var scene = GetCurrentScene();
                var pose = ParsePose(2);

                if(args.Length > 9) {
                    await scene.AddActionObjectAsync(
                        args[0], args[1], pose,
                        JsonConvert.DeserializeObject<List<Parameter>>(args[9])!);
                }
                else {
                    await scene.AddActionObjectAsync(args[0], args[1], pose);
                }
                break;

            case "!remove_action_object":
                await GetCurrentScene().ActionObjects!
                    .FirstOrDefault(o => o.Id == args[0])!.RemoveAsync(args.Length > 1 && args[1] == "force");
                break;

            case "!add_virtual_box":
                await GetCurrentScene().AddVirtualCollisionBoxAsync(
                    args[0],
                    ParsePose(1),
                    new BoxCollisionModel(Convert.ToDecimal(args[8]), Convert.ToDecimal(args[9]), Convert.ToDecimal(args[10])));
                break;

            case "!add_virtual_cylinder":
                await GetCurrentScene().AddVirtualCollisionCylinderAsync(
                    args[0],
                    ParsePose(1),
                    new CylinderCollisionModel(Convert.ToDecimal(args[8]), Convert.ToDecimal(args[9])));
                break;

            case "!add_virtual_sphere":
                await GetCurrentScene().AddVirtualCollisionSphereAsync(
                    args[0],
                    ParsePose(1),
                    new SphereCollisionModel(Convert.ToDecimal(args[8])));
                break;

            case "!add_virtual_mesh":
                //...
                break;

            case "!update_action_object_pose" or "!uaop":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.UpdatePoseAsync(ParsePose(1));
                break;

            case "!rename_action_object":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.RenameAsync(args[1]);
                break;

            case "!update_action_object_parameters":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.UpdateParametersAsync(
                        JsonConvert.DeserializeObject<List<Parameter>>(string.Join(' ', args.Skip(1)))!);
                break;

            case "!move_to_pose":
                if (args.Length > 11) {
                    await GetActionObjects()
                        .FirstOrDefault(o => o.Id == args[0])!.MoveToPoseAsync(
                            args[0],
                            ParsePose(2),
                            safe: Convert.ToBoolean(args[9]),
                            linear: Convert.ToBoolean(args[10]),
                            speed: Convert.ToDecimal(args[11]),
                            armId: args.Length > 12 ? args[12] : null!);
                }
                else {
                    await GetActionObjects()
                        .FirstOrDefault(o => o.Id == args[0])!.MoveToPoseAsync(
                            ParsePose(1),
                            safe: Convert.ToBoolean(args[8]),
                            linear: Convert.ToBoolean(args[9]),
                            speed: Convert.ToDecimal(args[10]));
                }
                break;

            case "!move_to_joints":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.MoveToActionPointJointsAsync(
                        args[1],
                        safe: Convert.ToBoolean(args[2]),
                        linear: Convert.ToBoolean(args[3]),
                        speed: Convert.ToDecimal(args[4]),
                        endEffectorId: args.Length > 5 ? args[5] : "default",
                        armId: args.Length > 6 ? args[6] : null!);
                break;

            case "!move_to_orientation":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.MoveToActionPointOrientationAsync(
                        args[1],
                        safe: Convert.ToBoolean(args[2]),
                        linear: Convert.ToBoolean(args[3]),
                        speed: Convert.ToDecimal(args[4]),
                        endEffectorId: args.Length > 5 ? args[5] : "default",
                        armId: args.Length > 6 ? args[6] : null!);
                break;

            case "!step_position":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.StepPositionAsync(
                        Enum.Parse<Axis>(args[1]),
                        Convert.ToDecimal(args[2]),
                        safe: Convert.ToBoolean(args[3]),
                        linear: Convert.ToBoolean(args[4]),
                        speed: Convert.ToDecimal(args[5]),
                        endEffectorId: args.Length > 6 ? args[6] : "default",
                        armId: args.Length > 7 ? args[7] : null!);
                break;

            case "!step_orientation":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.StepOrientationAsync(
                        Enum.Parse<Axis>(args[1]),
                        Convert.ToDecimal(args[2]),
                        safe: Convert.ToBoolean(args[3]),
                        linear: Convert.ToBoolean(args[4]),
                        speed: Convert.ToDecimal(args[5]),
                        endEffectorId: args.Length > 6 ? args[6] : "default",
                        armId: args.Length > 7 ? args[7] : null!);
                break;

            case "!set_eef_perpendicular_to_world":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.SetEndEffectorPerpendicularToWorldAsync();
                break;

            case "!set_hand_teaching_mode":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.SetHandTeachingModeAsync(Convert.ToBoolean(args[1]));
                break;
            case "!forward_kinematics":
                var forwardKinematics = await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.GetForwardKinematicsAsync();
                Console.WriteLine(ReflectionHelper.FormatObjectProperties(forwardKinematics));
                break;
            case "!inverse_kinematics":
                var inverseKinematics = await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.GetInverseKinematicsAsync();
                foreach (var inverseKinematic in inverseKinematics) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(inverseKinematic));
                }
                break;
            case "!calibrate_robot":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.CalibrateRobotAsync(args[1], Convert.ToBoolean(args[2]));
                break;
            case "!calibrate_camera":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.CalibrateCameraAsync();
                break;
            case "!get_camera_color_image":
#pragma warning disable CS0618 // Type or member is obsolete
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.GetCameraColorImageAsync();
#pragma warning restore CS0618 // Type or member is obsolete
                break;
            case "!get_camera_color_parameters":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.GetCameraColorParametersAsync();
                break;
            case "!stop_robot":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!.StopAsync();
                break;
            case "!action_object_param_value":
                await GetActionObjects()
                    .FirstOrDefault(o => o.Id == args[0])!
                    .GetParameterValuesAsync(args[1]);
                break;
            // Project RPC commands
            case "!rp" or "!reload_projects":
                await session.ReloadProjectsAsync();
                break;

            case "!new_project":
                await session.CreateProjectAsync(
                    args[0],
                    args[1],
                    args.Length > 3 ? args[3] : string.Empty,
                    Convert.ToBoolean(args[2]));
                break;

            case "!upd" or "!update_project_desc":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.UpdateDescriptionAsync(args[1]);
                break;

            case "!rename_project":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.RenameAsync(args[1]);
                break;

            case "!duplicate_project":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.DuplicateAsync(args[1]);
                break;

            case "!remove_project":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
                break;

            case "!op" or "!open_project":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.OpenAsync();
                break;

            case "!lp" or "!load_project":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.LoadAsync();
                break;

            case "!cp" or "!close_project":
                await GetCurrentProject().CloseAsync(args.Length > 0 && args[0] == "force");
                break;

            case "!sp" or "!save_project":
                await GetCurrentProject().SaveAsync();
                break;

            case "!start_project":
                await GetCurrentProject().Scene.StartAsync();
                break;

            case "!stop_project":
                await GetCurrentProject().Scene.StopAsync();
                break;

            case "!set_project_has_logic":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.SetHasLogicAsync(Convert.ToBoolean(args[1]));
                break;

            case "!build_project":
                await session.Projects.FirstOrDefault(s => s.Id == args[0])!.BuildIntoPackageAsync(args[1]);
                break;
            case "!build_project_temp":
                await GetCurrentProject().BuildIntoTemporaryPackageAndRunAsync(Convert.ToBoolean(args[0]), args.Skip(1).ToList());
                break;

            // Project parameter
            case "!parameters":
                foreach(var param in GetCurrentProject().Parameters!) {
                    Console.WriteLine(
                        ReflectionHelper.FormatObjectProperties(param, objectName: param.Data.Name));
                }
                break;

            case "!add_project_parameter":
                await GetCurrentProject().AddProjectParameterAsync(args[0], args[1], args[2]);
                break;

            case "!update_project_parameter_value":
                await GetCurrentProject().Parameters!
                    .FirstOrDefault(s => s.Id == args[0])!.UpdateValueAsync(args[1]);
                break;

            case "!update_project_parameter_name":
                await GetCurrentProject().Parameters!
                    .FirstOrDefault(s => s.Id == args[0])!.UpdateNameAsync(args[1]);
                break;

            case "!remove_project_parameter":
                await GetCurrentProject().Parameters!
                    .FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
                break;

            // Project overrides
            case "!overrides":
                foreach(var @override in GetCurrentProject().Overrides!) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(@override));
                }
                break;

            case "!add_project_override":
                await GetCurrentProject().AddOverrideAsync(
                    args[0],
                    new Parameter(args[1], args[2], args[3]));
                break;

            case "!update_project_override":
                await GetCurrentProject().Overrides!
                    .FirstOrDefault(s => s.Data.ActionObjectId == args[0] && s.Data.Parameter.Name == args[1])!
                    .UpdateAsync(new Parameter(args[1], args[2], args[3]));
                break;

            case "!remove_project_override":
                await GetCurrentProject().Overrides!
                    .FirstOrDefault(s => s.Data.ActionObjectId == args[0] && s.Data.Parameter.Name == args[1])!
                    .RemoveAsync();
                break;

            // Action points
            case "!ap" or "!action_points":
                foreach(var actionPoint in GetCurrentProject().ActionPoints!) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(actionPoint));
                }
                break;

            case "!add_ap":
                if(args.Length > 4) {
                    await GetCurrentProject().AddActionPointAsync(
                        args[0],
                        ParsePosition(1),
                        args[4]);
                }
                else {
                    await GetCurrentProject().AddActionPointAsync(
                        args[0],
                        ParsePosition(1));
                }
                break;

            case "!add_ap_using_robot":
                await GetCurrentProject().AddActionPointUsingRobotAsync(
                    args[0],
                    args[1],
                    args.Length > 2 ? args[2] : "default",
                    args.Length > 3 ? args[3] : null);
                break;

            case "!duplicate_ap":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .DuplicateAsync(ParsePosition(1));
                break;

            case "!rename_ap":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .RenameAsync(args[1]);
                break;

            case "!update_ap_parent":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .UpdateParentAsync(args[1]);
                break;

            case "!update_ap_position":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .UpdatePositionAsync(ParsePosition(1));
                break;

            case "!remove_ap":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .RemoveAsync();
                break;

            case "!update_ap_using_robot":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .UpdateUsingRobotAsync(
                        args[1],
                        args.Length > 2 ? args[2] : "default",
                        args.Length > 3 ? args[3] : null);
                break;

            // Actions 
            case "!actions":
                foreach(var action in GetCurrentProject().ActionPoints!
                             .FirstOrDefault(a => a.Id == args[0])!.Actions!) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(action));
                }
                break;

            case "!add_action":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!
                    .AddActionAsync(
                        args[1],
                        args[2],
                        [new Flow(Flow.TypeEnum.Default, [])],
                        []);
                break;

            case "!update_action_parameters":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Actions
                    .FirstOrDefault(a => a.Id == args[1])!
                    .UpdateParametersAsync(
                        JsonConvert.DeserializeObject<List<ActionParameter>>(string.Join(' ', args.Skip(2)))!);
                break;

            case "!update_action_flows":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Actions
                    .FirstOrDefault(a => a.Id == args[1])!
                    .UpdateFlowsAsync(
                        JsonConvert.DeserializeObject<List<Flow>>(string.Join(' ', args.Skip(2)))!);
                break;

            case "!rename_action":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Actions
                    .FirstOrDefault(a => a.Id == args[1])!
                    .RenameAsync(args[2]);
                break;

            case "!remove_action":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Actions
                    .FirstOrDefault(a => a.Id == args[1])!
                    .RemoveAsync();
                break;

            case "!execute_action":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Actions
                    .FirstOrDefault(a => a.Id == args[1])!
                    .ExecuteAsync();
                break;

            case "!cancel_action":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Actions
                    .FirstOrDefault(a => a.Id == args[1])!
                    .CancelAsync();
                break;

            // Orientation 
            case "!orientations":
                foreach(var orientation in GetCurrentProject().ActionPoints!
                             .FirstOrDefault(a => a.Id == args[0])!.Orientations!) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(orientation));
                }
                break;

            case "!add_orientation":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!
                    .AddOrientationAsync(
                        ParseOrientation(1),
                        args[5]);
                break;

            case "!add_orientation_using_robot":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!
                    .AddOrientationUsingRobotAsync(
                        args[1],
                        args.Length > 3 ? args[3] : "default",
                        args.Length > 4 ? args[4] : null,
                        args[2]);
                break;

            case "!update_orientation":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Orientations
                    .FirstOrDefault(a => a.Id == args[1])!
                    .UpdateAsync(ParseOrientation(2));
                break;

            case "!update_orientation_using_robot":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Orientations
                    .FirstOrDefault(a => a.Id == args[1])!
                    .UpdateUsingRobotAsync(
                        args[2],
                        args.Length > 3 ? args[3] : "default",
                        args.Length > 4 ? args[4] : null);
                break;

            case "!rename_orientation":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Orientations
                    .FirstOrDefault(a => a.Id == args[1])!
                    .RenameAsync(args[2]);
                break;

            case "!remove_orientation":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Orientations
                    .FirstOrDefault(a => a.Id == args[1])!
                    .RemoveAsync();
                break;

            // Joints 
            case "!joints":
                foreach(var joint in GetCurrentProject().ActionPoints!
                             .FirstOrDefault(a => a.Id == args[0])!.Joints!) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(joint));
                }
                break;

            case "!add_joints_using_robot":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!
                    .AddJointsUsingRobotAsync(
                        args[1],
                        args.Length > 3 ? args[3] : "default",
                        args.Length > 4 ? args[4] : null,
                        args[2]);
                break;

            case "!update_joints":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Joints
                    .FirstOrDefault(a => a.Id == args[1])!
                    .UpdateAsync(
                        JsonConvert.DeserializeObject<List<Joint>>(string.Join("", args.Skip(2)))!);
                break;

            case "!update_joints_using_robot":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Joints
                    .FirstOrDefault(a => a.Id == args[1])!
                    .UpdateUsingRobotAsync();
                break;

            case "!rename_joints":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Joints
                    .FirstOrDefault(a => a.Id == args[1])!
                    .RenameAsync(args[2]);
                break;

            case "!remove_joints":
                await GetCurrentProject().ActionPoints!
                    .FirstOrDefault(a => a.Id == args[0])!.Joints
                    .FirstOrDefault(a => a.Id == args[1])!
                    .RemoveAsync();
                break;

            // Logic items
            case "!logic_items":
                foreach(var logicItem in GetCurrentProject().LogicItems!) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(logicItem));
                }
                break;

            case "!add_logic_item":
                await GetCurrentProject().AddLogicItem(
                    args[0],
                    args[1],
                    args.Length > 2 ? new ProjectLogicIf(args[2], args[3]) : null);
                break;

            case "!update_logic_item":
                await GetCurrentProject().LogicItems!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .UpdateAsync(
                        args[1],
                        args[2],
                        args.Length > 3 ? new ProjectLogicIf(args[3], args[4]) : null);
                break;

            case "!remove_logic_item":
                await GetCurrentProject().LogicItems!
                    .FirstOrDefault(s => s.Id == args[0])!
                    .RemoveAsync();
                break;

            // Packages
            case "!packages":
                foreach(var package in session.Packages) {
                    Console.WriteLine(ReflectionHelper.FormatObjectProperties(package));
                }
                break;

            case "!rename_package":
                await session.Packages.FirstOrDefault(s => s.Id == args[0])!.RenameAsync(args[1]);
                break;

            case "!remove_package":
                await session.Packages.FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
                break;

            case "!run_package":
                await session.Packages.FirstOrDefault(s => s.Id == args[0])!
                    .RunAsync(Convert.ToBoolean(args[1]), args.Skip(2).ToList());
                break;

            case "!stop_package":
                await session.Packages.FirstOrDefault(s => s.Id == session.NavigationId)!.StopAsync();
                break;

            case "!resume_package":
                await session.Packages.FirstOrDefault(s => s.Id == session.NavigationId)!.ResumeAsync();
                break;

            case "!pause_package":
                await session.Packages.FirstOrDefault(s => s.Id == session.NavigationId)!.PauseAsync();
                break;
            case "!step_package":
                await session.Packages.FirstOrDefault(s => s.Id == session.NavigationId)!.StepAsync();
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
            !move_to_pose <ID> <POSX> <POSY> <POSZ> 
                          <ORIENTX> <ORIENTY> <ORIENTZ> <ORIENTW>
                          <SAFE_BOOL> <LINEAR_BOOL> <SPEED> [EEF_ID] [ARM_ID] - Moves the robot into a pose.
            !move_to_orientation <ID> <ORIENT_ID>
                                <SAFE_BOOL> <LINEAR_BOOL> <SPEED> [EEF_ID] [ARM_ID] 
                                - Moves the robot into a pose.
            !move_to_joints <ID> <JOINTS_ID>
                            <SAFE_BOOL> <LINEAR_BOOL> <SPEED> [EEF_ID] [ARM_ID] 
                            - Moves the robot into a pose.
            !step_position <ID> <AXIS> <STEP>
                            <SAFE_BOOL> <LINEAR_BOOL> <SPEED> [EEF_ID] [ARM_ID] 
                            -Steps the robot position.
            !step_orientation <ID> <AXIS> <STEP>
                        <SAFE_BOOL> <LINEAR_BOOL> <SPEED> [EEF_ID] [ARM_ID] 
                        - Steps the robot position.
            !set_eef_perpendicular_to_world <ID>
            !set_hand_teaching_mode <ID> ["true"/"false"]
            !forward_kinematics <ID>
            !inverse_kinematics <ID>
            !calibrate_robot <ID> <CAMERA_ID> <MOVE_TO_CAL_POSE_BOOL>
            !calibrate_camera <ID>
            !get_camera_color_image <ID>
            !get_camera_color_parameters <ID>
            !stop_robot <ID>
            
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
            !build_project_temp <START_STOPPED_BOOL> [BREAKPOINTS...] - Builds a project into temp package and runs it.
            
            - Project Parameters -
            !parameters - Lists project parameters for a project.
            !add_project_parameter <NAME> <TYPE> <VALUE> - Adds a new project parameter.
            !update_project_parameter_value <ID> <VALUE> - Changes the value of project parameter.
            !update_project_parameter_name <ID> <Name> - Changes the name of project parameter.
            !remove_project_parameter <ID> - Removes a project parameter.
            
            - Project Overrides -
            !overrides - Lists the project overrides.
            !add_project_override <ACTION_OBJECT_ID> <NAME> <TYPE> <VALUE> - Adds a new project override.
            !update_project_override <ACTION_OBJECT_ID> <NAME> <TYPE> <VALUE> - Updates a project override.
            !remove_project_override <ACTION_OBJECT_ID> <NAME> - Removes a project override.
            
            - Action Points -
            !action_points - Lists the action points.
            !add_ap <NAME> <POSX> <POSY> <POSZ> [PARENT_ID] - Adds an action point.
            !add_ap_using_robot <NAME> <ROBOT_ID> [END_EFFECTOR_ID] [ARM_ID] - Adds an action point using robot.
            !duplicate_ap <ID> <POSX> <POSY> <POSZ> - Duplicates an action point.
            !rename_ap <ID> <NEW_NAME> - Renames an action point.
            !update_ap_parent <ID> <PARENT_ID> - Updates the action point parent.
            !update_ap_position <ID> <POSX> <POSY> <POSZ> - Updates action point position.
            !update_ap_using_robot <ID> <ROBOT_ID> [END_EFFECTOR_ID] [ARM_ID] - Updates action point using robot.
            !remove_ap <ID> - Removes action point.
            
            - Actions -
            !actions <AP_ID> - List actions of action point.
            !add_action <AP_ID> <NAME> <TYPE> - Adds a new action (TYPE = '{ActionObjectId}/{ActionId}')
            !update_action_parameters <AP_ID> <ID> <PARAMS_AS_JSON_LIST> - Updates action parameters.
            !update_action_flows <AP_ID>< <ID> <FLOWS_AS_JSON_LIST> - Updates action flows.
            !rename_action <AP_ID> <ID> <NEW_NAME> - Renames an action.
            !remove_action <AP_ID> <ID> - Removes the action
            !execute_action <AP_ID> <ID>
            !cancel_action <AP_ID> <ID>
            
            - Orientations -
            !orientations <AP_ID> - Lists all orientations.
            !add_orientation <AP_ID> <X> <Y> <Z> <W> <NAME> - Adds an orientation.
            !add_orientation_using_robot <AP_ID> <NAME> <ROBOT_ID> [END_EFFECTOR_ID] [ARM_ID] - Adds an orientation using robot.
            !update_orientation <AP_ID> <ID> <X> <Y> <Z> <W> <NAME> - Updates an orientation.
            !update_orientation_using_robot <AP_ID> <ID> <ROBOT_ID> [END_EFFECTOR_ID] [ARM_ID] - Adds an orientation using robot.
            !rename_orientation <AP_ID> <ID> <NEW_NAME> - Renames an orientation.
            !remove_orientation <AP_ID> <ID> - Removes the orientation.
            
            - Joints -
            !joints <AP_ID> - Lists all joints.
            !add_joints_using_robot <AP_ID> <NAME> <ROBOT_ID> [END_EFFECTOR_ID] [ARM_ID] - Adds joints using robot.
            !update_joints <AP_ID> <ID> <JOINT_LIST_AS_JSON> - Updates the joints.
            !update_joints_using_robot <AP_ID> <ID> - Adds joints using robot.
            !rename_joints <AP_ID> <ID> <NEW_NAME> - Renames joints.
            !remove_joints <AP_ID> <ID> - Removes the joints.
            
            - Logic Items -
            !logic_items - Lists all logic items.
            !add_logic_item <START> <END> [WHAT VALUE] - Creates a logic item with optional condition.
            !update_logic_item <ID> <START> <END> [WHAT VALUE] - Updates a logic item with optional condition.
            !remove_logic_item <ID> - Removes a logic item.
            
            - Packages -
            !packages - Lists all packages
            !rename_package <ID> <NEW_NAME> - Renames a package.
            !remove_package <ID> <NEW_NAME> - Deletes a package.
            !run_package <ID> <START_STOPPED_BOOL> [BREAKPOINTS...] - Runs a package.
            !stop_package - Stops a package.
            !pause_package - Pauses a package.
            !resume_package - Resumes a package.
            !step_package - Steps onto the next action in pause package.
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