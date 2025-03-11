using System.Collections.ObjectModel;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Managers;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Ignore.Test.Models;
using Ignore.Test.Output;
using Newtonsoft.Json;
using Joint = Arcor2.ClientSdk.Communication.OpenApi.Models.Joint;

namespace Arcor2.ClientSdk.ClientServices.ConsoleTestApp;

/// <summary>
/// Showcase client application for the Arcor2.ClientSdk.ClientServices library.
/// </summary>
internal class Program {
    public static Arcor2Session Session = null!;
    private static int _isTerminating;

    private static async Task Main() {
        ConfigureGracefulTermination();

        // Initialize application and get configuration options
        var options = await InitializeApplicationAsync();
        if (options == null) {
            return;
        }

        // Set up session with appropriate logging
        var logger = options.EnableConsoleLogger ? new ConsoleLogger() : null;
        Session = new Arcor2Session(logger: logger);

        try {
            await EstablishSessionAsync();
        }
        catch (ArgumentNullException) {
            await Session.CloseAsync();
            return;
        }

        // Start command processing loop
        await RunCommandLoopAsync();
    }

    /// <summary>
    /// Displays welcome message and initializes application configuration.
    /// </summary>
    private static async Task<Options?> InitializeApplicationAsync() {
        ConsoleEx.WriteLinePrefix("This is a simple showcase client for the Arcor2.ClientSdk.ClientServices library.");
        ConsoleEx.WriteLinePrefix("See the source code for insight on usage.");
        ConsoleEx.WriteLinePrefix("Enable debug logger? (Y/N)");

        // Use a timeout for the initial setup question
        var response = await Task.Run(ConsoleEx.ReadLinePrefix);
        if (response == null) {
            return null;
        }

        var enableLogger = response.ToLower().FirstOrDefault() is 'y';
        ConsoleEx.WriteLinePrefix($"Proceeding with logger {(enableLogger ? "enabled" : "disabled")}.");

        return new Options {
            EnableConsoleLogger = enableLogger
        };
    }

    /// <summary>
    /// Establishes connection, initializes session, and registers event handlers.
    /// </summary>
    public static async Task<bool> EstablishSessionAsync() {
        // Connect to server
        await Session.ConnectAsync("127.0.0.1", 6789);
        ConsoleEx.WriteLinePrefix("Connected to localhost:6789.");

        // Initialize and display server information
        var info = await Session.InitializeAsync();
        ConsoleEx.WriteLinePrefix($"Session initialized. Server v{info.VarVersion}, API v{info.ApiVersion}.");

        // Prompt for username and register
        ConsoleEx.WriteLinePrefix("Username?");
        string? name = await Task.Run(ConsoleEx.ReadLinePrefix);
        ArgumentNullException.ThrowIfNull(name);

        await Session.RegisterAndSubscribeAsync(name);
        ConsoleEx.WriteLinePrefix($"Registered as '{name}'");

        // Set up connection closed handler
        var shutdownTcs = new TaskCompletionSource<bool>();
        Session.ConnectionClosed += (_, _) => {
            ConsoleEx.WriteLinePrefix("Session closed.");
            shutdownTcs.TrySetResult(true);
            Environment.Exit(0);
        };

        return true;
    }

    /// <summary>
    /// Runs the main command processing loop without blocking the thread.
    /// </summary>
    private static async Task RunCommandLoopAsync() {
        using var cts = new CancellationTokenSource();
        // ReSharper disable once MethodSupportsCancellation
        var commandLoopTask = Task.Run(async () => {
            // ReSharper disable AccessToDisposedClosure
            while (!cts.Token.IsCancellationRequested) {
                if (Console.KeyAvailable) {
                    var command = ConsoleEx.ReadLinePrefix();
                    if (command == null) {
                        await cts.CancelAsync();
                        break;
                    }

                    try {
                        await ProcessCommandAsync(command);
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
            await Session.CloseAsync();
        }
    }

    private static void ConfigureGracefulTermination() {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => {
            // For ProcessExit we can block since the process is shutting down
            CloseSessionAsync().GetAwaiter().GetResult();
        };
        return;

        // Termination function
        static async Task CloseSessionAsync() {
            if(Interlocked.CompareExchange(ref _isTerminating, 1, 0) == 0) {
                if(Session != null! && Session.ConnectionState == Arcor2SessionState.Open) {
                    ConsoleEx.WriteLinePrefix("Closing the session...");
                    await Session.CloseAsync();
                }
            }
        }
    }


    /// <summary>
    /// Processes a command input by the user.
    /// </summary>
    private static async Task ProcessCommandAsync(string text) {
        var parts = text.Split(" ");
        var command = parts[0];
        var args = parts[1..];

        switch (command) {
            // Utility commands
            case "!help":
                DisplayHelp();
                break;

            // Navigation and information commands
            case "!ns" or "!navigation_state":
                DisplayNavigationState();
                break;

            case "!ot" or "!object_types":
                DisplayObjectTypes();
                break;

            case "!s" or "!scenes":
                DisplayScenes();
                break;

            case "!p" or "!projects":
                DisplayProjects();
                break;

            // Object Type commands
            case "!aot" or "!add_object_type":
                await AddObjectTypeAsync(args);
                break;

            case "!rot" or "!remove_object_type":
                await RemoveObjectTypeAsync(args);
                break;

            case "!update_object_model_box":
                await UpdateObjectModelBoxAsync(args);
                break;

            // Scene commands
            case "!action_objects":
                DisplayActionObjects();
                break;

            case "!rs" or "!reload_scenes":
                await ReloadScenesAsync();
                break;

            case "!usd" or "!update_scene_desc":
                await UpdateSceneDescriptionAsync(args);
                break;

            case "!rename_scene":
                await RenameSceneAsync(args);
                break;

            case "!duplicate_scene":
                await DuplicateSceneAsync(args);
                break;

            case "!new_scene":
                await CreateSceneAsync(args);
                break;

            case "!remove_scene":
                await RemoveSceneAsync(args);
                break;

            case "!os" or "!open_scene":
                await OpenSceneAsync(args);
                break;

            case "!ls" or "!load_scene":
                await LoadSceneAsync(args);
                break;

            case "!cs" or "!close_scene":
                await CloseSceneAsync(args);
                break;

            case "!ss" or "!save_scene":
                await SaveSceneAsync();
                break;

            case "!start_scene":
                await StartSceneAsync();
                break;

            case "!stop_scene":
                await StopSceneAsync();
                break;

            // Action Object commands
            case "!aac" or "!add_action_object":
                await AddActionObjectAsync(args);
                break;

            case "!remove_action_object":
                await RemoveActionObjectAsync(args);
                break;

            case "!add_virtual_box":
                await AddVirtualBoxAsync(args);
                break;

            case "!add_virtual_cylinder":
                await AddVirtualCylinderAsync(args);
                break;

            case "!add_virtual_sphere":
                await AddVirtualSphereAsync(args);
                break;

            case "!update_action_object_pose" or "!uaop":
                await UpdateActionObjectPoseAsync(args);
                break;

            case "!rename_action_object":
                await RenameActionObjectAsync(args);
                break;

            case "!update_action_object_parameters":
                await UpdateActionObjectParametersAsync(args);
                break;

            case "!move_to_pose":
                await MoveToPoseAsync(args);
                break;

            case "!move_to_joints":
                await MoveToJointsAsync(args);
                break;

            case "!move_to_orientation":
                await MoveToOrientationAsync(args);
                break;

            case "!step_position":
                await StepPositionAsync(args);
                break;

            case "!step_orientation":
                await StepOrientationAsync(args);
                break;

            case "!set_eef_perpendicular_to_world":
                await SetEndEffectorPerpendicularToWorldAsync(args);
                break;

            case "!set_hand_teaching_mode":
                await SetHandTeachingModeAsync(args);
                break;

            case "!forward_kinematics":
                await GetForwardKinematicsAsync(args);
                break;

            case "!inverse_kinematics":
                await GetInverseKinematicsAsync(args);
                break;

            case "!calibrate_robot":
                await CalibrateRobotAsync(args);
                break;

            case "!calibrate_camera":
                await CalibrateCameraAsync(args);
                break;

            case "!get_camera_color_image":
                await GetCameraColorImageAsync(args);
                break;

            case "!get_camera_color_parameters":
                await GetCameraColorParametersAsync(args);
                break;

            case "!stop_robot":
                await StopRobotAsync(args);
                break;

            case "!action_object_param_value":
                await GetActionObjectParameterValuesAsync(args);
                break;

            case "!update_pose_using_robot":
                await UpdatePoseUsingRobotAsync(args);
                break;

            case "!aiming_start":
                await StartObjectAimingAsync(args);
                break;

            case "!aiming_cancel":
                await CancelObjectAimingAsync(args);
                break;

            case "!aiming_finish":
                await FinishObjectAimingAsync(args);
                break;

            case "!aiming_add":
                await AddPointForObjectAimingAsync(args);
                break;

            // Project commands
            case "!rp" or "!reload_projects":
                await ReloadProjectsAsync();
                break;

            case "!new_project":
                await CreateProjectAsync(args);
                break;

            case "!upd" or "!update_project_desc":
                await UpdateProjectDescriptionAsync(args);
                break;

            case "!rename_project":
                await RenameProjectAsync(args);
                break;

            case "!duplicate_project":
                await DuplicateProjectAsync(args);
                break;

            case "!remove_project":
                await RemoveProjectAsync(args);
                break;

            case "!op" or "!open_project":
                await OpenProjectAsync(args);
                break;

            case "!lp" or "!load_project":
                await LoadProjectAsync(args);
                break;

            case "!cp" or "!close_project":
                await CloseProjectAsync(args);
                break;

            case "!sp" or "!save_project":
                await SaveProjectAsync();
                break;

            case "!start_project":
                await StartProjectAsync();
                break;

            case "!stop_project":
                await StopProjectAsync();
                break;

            case "!set_project_has_logic":
                await SetProjectHasLogicAsync(args);
                break;

            case "!build_project":
                await BuildProjectAsync(args);
                break;

            case "!build_project_temp":
                await BuildProjectTempAsync(args);
                break;

            // Project parameters
            case "!parameters":
                DisplayProjectParameters();
                break;

            case "!add_project_parameter":
                await AddProjectParameterAsync(args);
                break;

            case "!update_project_parameter_value":
                await UpdateProjectParameterValueAsync(args);
                break;

            case "!update_project_parameter_name":
                await UpdateProjectParameterNameAsync(args);
                break;

            case "!remove_project_parameter":
                await RemoveProjectParameterAsync(args);
                break;

            // Project overrides
            case "!overrides":
                DisplayProjectOverrides();
                break;

            case "!add_project_override":
                await AddProjectOverrideAsync(args);
                break;

            case "!update_project_override":
                await UpdateProjectOverrideAsync(args);
                break;

            case "!remove_project_override":
                await RemoveProjectOverrideAsync(args);
                break;

            // Action points
            case "!ap" or "!action_points":
                DisplayActionPoints();
                break;

            case "!add_ap":
                await AddActionPointAsync(args);
                break;

            case "!add_ap_using_robot":
                await AddActionPointUsingRobotAsync(args);
                break;

            case "!duplicate_ap":
                await DuplicateActionPointAsync(args);
                break;

            case "!rename_ap":
                await RenameActionPointAsync(args);
                break;

            case "!update_ap_parent":
                await UpdateActionPointParentAsync(args);
                break;

            case "!update_ap_position":
                await UpdateActionPointPositionAsync(args);
                break;

            case "!remove_ap":
                await RemoveActionPointAsync(args);
                break;

            case "!update_ap_using_robot":
                await UpdateActionPointUsingRobotAsync(args);
                break;

            // Actions
            case "!actions":
                DisplayActions(args);
                break;

            case "!add_action":
                await AddActionAsync(args);
                break;

            case "!update_action_parameters":
                await UpdateActionParametersAsync(args);
                break;

            case "!update_action_flows":
                await UpdateActionFlowsAsync(args);
                break;

            case "!rename_action":
                await RenameActionAsync(args);
                break;

            case "!remove_action":
                await RemoveActionAsync(args);
                break;

            case "!execute_action":
                await ExecuteActionAsync(args);
                break;

            case "!cancel_action":
                await CancelActionAsync(args);
                break;

            // Orientations
            case "!orientations":
                DisplayOrientations(args);
                break;

            case "!add_orientation":
                await AddOrientationAsync(args);
                break;

            case "!add_orientation_using_robot":
                await AddOrientationUsingRobotAsync(args);
                break;

            case "!update_orientation":
                await UpdateOrientationAsync(args);
                break;

            case "!update_orientation_using_robot":
                await UpdateOrientationUsingRobotAsync(args);
                break;

            case "!rename_orientation":
                await RenameOrientationAsync(args);
                break;

            case "!remove_orientation":
                await RemoveOrientationAsync(args);
                break;

            // Joints
            case "!joints":
                DisplayJoints(args);
                break;

            case "!add_joints_using_robot":
                await AddJointsUsingRobotAsync(args);
                break;

            case "!update_joints":
                await UpdateJointsAsync(args);
                break;

            case "!update_joints_using_robot":
                await UpdateJointsUsingRobotAsync(args);
                break;

            case "!rename_joints":
                await RenameJointsAsync(args);
                break;

            case "!remove_joints":
                await RemoveJointsAsync(args);
                break;

            // Logic items
            case "!logic_items":
                DisplayLogicItems();
                break;

            case "!add_logic_item":
                await AddLogicItemAsync(args);
                break;

            case "!update_logic_item":
                await UpdateLogicItemAsync(args);
                break;

            case "!remove_logic_item":
                await RemoveLogicItemAsync(args);
                break;

            // Packages
            case "!packages":
                DisplayPackages();
                break;

            case "!rename_package":
                await RenamePackageAsync(args);
                break;

            case "!remove_package":
                await RemovePackageAsync(args);
                break;

            case "!run_package":
                await RunPackageAsync(args);
                break;

            case "!stop_package":
                await StopPackageAsync();
                break;

            case "!resume_package":
                await ResumePackageAsync();
                break;

            case "!pause_package":
                await PausePackageAsync();
                break;

            case "!step_package":
                await StepPackageAsync();
                break;

            case "!upload_package":
                await UploadPackage(args);
                break;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Gets the current scene based on navigation state.
    /// </summary>
    private static SceneManager GetCurrentScene() =>
        Session.NavigationState switch {
            NavigationState.Scene => Session.Scenes.FirstOrDefault(s => s.Id == Session.NavigationId)!,
            NavigationState.Project => Session.Projects.FirstOrDefault(s => s.Id == Session.NavigationId)!.Scene,
            NavigationState.Package => Session.Packages.FirstOrDefault(s => s.Id == Session.NavigationId)!.Project
                .Scene,
            _ => throw new Exception("Invalid navigation state.")
        };

    /// <summary>
    /// Gets action objects for the current scene or project.
    /// </summary>
    private static ReadOnlyObservableCollection<ActionObjectManager> GetActionObjects() =>
        (Session.NavigationState switch {
            NavigationState.Scene => Session.Scenes.FirstOrDefault(s => s.Id == Session.NavigationId)!.ActionObjects,
            NavigationState.Project => Session.Projects.FirstOrDefault(s => s.Id == Session.NavigationId)!.Scene
                .ActionObjects,
            NavigationState.Package => Session.Packages.FirstOrDefault(s => s.Id == Session.NavigationId)!.Project.Scene
                .ActionObjects,
            _ => throw new Exception("Invalid navigation state.")
        })!;

    /// <summary>
    /// Gets the current project.
    /// </summary>
    private static ProjectManager GetCurrentProject() =>
        Session.Projects.FirstOrDefault(s => s.Id == Session.NavigationId)!;

    /// <summary>
    /// Creates a Position from command arguments.
    /// </summary>
    private static Position ParsePosition(string[] args, int startIndex) =>
        new(
            Convert.ToDecimal(args[startIndex]),
            Convert.ToDecimal(args[startIndex + 1]),
            Convert.ToDecimal(args[startIndex + 2])
        );

    /// <summary>
    /// Creates an Orientation from command arguments.
    /// </summary>
    private static Orientation ParseOrientation(string[] args, int startIndex) =>
        new(
            Convert.ToDecimal(args[startIndex]),
            Convert.ToDecimal(args[startIndex + 1]),
            Convert.ToDecimal(args[startIndex + 2]),
            Convert.ToDecimal(args[startIndex + 3])
        );

    /// <summary>
    /// Creates a Pose from command arguments.
    /// </summary>
    private static Pose ParsePose(string[] args, int startIndex) =>
        new(ParsePosition(args, startIndex), ParseOrientation(args, startIndex + 3));

    #endregion

    #region Command Implementation Methods

    // Display and Information Methods

    private static void DisplayHelp() {
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
            !add_object_type <TYPE> <PARENT_TYPE> <DESC> - Adds a new object type.
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
            !set_eef_perpendicular_to_world <ID> - Sets the default end-effector perpendicular to world.
            !set_hand_teaching_mode <ID> ["true"/"false"] - Toggles the hand teaching mode for the default arm.
            !forward_kinematics <ID> - Calculates forward kinematics for the current joints.
            !inverse_kinematics <ID >- Calculates forward kinematics for the current pose.
            !calibrate_robot <ID> <CAMERA_ID> <MOVE_TO_CAL_POSE_BOOL> - Calibrates the robot.
            !calibrate_camera <ID> - Calibrates the camera.
            !get_camera_color_image <ID> - Gets camera color image.
            !get_camera_color_parameters <ID> - Get camera intrinsic parameters.
            !stop_robot <ID> - Stops the robot's movement.
            !update_pose_using_robot <ID> <ROBOT_ID> - Updates pose using the default end effector of a robot.
            
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
            !execute_action <AP_ID> <ID> - Executes the action.
            !cancel_action <AP_ID> <ID> - Cancels the execution of the action.
            
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
            !upload_package <ID> <BASE64_ZIP> - Uploads a package to the server.
            """);
    }

    private static void DisplayNavigationState() {
        Console.WriteLine(
            $"{Enum.GetName(Session.NavigationState)} {(Session.NavigationId is null ? "" : "(" + Session.NavigationId + ")")}");
    }

    private static void DisplayObjectTypes() {
        foreach (var type in Session.ObjectTypes) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(type, objectName: type.Data.Meta.Type));
        }
    }

    private static void DisplayScenes() {
        foreach (var sceneData in Session.Scenes) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(sceneData, objectName: sceneData.Data.Name));
        }
    }

    private static void DisplayProjects() {
        foreach (var projectData in Session.Projects) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(projectData, objectName: projectData.Data.Name));
        }
    }

    private static void DisplayActionObjects() {
        foreach (var actionObject in GetActionObjects()) {
            Console.WriteLine(
                ReflectionHelper.FormatObjectProperties(actionObject, objectName: actionObject.Data.Meta.Type));
        }
    }

    private static void DisplayProjectParameters() {
        foreach (var param in GetCurrentProject().Parameters!) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(param, objectName: param.Data.Name));
        }
    }

    private static void DisplayProjectOverrides() {
        foreach (var @override in GetCurrentProject().Overrides!) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(@override));
        }
    }

    private static void DisplayActionPoints() {
        foreach (var actionPoint in GetCurrentProject().ActionPoints!) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(actionPoint));
        }
    }

    private static void DisplayActions(string[] args) {
        foreach (var action in GetCurrentProject().ActionPoints!
                     .FirstOrDefault(a => a.Id == args[0])!.Actions) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(action));
        }
    }

    private static void DisplayOrientations(string[] args) {
        foreach (var orientation in GetCurrentProject().ActionPoints!
                     .FirstOrDefault(a => a.Id == args[0])!.Orientations) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(orientation));
        }
    }

    private static void DisplayJoints(string[] args) {
        foreach (var joint in GetCurrentProject().ActionPoints!
                     .FirstOrDefault(a => a.Id == args[0])!.Joints) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(joint));
        }
    }

    private static void DisplayLogicItems() {
        foreach (var logicItem in GetCurrentProject().LogicItems!) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(logicItem));
        }
    }

    private static void DisplayPackages() {
        foreach (var package in Session.Packages) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(package));
        }
    }

    private static async Task AddObjectTypeAsync(string[] args) {
        await Session.CreateObjectTypeAsync(args[0], args[1], args[2]);
    }

    private static async Task RemoveObjectTypeAsync(string[] args) {
        await Session.ObjectTypes.FirstOrDefault(s => s.Id == args[0])!.DeleteAsync();
    }

    private static async Task UpdateObjectModelBoxAsync(string[] args) {
        await Session.ObjectTypes.FirstOrDefault(s => s.Id == args[0])!.UpdateObjectModel(
            new BoxCollisionModel(
                Convert.ToDecimal(args[1]),
                Convert.ToDecimal(args[2]),
                Convert.ToDecimal(args[3])
            )
        );
    }

    private static async Task ReloadScenesAsync() {
        await Session.ReloadScenesAsync();
    }

    private static async Task UpdateSceneDescriptionAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == args[0])!.UpdateDescriptionAsync(args[1]);
    }

    private static async Task RenameSceneAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == args[0])!.RenameAsync(args[1]);
    }

    private static async Task DuplicateSceneAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == args[0])!.DuplicateAsync(args[1]);
    }

    private static async Task CreateSceneAsync(string[] args) {
        await Session.CreateSceneAsync(args[0], args.Length > 1 ? args[1] : string.Empty);
    }

    private static async Task RemoveSceneAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
    }

    private static async Task OpenSceneAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == args[0])!.OpenAsync();
    }

    private static async Task LoadSceneAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == args[0])!.LoadAsync();
    }

    private static async Task CloseSceneAsync(string[] args) {
        await Session.Scenes.FirstOrDefault(s => s.Id == Session.NavigationId)!.CloseAsync(
            args.Length > 0 && args[0] == "force");
    }

    private static async Task SaveSceneAsync() {
        await Session.Scenes.FirstOrDefault(s => s.Id == Session.NavigationId)!.SaveAsync();
    }

    private static async Task StartSceneAsync() {
        await Session.Scenes.FirstOrDefault(s => s.Id == Session.NavigationId)!.StartAsync();
    }

    private static async Task StopSceneAsync() {
        await Session.Scenes.FirstOrDefault(s => s.Id == Session.NavigationId)!.StopAsync();
    }

    private static async Task AddActionObjectAsync(string[] args) {
        var scene = GetCurrentScene();
        var pose = ParsePose(args, 2);

        if (args.Length > 9) {
            await scene.AddActionObjectAsync(
                args[0],
                args[1],
                pose,
                JsonConvert.DeserializeObject<List<Parameter>>(args[9])!);
        }
        else {
            await scene.AddActionObjectAsync(args[0], args[1], pose);
        }
    }

    private static async Task RemoveActionObjectAsync(string[] args) {
        await GetCurrentScene().ActionObjects!
            .FirstOrDefault(o => o.Id == args[0])!
            .RemoveAsync(args.Length > 1 && args[1] == "force");
    }

    private static async Task AddVirtualBoxAsync(string[] args) {
        await GetCurrentScene().AddVirtualCollisionBoxAsync(
            args[0],
            ParsePose(args, 1),
            new BoxCollisionModel(
                Convert.ToDecimal(args[8]),
                Convert.ToDecimal(args[9]),
                Convert.ToDecimal(args[10])
            )
        );
    }

    private static async Task AddVirtualCylinderAsync(string[] args) {
        await GetCurrentScene().AddVirtualCollisionCylinderAsync(
            args[0],
            ParsePose(args, 1),
            new CylinderCollisionModel(
                Convert.ToDecimal(args[8]),
                Convert.ToDecimal(args[9])
            )
        );
    }

    private static async Task AddVirtualSphereAsync(string[] args) {
        await GetCurrentScene().AddVirtualCollisionSphereAsync(
            args[0],
            ParsePose(args, 1),
            new SphereCollisionModel(Convert.ToDecimal(args[8]))
        );
    }

    private static async Task UpdateActionObjectPoseAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .UpdatePoseAsync(ParsePose(args, 1));
    }

    private static async Task RenameActionObjectAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .RenameAsync(args[1]);
    }

    private static async Task UpdateActionObjectParametersAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .UpdateParametersAsync(
                JsonConvert.DeserializeObject<List<Parameter>>(string.Join(' ', args.Skip(1)))!);
    }

    private static async Task MoveToPoseAsync(string[] args) {
        if (args.Length > 11) {
            await GetActionObjects()
                .FirstOrDefault(o => o.Id == args[0])!
                .MoveToPoseAsync(
                    args[0],
                    ParsePose(args, 2),
                    safe: Convert.ToBoolean(args[9]),
                    linear: Convert.ToBoolean(args[10]),
                    speed: Convert.ToDecimal(args[11]),
                    armId: args.Length > 12 ? args[12] : null!);
        }
        else {
            await GetActionObjects()
                .FirstOrDefault(o => o.Id == args[0])!
                .MoveToPoseAsync(
                    ParsePose(args, 1),
                    safe: Convert.ToBoolean(args[8]),
                    linear: Convert.ToBoolean(args[9]),
                    speed: Convert.ToDecimal(args[10]));
        }
    }

    private static async Task MoveToJointsAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .MoveToActionPointJointsAsync(
                args[1],
                safe: Convert.ToBoolean(args[2]),
                linear: Convert.ToBoolean(args[3]),
                speed: Convert.ToDecimal(args[4]),
                endEffectorId: args.Length > 5 ? args[5] : "default",
                armId: args.Length > 6 ? args[6] : null!);
    }

    private static async Task MoveToOrientationAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.MoveToActionPointOrientationAsync(
                args[1],
                safe: Convert.ToBoolean(args[2]),
                linear: Convert.ToBoolean(args[3]),
                speed: Convert.ToDecimal(args[4]),
                endEffectorId: args.Length > 5 ? args[5] : "default",
                armId: args.Length > 6 ? args[6] : null!);
    }


    private static async Task UploadPackage(string[] args) {
        await Session.UploadPackageAsync(args[0], args[1]);
    }

    private static async Task StepPackageAsync() {
        await Session.Packages.FirstOrDefault(s => s.Id == Session.NavigationId)!.StepAsync();
    }

    private static async Task PausePackageAsync() {
        await Session.Packages.FirstOrDefault(s => s.Id == Session.NavigationId)!.PauseAsync();
    }

    private static async Task ResumePackageAsync() {
        await Session.Packages.FirstOrDefault(s => s.Id == Session.NavigationId)!.ResumeAsync();
    }

    private static async Task StopPackageAsync() {
        await Session.Packages.FirstOrDefault(s => s.Id == Session.NavigationId)!.StopAsync();
    }

    private static async Task RunPackageAsync(string[] args) {
        await Session.Packages.FirstOrDefault(s => s.Id == args[0])!
            .RunAsync(args.Skip(2).ToList(), Convert.ToBoolean(args[1]));
    }

    private static async Task RemovePackageAsync(string[] args) {
        await Session.Packages.FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
    }

    private static async Task RenamePackageAsync(string[] args) {
        await Session.Packages.FirstOrDefault(s => s.Id == args[0])!.RenameAsync(args[1]);
    }

    private static async Task RemoveLogicItemAsync(string[] args) {
        await GetCurrentProject().LogicItems!
            .FirstOrDefault(s => s.Id == args[0])!
            .RemoveAsync();
    }

    private static async Task UpdateLogicItemAsync(string[] args) {
        await GetCurrentProject().LogicItems!
            .FirstOrDefault(s => s.Id == args[0])!
            .UpdateAsync(
                args[1],
                args[2],
                args.Length > 3 ? new ProjectLogicIf(args[3], args[4]) : null); 
    }

    private static async Task AddLogicItemAsync(string[] args) {
        await GetCurrentProject().AddLogicItem(
            args[0],
            args[1],
            args.Length > 2 ? new ProjectLogicIf(args[2], args[3]) : null);
    }

    private static async Task RemoveJointsAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Joints
            .FirstOrDefault(a => a.Id == args[1])!
            .RemoveAsync();
    }

    private static async Task RenameJointsAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Joints
            .FirstOrDefault(a => a.Id == args[1])!
            .RenameAsync(args[2]);
    }

    private static async Task UpdateJointsUsingRobotAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Joints
            .FirstOrDefault(a => a.Id == args[1])!
            .UpdateUsingRobotAsync();
    }

    private static async Task UpdateJointsAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Joints
            .FirstOrDefault(a => a.Id == args[1])!
            .UpdateAsync(
                JsonConvert.DeserializeObject<List<Joint>>(string.Join("", args.Skip(2)))!);
    }

    private static async Task AddJointsUsingRobotAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!
            .AddJointsUsingRobotAsync(
                args[1],
                args.Length > 3 ? args[3] : "default",
                args.Length > 4 ? args[4] : null,
                args[2]);
    }

    private static async Task RemoveOrientationAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Orientations
            .FirstOrDefault(a => a.Id == args[1])!
            .RemoveAsync();
    }

    private static async Task RenameOrientationAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Orientations
            .FirstOrDefault(a => a.Id == args[1])!
            .RenameAsync(args[2]);
    }

    private static async Task UpdateOrientationUsingRobotAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Orientations
            .FirstOrDefault(a => a.Id == args[1])!
            .UpdateUsingRobotAsync(
                args[2],
                args.Length > 3 ? args[3] : "default",
                args.Length > 4 ? args[4] : null);
    }

    private static async Task UpdateOrientationAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Orientations
            .FirstOrDefault(a => a.Id == args[1])!
            .UpdateAsync(ParseOrientation(args, 2));
    }

    private static async Task AddOrientationUsingRobotAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!
            .AddOrientationUsingRobotAsync(
                args[1],
                args.Length > 3 ? args[3] : "default",
                args.Length > 4 ? args[4] : null,
                args[2]);
    }

    private static async Task AddOrientationAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!
            .AddOrientationAsync(
                ParseOrientation(args, 1),
                args[5]);
    }

    private static async Task CancelActionAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Actions
            .FirstOrDefault(a => a.Id == args[1])!
            .CancelAsync();
    }

    private static async Task ExecuteActionAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Actions
            .FirstOrDefault(a => a.Id == args[1])!
            .ExecuteAsync();
    }

    private static async Task RemoveActionAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Actions
            .FirstOrDefault(a => a.Id == args[1])!
            .RemoveAsync();
    }

    private static async Task RenameActionAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Actions
            .FirstOrDefault(a => a.Id == args[1])!
            .RenameAsync(args[2]);
    }

    private static async Task UpdateActionFlowsAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Actions
            .FirstOrDefault(a => a.Id == args[1])!
            .UpdateFlowsAsync(
                JsonConvert.DeserializeObject<List<Flow>>(string.Join(' ', args.Skip(2)))!);
    }

    private static async Task UpdateActionParametersAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!.Actions
            .FirstOrDefault(a => a.Id == args[1])!
            .UpdateParametersAsync(
                JsonConvert.DeserializeObject<List<ActionParameter>>(string.Join(' ', args.Skip(2)))!);
    }

    private static async Task AddActionAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(a => a.Id == args[0])!
            .AddActionAsync(
                args[1],
                args[2],
                [new Flow(Flow.TypeEnum.Default, [])],
                []);
    }

    private static async Task UpdateActionPointUsingRobotAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(s => s.Id == args[0])!
            .UpdateUsingRobotAsync(
                args[1],
                args.Length > 2 ? args[2] : "default",
                args.Length > 3 ? args[3] : null);
    }

    private static async Task RemoveActionPointAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(s => s.Id == args[0])!
            .RemoveAsync();
    }

    private static async Task UpdateActionPointPositionAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(s => s.Id == args[0])!
            .UpdatePositionAsync(ParsePosition(args, 1));
    }

    private static async Task UpdateActionPointParentAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(s => s.Id == args[0])!
            .UpdateParentAsync(args[1]);
    }

    private static async Task RenameActionPointAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(s => s.Id == args[0])!
            .RenameAsync(args[1]);
    }

    private static async Task DuplicateActionPointAsync(string[] args) {
        await GetCurrentProject().ActionPoints!
            .FirstOrDefault(s => s.Id == args[0])!
            .DuplicateAsync(ParsePosition(args, 1));
    }

    private static async Task AddActionPointUsingRobotAsync(string[] args) {
        await GetCurrentProject().AddActionPointUsingRobotAsync(
            args[0],
            args[1],
            args.Length > 2 ? args[2] : "default",
            (args.Length > 3 ? args[3] : null)!);
    }

    private static async Task AddActionPointAsync(string[] args) {
        if(args.Length > 4) {
            await GetCurrentProject().AddActionPointAsync(
                args[0],
                ParsePosition(args, 1),
                args[4]);
        }
        else {
            await GetCurrentProject().AddActionPointAsync(
                args[0],
                ParsePosition(args, 1));
        }
    }

    private static async Task RemoveProjectOverrideAsync(string[] args) {
        await GetCurrentProject().Overrides!
            .FirstOrDefault(s => s.Data.ActionObjectId == args[0] && s.Data.Parameter.Name == args[1])!
            .RemoveAsync();
    }

    private static async Task UpdateProjectOverrideAsync(string[] args) {
        await GetCurrentProject().Overrides!
            .FirstOrDefault(s => s.Data.ActionObjectId == args[0] && s.Data.Parameter.Name == args[1])!
            .UpdateAsync(new Parameter(args[1], args[2], args[3]));

    }

    private static async Task AddProjectOverrideAsync(string[] args) {
        await GetCurrentProject().AddOverrideAsync(
            args[0],
            new Parameter(args[1], args[2], args[3]));
    }

    private static async Task RemoveProjectParameterAsync(string[] args) {
        await GetCurrentProject().Parameters!
            .FirstOrDefault(s => s.Id == args[0])!.RemoveAsync();
    }

    private static async Task UpdateProjectParameterNameAsync(string[] args) {
        await GetCurrentProject().Parameters!
            .FirstOrDefault(s => s.Id == args[0])!.UpdateNameAsync(args[1]);
    }

    private static async Task UpdateProjectParameterValueAsync(string[] args) {
        await GetCurrentProject().Parameters!
            .FirstOrDefault(s => s.Id == args[0])!.UpdateValueAsync(args[1]);
    }

    private static async Task AddProjectParameterAsync(string[] args) {
        await GetCurrentProject().AddProjectParameterAsync(args[0], args[1], args[2]);
    }

    private static async Task BuildProjectTempAsync(string[] args) {
        await GetCurrentProject().BuildIntoTemporaryPackageAndRunAsync(args.Skip(1).ToList(), Convert.ToBoolean(args[0]));
    }

    private static async Task BuildProjectAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.BuildIntoPackageAsync(args[1]);
    }

    private static async Task SetProjectHasLogicAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.SetHasLogicAsync(Convert.ToBoolean(args[1]));
    }

    private static async Task StopProjectAsync() {
        await GetCurrentProject().Scene.StopAsync();
    }

    private static async Task StartProjectAsync() {
        await GetCurrentProject().Scene.StartAsync();
    }

    private static async Task SaveProjectAsync() {
        await GetCurrentProject().SaveAsync();
    }

    private static async Task CloseProjectAsync(string[] args) {
        await GetCurrentProject().CloseAsync(args.Length > 0 && args[0] == "force");
    }

    private static async Task LoadProjectAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.LoadAsync();
    }

    private static async Task OpenProjectAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.OpenAsync();
    }

    private static async Task RemoveProjectAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.RemoveAsync(); 
    }

    private static async Task DuplicateProjectAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.DuplicateAsync(args[1]);
    }

    private static async Task RenameProjectAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.RenameAsync(args[1]);
    }

    private static async Task UpdateProjectDescriptionAsync(string[] args) {
        await Session.Projects.FirstOrDefault(s => s.Id == args[0])!.UpdateDescriptionAsync(args[1]);
    }

    private static async Task CreateProjectAsync(string[] args) {
        await Session.CreateProjectAsync(
            args[0],
            args[1],
            args.Length > 3 ? args[3] : string.Empty,
            Convert.ToBoolean(args[2]));
    }

    private static async Task ReloadProjectsAsync() {
        await Session.ReloadProjectsAsync();
    }

    private static async Task AddPointForObjectAimingAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .AddPointForObjectAimingAsync(Convert.ToInt32(args[1]));
    }

    private static async Task FinishObjectAimingAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .FinishObjectAimingAsync();
    }

    private static async Task CancelObjectAimingAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .CancelObjectAimingAsync();
    }

    private static async Task StartObjectAimingAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .StartObjectAimingAsync(args[1]);
    }

    private static async Task UpdatePoseUsingRobotAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .UpdatePoseUsingRobotAsync(args[1]);
    }

    private static async Task GetActionObjectParameterValuesAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!
            .GetParameterValuesAsync(args[1]);
    }

    private static async Task StopRobotAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.StopAsync();
    }

    private static async Task GetCameraColorParametersAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.GetCameraColorParametersAsync();
    }

    private static async Task GetCameraColorImageAsync(string[] args) {
#pragma warning disable CS0618 // Type or member is obsolete
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.GetCameraColorImageAsync();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static async Task CalibrateCameraAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.CalibrateCameraAsync();
    }

    private static async Task CalibrateRobotAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.CalibrateRobotAsync(args[1], Convert.ToBoolean(args[2]));
    }

    private static async Task GetInverseKinematicsAsync(string[] args) {
        var inverseKinematics = await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.GetInverseKinematicsAsync();
        foreach(var inverseKinematic in inverseKinematics) {
            Console.WriteLine(ReflectionHelper.FormatObjectProperties(inverseKinematic));
        }

    }

    private static async Task GetForwardKinematicsAsync(string[] args) {
        var forwardKinematics = await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.GetForwardKinematicsAsync();
        Console.WriteLine(ReflectionHelper.FormatObjectProperties(forwardKinematics));

    }

    private static async Task SetHandTeachingModeAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.SetHandTeachingModeAsync(Convert.ToBoolean(args[1]));

    }

    private static async Task SetEndEffectorPerpendicularToWorldAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.SetEndEffectorPerpendicularToWorldAsync();

    }

    private static async Task StepOrientationAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.StepOrientationAsync(
                Enum.Parse<Axis>(args[1]),
                Convert.ToDecimal(args[2]),
                safe: Convert.ToBoolean(args[3]),
                linear: Convert.ToBoolean(args[4]),
                speed: Convert.ToDecimal(args[5]),
                endEffectorId: args.Length > 6 ? args[6] : "default",
                armId: args.Length > 7 ? args[7] : null!);

    }

    private static async Task StepPositionAsync(string[] args) {
        await GetActionObjects()
            .FirstOrDefault(o => o.Id == args[0])!.StepPositionAsync(
                Enum.Parse<Axis>(args[1]),
                Convert.ToDecimal(args[2]),
                safe: Convert.ToBoolean(args[3]),
                linear: Convert.ToBoolean(args[4]),
                speed: Convert.ToDecimal(args[5]),
                endEffectorId: args.Length > 6 ? args[6] : "default",
                armId: args.Length > 7 ? args[7] : null!);

    }

    #endregion
}