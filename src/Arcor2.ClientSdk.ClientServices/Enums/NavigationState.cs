namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a client view that is expected by the server.
    /// </summary>
    public enum NavigationState {
        /// <summary>
        /// The default state. No information about the requested view.
        /// </summary>
        None = 0,
        /// <summary>
        /// Menu with a list of scenes.
        /// </summary>
        /// <remarks>
        /// The distinction of what type of object should the menu list is purely informational. There are no restrictions on available RPCs, event, or other behavior.
        /// This state usually follows a scene closing and, as of ARCOR2 server 1.5.0, this is the default server state.
        /// </remarks>
        MenuListOfScenes,
        /// <summary>
        /// Menu with a list of projects.
        /// </summary>
        /// <remarks>
        /// The distinction of what type of object should the menu list is purely informational. There are no restrictions on available RPCs, event, or other behavior.
        /// This state usually follows a project closing.
        /// </remarks>
        MenuListOfProjects,
        /// <summary>
        /// Menu with a list of packages.
        /// </summary>
        /// <remarks>
        /// The distinction of what type of object should the menu list is purely informational. There are no restrictions on available RPCs, event, or other behavior.
        /// This state usually follows a package stopping.
        /// </remarks>
        MenuListOfPackages,
        /// <summary>
        /// Occurs when scene is closed, but the server didn't request to show main menu yet.
        /// </summary>
        /// <remarks>
        /// This state can be either be ignored or a loading screen can be shown. One of the menu states usually quickly follows.
        /// </remarks>
        SceneClosed,
        /// <summary>
        /// Occurs when project is closed, but the server didn't request to show main menu yet.
        /// </summary>
        /// <remarks>
        /// This state can be either be ignored or a loading screen can be shown. One of the menu states usually quickly follows.
        /// </remarks>
        ProjectClosed,
        /// <summary>
        /// A scene.
        /// </summary>
        Scene,
        /// <summary>
        /// A project.
        /// </summary>
        Project,
        /// <summary>
        /// A package.
        /// </summary>
        Package
    }

    /// <summary>
    /// A class adding helper methods for the <see cref="NavigationState"/> enum.
    /// </summary>
    public static class NavigationStateExtensions {
        /// <summary>
        /// Determines if a navigation is in one of its menu states.
        /// </summary>
        /// <param name="state">The current state.</param>
        /// <returns><c>true</c> if the state is one of the menu states.</returns>
        public static bool IsInMenu(this NavigationState state) {
            return state == NavigationState.MenuListOfScenes ||
                   state == NavigationState.MenuListOfProjects ||
                   state == NavigationState.MenuListOfPackages;
        }
    }
}
