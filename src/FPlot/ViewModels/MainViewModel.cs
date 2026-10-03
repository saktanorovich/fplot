using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FPlot.Math;
using FPlot.Services;

namespace FPlot.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPointsIoService io;
    private string? selectedPath;
    private bool pointsRemoved;
    private PointViewModel? clickedPoint;
    private bool showInverse;

    public ObservableCollection<PointViewModel> Points { get; }
    public GridViewModel Grid { get; }
    public PlotViewModel Plot { get; }

    // The point last clicked on the plot; Delete removes it.
    public PointViewModel? ClickedPoint => clickedPoint;

    public bool ShowInverse
    {
        get => showInverse;
        set
        {
            if (SetProperty(ref showInverse, value))
            {
                Plot.Update();
            }
        }
    }

    public string? SelectedPath
    {
        get => selectedPath;
        set => SetProperty(ref selectedPath, value);
    }

    public MainViewModel(IPointsIoService io)
    {
        this.io = io;
        Points = MainViewModelInit.CreateInitPoints();
        SelectedPath = MainViewModelInit.DefaultPath;
        Grid = new GridViewModel();
        Plot = new PlotViewModel();
        Grid.Initialize(this);
        Plot.Initialize(this);
    }

    #region Mediator

    private void NotifyGrid()
    {
        Grid.Update();
    }

    private void NotifyGrid(PointViewModel pointViewModel)
    {
        Grid.Update(pointViewModel);
        SaveFileCommand.NotifyCanExecuteChanged();
    }

    public void NotifyGrid(Point2d point, int index)
    {
        Grid.Update(point, index);
        SaveFileCommand.NotifyCanExecuteChanged();
        RemovePointCommand.NotifyCanExecuteChanged();
    }

    public void NotifyPlot()
    {
        Plot.Update();
        SaveFileCommand.NotifyCanExecuteChanged();
        RemovePointCommand.NotifyCanExecuteChanged();
    }

    public void NotifySelection()
    {
        RemovePointCommand.NotifyCanExecuteChanged();
    }

    public void NotifyClick(int index)
    {
        clickedPoint = Points[index];
        RemovePointCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Commands (File)

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var opened = await io.OpenAsync();
        if (opened is { } file)
        {
            Invalidate(file.Points);
            SelectedPath = file.Path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveFileAsync))]
    private async Task SaveFileAsync()
    {
        var points = Points.Select(point => new Point2d(point.X, point.Y)).ToList();
        var pointsSaved = await io.SaveAsync(points, SelectedPath);
        if (pointsSaved)
        {
            Invalidate();
        }
    }

    private bool CanSaveFileAsync()
    {
        return File.Exists(SelectedPath) && (pointsRemoved || Points.Any(point => point.HasChanges()));
    }

    [RelayCommand]
    private async Task SaveAsFileAsync()
    {
        var points = Points.Select(point => new Point2d(point.X, point.Y)).ToList();
        var pointsSavedTo = await io.SaveAsAsync(points);
        if (pointsSavedTo != null)
        {
            Invalidate();
            SelectedPath = pointsSavedTo;
        }
    }

    #endregion

    #region Commands (Edit)

    [RelayCommand]
    private void AddPoint()
    {
        var pointViewModel = GetLastPoint();
        Points.Add(pointViewModel);
        NotifyGrid(pointViewModel);
        NotifyPlot();
    }

    private PointViewModel GetLastPoint()
    {
        var lastPoint = Points.LastOrDefault();
        var point = lastPoint is null ? new Point2d(0, 0) : new Point2d(lastPoint.X + 1, lastPoint.Y);
        return new PointViewModel(point, added: true);
    }

    [RelayCommand(CanExecute = nameof(CanRemovePoint))]
    private void RemovePoint()
    {
        var pointViewModel = clickedPoint;
        if (pointViewModel is null)
        {
            return;
        }

        if (Grid.SelectedPoint == pointViewModel)
        {
            Grid.SelectedPoint = null;
        }
        clickedPoint = null;
        Grid.Remove(pointViewModel);
        Points.Remove(pointViewModel);
        pointsRemoved = true;
        NotifyPlot();
    }

    private bool CanRemovePoint()
    {
        return clickedPoint is not null;
    }

    [RelayCommand(CanExecute = nameof(CanCopyAsync))]
    private async Task CopyAsync()
    {
        await io.CopyAsync(Points.Select(
            point => new Point2d(point.X, point.Y)).ToList());
    }

    private bool CanCopyAsync()
    {
        return Points.Count > 0;
    }

    [RelayCommand]
    private async Task PasteAsync()
    {
        var points = await io.PasteAsync();
        if (points is { Count: > 0 })
        {
            Invalidate(points);
            SelectedPath = $"{System.Environment.MachineName}/Clipboard";
        }
    }

    #endregion

    #region Private Methods
    
    private void Invalidate(List<Point2d> points)
    {
        pointsRemoved = false;
        clickedPoint = null;
        Points.Clear();
        points.ForEach(point =>
        {
            var pointViewModel = new PointViewModel(point);
            Points.Add(pointViewModel);
        });
        NotifyGrid();
        NotifyPlot();
    }

    private void Invalidate()
    {
        pointsRemoved = false;
        foreach (var point in Points)
        {
            point.Invalidate();
        }
        Plot.UpdateMarkers();
        SaveFileCommand.NotifyCanExecuteChanged();
        RemovePointCommand.NotifyCanExecuteChanged();
    }

    #endregion
}
