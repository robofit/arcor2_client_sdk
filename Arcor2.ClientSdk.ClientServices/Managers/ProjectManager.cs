using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Managers
{
    /// <summary>
    /// Manages lifetime of a project.
    /// </summary>
    public class ProjectManager : LockableArcor2ObjectManager<BareProject> {

        internal ObservableCollection<ProjectParameterManager>? parameters { get; }
        /// <summary>
        /// A collection of project parameters.
        /// </summary>
        public ReadOnlyObservableCollection<ProjectParameterManager>? Parameters { get; }

        internal ObservableCollection<ActionPointManager>? actionPoints { get; }
        /// <summary>
        /// A collection of action points.
        /// </summary>
        public ReadOnlyObservableCollection<ActionPointManager>? ActionPoints { get; }

        internal ObservableCollection<ProjectOverrideManager>? overrides { get; }
        /// <summary>
        /// A collection of project overrides.
        /// </summary>
        public ReadOnlyObservableCollection<ProjectOverrideManager>? Overrides { get; }

        internal ObservableCollection<LogicItemManager>? logicItems { get; }
        /// <summary>
        /// A collection of logic items.
        /// </summary>
        public ReadOnlyObservableCollection<LogicItemManager>? LogicItems { get; }

        /// <summary>
        /// Gets if the project is open.
        /// </summary>
        /// <returns> <c>true</c> if this project is open, <c>false</c> otherwise.</returns>
        public bool IsOpen => Session.NavigationState == NavigationState.Project && Session.NavigationId == Id;

        /// <summary>
        /// Gets the parent scene.
        /// </summary>
        // Do not cache, can in-theory change.
        public SceneManager Scene => Session.Scenes.First(s => s.Id == Data.SceneId);

        /// <summary>
        /// Raised when project is saved by the server.
        /// </summary>
        public EventHandler? Saved;

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="meta">Project meta object.</param>
        internal ProjectManager(Arcor2Session session, BareProject meta) : base(session, meta, meta.Id) { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">Project object.</param>
        internal ProjectManager(Arcor2Session session, Project project) : base(session, project.MapToBareProject(), project.Id) {
            parameters = new ObservableCollection<ProjectParameterManager>(project.Parameters
                .Select(p => new ProjectParameterManager(Session, this, p)));
            actionPoints = new ObservableCollection<ActionPointManager>(project.ActionPoints
                .Select(a => new ActionPointManager(Session, this, a)));
            var flattenedOverrides =
                project.ObjectOverrides.SelectMany(o => o.Parameters, (@override, parameter) => (ActionObjectId: @override.Id, Parameter: parameter)).ToList();
            overrides = new ObservableCollection<ProjectOverrideManager>(flattenedOverrides
                .Select(o => new ProjectOverrideManager(Session, this, o.ActionObjectId, o.Parameter)));
            logicItems = new ObservableCollection<LogicItemManager>(project.LogicItems.Select(l => new LogicItemManager(Session, this, l)));
            // Project functions are not really needed atm...
            Parameters = new ReadOnlyObservableCollection<ProjectParameterManager>(parameters);
            ActionPoints = new ReadOnlyObservableCollection<ActionPointManager>(actionPoints);
            Overrides = new ReadOnlyObservableCollection<ProjectOverrideManager>(overrides);
            LogicItems = new ReadOnlyObservableCollection<LogicItemManager>(logicItems);
        }

        /// <summary>
        /// Returns a collection of packages based of this project.
        /// </summary>
        public IList<PackageManager> GetPackages() {
            return Session.Packages.Where(p => p.ProjectId == Id).ToList();
        }

        /// <summary>
        /// Renames the project.
        /// </summary>
        /// <param name="newName">New name for the project.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task RenameAsync(string newName) {
            await LibraryLockAsync();
            var response = await Session.Client.RenameProjectAsync(new RenameProjectRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming project {Id} failed.", response.Messages);
            }
            // Unlocked by the server.
        }

        /// <summary>
        /// Opens the project.
        /// </summary>
        /// <remarks>
        /// The session must be in a menu.
        ///
        /// For extra caution, make sure that the project is actually opened by invoking <see cref="IsOpen"/>
        /// or checking if the <see cref="Arcor2Session.NavigationState"/> is in the <see cref="NavigationState.Project"/> state with corresponding project ID.
        /// 
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task OpenAsync() {
            var response = await Session.Client.OpenProjectAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Opening project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Closes the project.
        /// </summary>
        /// <remarks>
        /// Project must be open on invocation. Will fail with unsaved changes and if unforced.
        /// </remarks>
        /// <param name="force">If true, the project will be closed even with unsaved changes, etc... </param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CloseAsync(bool force = false) {
            var response = await Session.Client.CloseProjectAsync(new CloseProjectRequestArgs(force));
            if(!response.Result) {
                throw new Arcor2Exception($"Closing project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Saves the project.
        /// </summary>
        /// <remarks>
        /// Project must be open on invocation.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SaveAsync() {
            var response = await Session.Client.SaveProjectAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Saving project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Check if the project has unsaved changes.
        /// </summary>
        /// <remarks>
        /// Project must be open on invocation.
        /// </remarks>
        public async Task<bool> HasUnsavedChangesAsync() {
            var response = await Session.Client.SaveProjectAsync(true);
            return !response.Result;
        }

        /// <summary>
        /// Deletes the project.
        /// </summary>
        /// <remarks>
        /// The project must be closed.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.Client.RemoveProjectAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the description of the project.
        /// </summary>
        /// <param name="newDescription">The new description.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateDescriptionAsync(string newDescription) {
            var response = await Session.Client.UpdateProjectDescriptionAsync(new UpdateProjectDescriptionRequestArgs(Id, newDescription));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating description of project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Loads the project fully without opening it.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task LoadAsync() {
            var response = await Session.Client.GetProjectAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Loading project {Id} failed.", response.Messages);
            }
            UpdateAccordingToNewObject(response.Data);
        }

        /// <summary>
        /// Duplicates the project.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task DuplicateAsync(string newName) {
            var response = await Session.Client.DuplicateProjectAsync(new CopyProjectRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Duplicating project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Sets if the project should contain logic.
        /// </summary>
        /// <param name="hasLogic">Should logic be enabled?</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetHasLogicAsync(bool hasLogic) {
            var response = await Session.Client.UpdateProjectHasLogicAsync(new UpdateProjectHasLogicRequestArgs(Id, hasLogic));
            if(!response.Result) {
                throw new Arcor2Exception($"Setting HasLogic for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Builds the project into package.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task BuildIntoPackageAsync(string packageName) {
            var response = await Session.Client.BuildProjectAsync(new BuildProjectRequestArgs(Id, packageName));
            if(!response.Result) {
                throw new Arcor2Exception($"Building project {Id} into package failed.", response.Messages);
            }
        }

        /// <summary>
        /// Builds the project into a temporary package and runs it.
        /// </summary>
        /// <remarks>
        /// A saved project must be opened.
        /// Invalid breakpoint IDs will not throw an exception, but rather lead to <see cref="PackageManager.ExceptionOccured"/> event and closing of the package.
        /// </remarks>
        /// <param name="startPaused">Should the package start paused? By default, "true".</param>
        /// <param name="breakPoints">A list of breakpoints (action point IDs). The package will pause before executing them.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task BuildIntoTemporaryPackageAndRunAsync(List<string> breakPoints, bool startPaused = true) {
            var response = await Session.Client.RunTemporaryPackageAsync(new TemporaryPackageRequestArgs(startPaused, breakPoints));
            if(!response.Result) {
                throw new Arcor2Exception($"Building project {Id} into temporary package failed.", response.Messages);
            }
        }

        /// <summary>
        /// Builds the project into a temporary package and runs it.
        /// </summary>
        /// <remarks>
        /// Invalid breakpoint IDs will not throw an exception, but rather lead to <see cref="PackageManager.ExceptionOccured"/> event and closing of the package.
        /// </remarks>
        /// <param name="startPaused">Should the package start paused? By default, "true".</param>
        /// <param name="breakPoints">A list of breakpoints (action point IDs). The package will pause before executing them.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task BuildIntoTemporaryPackageAndRunAsync(List<ActionPointManager> breakPoints, bool startPaused = true) {
            var breakPointIds = breakPoints.Select(a => a.Id).ToList();
            await BuildIntoTemporaryPackageAndRunAsync(breakPointIds, startPaused);
        }

        /// <summary>
        /// Builds the project into a temporary package and runs it.
        /// </summary>
        /// <param name="startPaused">Should the package start paused? By default, "true".</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task BuildIntoTemporaryPackageAndRunAsync(bool startPaused = true) {
            await BuildIntoTemporaryPackageAndRunAsync(new List<string>(), startPaused);
        }

        /// <summary>
        /// Adds a new project parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <remarks>
        /// The project must be opened.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddProjectParameterAsync(string name, string type, string value) {
            var response = await Session.Client.AddProjectParameterAsync(new AddProjectParameterRequestArgs(name, type, value));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding project parameter for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action point.
        /// </summary>
        /// <param name="name">The name of the action point.</param>
        /// <param name="position">The position of the action point.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointAsync(string name, Position position) {
            var response = await Session.Client.AddActionPointAsync(new AddActionPointRequestArgs(name, position, string.Empty));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding action point for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action point.
        /// </summary>
        /// <param name="name">The name of the action point.</param>
        /// <param name="position">The position of the action point.</param>
        /// <param name="parent">The parent action point.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointAsync(string name, Position position, ActionPointManager parent) {
            var response = await Session.Client.AddActionPointAsync(new AddActionPointRequestArgs(name, position, parent.Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding action point for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action point.
        /// </summary>
        /// <param name="name">The name of the action point.</param>
        /// <param name="position">The position of the action point.</param>
        /// <param name="parent">The parent action object.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointAsync(string name, Position position, ActionObjectManager parent) {
            var response = await Session.Client.AddActionPointAsync(new AddActionPointRequestArgs(name, position, parent.Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding action point for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action point using a robot. Uses the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObjectId">The robot.</param>
        /// <param name="name">The name of the action point.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, string actionObjectId) {
            await AddActionPointUsingRobotAsync(name, actionObjectId, "default", null!);
        }

        /// <summary>
        /// Adds a new action point using a robot. Uses the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="name">The name of the action point.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, ActionObjectManager actionObject) {
            await AddActionPointUsingRobotAsync(name, actionObject.Id, "default", null!);
        }

        /// <summary>
        /// Adds a new action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="name">The name of the action point.</param>
        /// <param name="endEffectorId">The ID of the end effector.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, ActionObjectManager actionObject, string endEffectorId) {
            await AddActionPointUsingRobotAsync(name, actionObject.Id, endEffectorId, null!);
        }

        /// <summary>
        /// Adds a new action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="name">The name of the action point.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, ActionObjectManager actionObject, EndEffector endEffector) {
            await AddActionPointUsingRobotAsync(name, actionObject.Id, endEffector.Id, null!);
        }

        /// <summary>
        /// Adds a new action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="name">The name of the action point.</param>
        /// <param name="endEffectorId">The ID of the end effector.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, ActionObjectManager actionObject, string endEffectorId, string armId) {
            await AddActionPointUsingRobotAsync(name, actionObject.Id, endEffectorId, armId);
        }

        /// <summary>
        /// Adds a new action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObjectId">The robot ID.</param>
        /// <param name="name">The name of the action point.</param>
        /// <param name="endEffectorId">The ID of the end effector.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, string actionObjectId, string endEffectorId, string armId) {
            var response = await Session.Client.AddActionPointUsingRobotAsync(new AddApUsingRobotRequestArgs(actionObjectId, endEffectorId, name, armId));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding action point using robot for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="name">The name of the action point.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointUsingRobotAsync(string name, ActionObjectManager actionObject, EndEffector? endEffector, string armId) {
            await AddActionPointUsingRobotAsync(actionObject.Id, endEffector?.Id ?? "default", name, armId);
        }

        /// <summary>
        /// Adds a new action point.
        /// </summary>
        /// <param name="name">The name of the action point.</param>
        /// <param name="position">The position of the action point.</param>
        /// <param name="parentId">The ID of the parent object.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionPointAsync(string name, Position position, string parentId) {
            var response = await Session.Client.AddActionPointAsync(new AddActionPointRequestArgs(name, position, parentId));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding project parameter for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a project override for an action object.
        /// </summary>
        /// <param name="actionObjectId">The ID of an action object.</param>
        /// <param name="parameter">The overriden parameter of an action object.</param>
        /// <remarks>
        /// The project must be opened.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddOverrideAsync(string actionObjectId, Parameter parameter) {
            await LibraryLockAsync(actionObjectId);
            var response = await Session.Client.AddOverrideAsync(new AddOverrideRequestArgs(actionObjectId, parameter));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Adding project override for project {Id} failed.", response.Messages);
            }
            await LibraryUnlockAsync(actionObjectId);
        }

        /// <summary>
        /// Adds a project override for an action object.
        /// </summary>
        /// <param name="actionObject">The action object.</param>
        /// <param name="parameter">The overriden parameter of an action object.</param>
        /// <remarks>
        /// The project must be opened.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddOverrideAsync(ActionObjectManager actionObject, Parameter parameter) {
            await AddOverrideAsync(actionObject.Id, parameter);
        }

        /// <summary>
        /// Adds a new logic item.
        /// </summary>
        /// <remarks>
        /// Use overload with string ID parameters to set the "START" and "END". Connection to these can only exist one at a time.
        /// Start and end actions should be different. If an action has multiple start connections (logic item), they should have conditions.
        /// </remarks>
        /// <param name="startId">The starting action ID, alternatively, "START" for the first action.</param>
        /// <param name="endId">The ending action ID, alternatively, "END" for the last action.</param>
        /// <param name="condition">The condition, <c>null</c> if not applicable.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddLogicItem(string startId, string endId, ProjectLogicIf? condition = null) {
            var response = await Session.Client.AddLogicItemAsync(new AddLogicItemRequestArgs(startId, endId, condition!));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding logic item for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new logic item.
        /// </summary>
        /// <remarks>
        /// Use overload with string ID parameters to set the "START" and "END". Connection to these can only exist one at a time.
        /// Start and end actions should be different. If an action has multiple start connections (logic item), they should have conditions.
        /// </remarks>
        /// <param name="start">The starting action ID.</param>
        /// <param name="end">The ending action ID.</param>
        /// <param name="condition">The condition, <c>null</c> if not applicable.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddLogicItem(ActionManager start, ActionManager end, ProjectLogicIf? condition = null) {
            await AddLogicItem(start.Id, end.Id, condition);
        }


        /// <summary>
        /// Updates the project according to the <paramref name="project"/> instance.
        /// </summary>
        /// <param name="project">Newer version of the project.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(Project project) {
            if(Id != project.Id) {
                throw new InvalidOperationException($"Can't update a ProjectManager ({Id}) using a project data object ({project.Id}) with different ID.");
            }

            UpdateData(project.MapToBareProject());
            parameters.UpdateListOfLockableArcor2Objects<ProjectParameterManager, ProjectParameter, ProjectParameter>(project.Parameters,
                p => p.Id,
                (m, p) => m.UpdateAccordingToNewObject(p),
                p => new ProjectParameterManager(Session, this, p));
            actionPoints.UpdateListOfLockableArcor2Objects<ActionPointManager, ActionPoint, BareActionPoint>(project.ActionPoints,
                a => a.Id,
                (m, a) => m.UpdateAccordingToNewObject(a),
                a => new ActionPointManager(Session, this, a));
            var flattenedOverrides =
                project.ObjectOverrides.SelectMany(o => o.Parameters, (@override, parameter) => (ActionObjectId: @override.Id, Parameter: parameter)).ToList();
            overrides.UpdateListOfArcor2Objects<ProjectOverrideManager, (string ActionObjectId, Parameter Parameter), ProjectOverride>(flattenedOverrides,
                (m, o) => m.Data.ActionObjectId == o.ActionObjectId && m.Data.Parameter.Name == o.Parameter.Name && m.Data.Parameter.Type == o.Parameter.Type,
                (m, o) => m.UpdateAccordingToNewObject(o.Parameter),
                o => new ProjectOverrideManager(Session, this, o.ActionObjectId, o.Parameter));
            logicItems.UpdateListOfLockableArcor2Objects<LogicItemManager, LogicItem, LogicItem>(project.LogicItems,
                l => l.Id,
                (m, l) => m.UpdateAccordingToNewObject(l),
                l => new LogicItemManager(Session, this, l));
        }

        /// <summary>
        /// Updates the project according to the <paramref name="project"/> instance.
        /// </summary>
        /// <param name="project">Newer version of the project.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(BareProject project) {
            if(Id != project.Id) {
                throw new InvalidOperationException($"Can't update a ProjectManager ({Id}) using a project data object ({project.Id}) with different ID.");
            }
            UpdateData(project);
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.ProjectSaved += Saved;
            Session.Client.ProjectRemoved += OnProjectRemoved;
            Session.Client.ProjectBaseUpdated += OnProjectBaseUpdated;
            Session.Client.ProjectParameterAdded += OnProjectParameterAdded;
            Session.Client.ActionPointAdded += OnActionPointAdded;
            Session.Client.ProjectOverrideAdded += OnProjectOverrideAdded;
            Session.Client.LogicItemAdded += OnLogicItemAdded;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.ProjectSaved -= Saved;
            Session.Client.ProjectRemoved -= OnProjectRemoved;
            Session.Client.ProjectBaseUpdated -= OnProjectBaseUpdated;
            Session.Client.ProjectParameterAdded -= OnProjectParameterAdded;
            Session.Client.ActionPointAdded -= OnActionPointAdded;
            Session.Client.ProjectOverrideAdded -= OnProjectOverrideAdded;
            Session.Client.LogicItemAdded -= OnLogicItemAdded;
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if(ActionPoints != null) {
                foreach(var actionPoint in ActionPoints) {
                    actionPoint.Dispose();
                }
            }
            if(Parameters != null) {
                foreach(var parameter in Parameters) {
                    parameter.Dispose();
                }
            }
            if(Overrides != null) {
                foreach(var @override in Overrides) {
                    @override.Dispose();
                }
            }
            if(LogicItems != null) {
                foreach(var logicItem in LogicItems) {
                    logicItem.Dispose();
                }
            }
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs e) {
            if(e.Data.Id == Id) {
                UpdateData(e.Data);
            }
        }

        private void OnProjectRemoved(object sender, BareProjectEventArgs e) {
            if(e.Data.Id == Id) {
                RemoveData();
                Session.projects.Remove(this);
                Dispose();
            }
        }

        private void OnProjectParameterAdded(object sender, ProjectParameterEventArgs e) {
            if(IsOpen) {
                if(Parameters == null) {
                    Session.Logger?.LogError($"When adding a new project parameter, the parameters collection for project {Id} was null.");
                }

                parameters?.Add(new ProjectParameterManager(Session, this, e.Data));
            }
        }

        private void OnActionPointAdded(object sender, BareActionPointEventArgs e) {
            if(IsOpen) {
                if(ActionPoints == null) {
                    Session.Logger?.LogError($"When adding a new action point, the action point collection for project {Id} was null.");
                }

                actionPoints?.Add(new ActionPointManager(Session, this, e.Data));
            }
        }

        private void OnProjectOverrideAdded(object sender, ParameterEventArgs e) {
            if(IsOpen) {
                if(Overrides == null) {
                    Session.Logger?.LogError($"When adding a new project override, the override collection for project {Id} was null.");
                    return;
                }
                
                overrides?.Add(new ProjectOverrideManager(Session, this, e.ParentId, e.Data));
            }
        }

        private void OnLogicItemAdded(object sender, LogicItemEventArgs e) {
            if(IsOpen) {
                if(LogicItems == null) {
                    Session.Logger?.LogError($"When adding a new logic item, the logic item collection for project {Id} was null.");
                    return;
                }

                logicItems?.Add(new LogicItemManager(Session, this, e.Data));
            }
        }
    }
}
