using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Windows;

public partial class SceneWindow : FluentWindow {
    public SceneWindowViewModel ViewModel { get; }

    public SceneWindow(
        SceneWindowViewModel viewModel
    ) {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = this;

        Loaded += (s, e) => {
            SizeChanged += OnSizeChanged;
            UpdateLayout();
        };
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        UpdateLayout();
    }

    private void UpdateLayout() {
        ViewModel.UpdateVisualization(VisualizationCanvas.ActualWidth,
           VisualizationCanvas.ActualHeight);
    }
}