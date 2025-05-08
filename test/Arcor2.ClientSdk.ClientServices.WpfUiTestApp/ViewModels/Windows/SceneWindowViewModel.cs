using Arcor2.ClientSdk.ClientServices.Managers;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Services;
using System.Collections.ObjectModel;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Windows;

public partial class SceneWindowViewModel(Arcor2Service arcor2Service, SceneManager scene) : ObservableObject {
    [ObservableProperty] private SceneManager _scene = scene;
    private double _canvasWidth;
    private double _canvasHeight;
    private readonly double _scaleUnit = 50; // Number of pixels per unit
    private readonly int _gridSpacing = 1; // Draw grid lines every 5 units


    public ObservableCollection<GridLine> GridLinesX { get; set; } = new();
    public ObservableCollection<GridLine> GridLinesY { get; set; } = new();

    [ObservableProperty] private double _centerX;
    [ObservableProperty] private double _centerY;


    public void UpdateVisualization(double width, double height) {
        _canvasWidth = width;
        _canvasHeight = height;
        CenterX = width / 2;
        CenterY = height / 2;

        UpdateGridLines();
    }

    private void UpdateGridLines() {
        GridLinesX.Clear();
        GridLinesY.Clear();

        int halfUnitsX = (int) Math.Ceiling(_canvasWidth / (2 * _scaleUnit));
        int halfUnitsY = (int) Math.Ceiling(_canvasHeight / (2 * _scaleUnit));

        for(int i = -halfUnitsY; i <= halfUnitsY; i++) {
            if(i % _gridSpacing == 0 && i != 0) {
                double y = CenterY - (i * _scaleUnit);
                if(y >= 0 && y <= _canvasHeight) {
                    GridLinesX.Add(new GridLine { Y = y, Width = _canvasWidth });
                }
            }
        }

        for(int i = -halfUnitsX; i <= halfUnitsX; i++) {
            if(i % _gridSpacing == 0 && i != 0) {
                double x = CenterX + (i * _scaleUnit);
                if(x >= 0 && x <= _canvasWidth) {
                    GridLinesY.Add(new GridLine { X = x, Height = _canvasHeight });
                }
            }
        }
    }
}

public record GridLine {
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public record ScaleLabel {
    public string Text { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
