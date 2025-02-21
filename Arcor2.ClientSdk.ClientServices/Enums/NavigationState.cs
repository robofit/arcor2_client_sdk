namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a client view that is expected by the server.
    /// </summary>
    public enum NavigationState {
        /// <summary>
        /// Default state. No information about the requested view.
        /// </summary>
        None = 0,
        /// <summary>
        /// Menu with a list of scenes.
        /// </summary>
        MenuListOfScenes,
        /// <summary>
        /// Menu with a list of projects.
        /// </summary>
        MenuListOfProjects,
        /// <summary>
        /// Menu with a list of packages.
        /// </summary>
        MenuListOfPackages,
        /// <summary>
        /// Occurs when scene is closed, but the server didn't request to show main menu yet.
        /// </summary>
        SceneClosed,
        /// <summary>
        /// Occurs when project is closed, but the server didn't request to show main menu yet.
        /// </summary>
        ProjectClosed,
        /// <summary>
        /// A scene.
        /// </summary>
        Scene,
        /// <summary>
        /// A project.
        /// </summary>
        Project,
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
