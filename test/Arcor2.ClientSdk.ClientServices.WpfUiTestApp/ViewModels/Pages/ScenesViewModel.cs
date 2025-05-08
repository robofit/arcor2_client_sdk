using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Managers;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Services;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Windows;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Windows;
using System.Collections.ObjectModel;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Pages;

public partial class ScenesViewModel(Arcor2Service arcor2Service, ISnackbarService snackbar) : ObservableObject, INavigationAware {
    private FluentWindow? _sceneWindow;
    private bool _isInitialized = false;

    [ObservableProperty]
    private ReadOnlyObservableCollection<SceneManager> _scenes = new(new ObservableCollection<SceneManager>());

    [ObservableProperty] 
    private bool _isInMenu = false;

    public void OnNavigatedTo() {
        if(!_isInitialized && arcor2Service.Session is not null) {
            InitializeViewModel();
            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom() {
        _sceneWindow?.Close();
    }

    private void InitializeViewModel() {
        Scenes = arcor2Service.Session!.Scenes;
        IsInMenu = arcor2Service.Session.NavigationState.IsInMenu();
        arcor2Service.Session!.NavigationStateChanged += (_, args) => {
            IsInMenu = args.State.IsInMenu();
            Scenes = new ReadOnlyObservableCollection<SceneManager>(new ObservableCollection<SceneManager>(arcor2Service.Session!.Scenes));
        };

        var openedScene = Scenes.FirstOrDefault(s => s.IsOpen);
        if(openedScene != null) {
            _sceneWindow = new SceneWindow(new SceneWindowViewModel(arcor2Service, openedScene));
            _sceneWindow.Show();
        }
    }

    [RelayCommand]
    private async Task OpenSceneDetail(SceneManager sceneManager) {
        await sceneManager.OpenAsync();
        _sceneWindow = new SceneWindow(new SceneWindowViewModel(arcor2Service, sceneManager));
        _sceneWindow.Show();
    }

    [RelayCommand]
    private async Task CloseSceneDetail(SceneManager sceneManager) {
        await sceneManager.CloseAsync();
        _sceneWindow?.Close();
    }

    [RelayCommand]
    private async Task DeleteScene(SceneManager sceneManager) => await sceneManager.RemoveAsync();

    [RelayCommand]
    private async Task DuplicateScene(SceneManager sceneManager) => await sceneManager.DuplicateAsync($"Copy of {sceneManager.Data.Name}");
}