namespace Arcor2.ClientSdk.ClientServices.Enums {
    public enum NavigationState {
        None = 0,
        MenuListOfScenes,
        MenuListOfProjects,
        MenuListOfPackages,
        /// <summary>
        /// Occurs when scene is closed, but the server didn't send ShowMainMenu yet.
        /// </summary>
        SceneClosed,
        /// <summary>
        /// Occurs when project is closed, but the server didn't send ShowMainMenu yet.
        /// </summary>
        ProjectClosed,
        Scene,
        Project,
        Package
    }
}
