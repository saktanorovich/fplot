using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FPlot.Model;
using FPlot.Utils;
using ScottPlot.Avalonia;

namespace FPlot.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Window? owner;
    private string? selectedPath;

    public ObservableCollection<PointViewModel> Points { get; }
    public GridViewModel Grid { get; }
    public PlotViewModel Plot { get; }

    public string? SelectedPath
    {
        get => selectedPath;
        set => SetProperty(ref selectedPath, value);
    }

    public MainViewModel(Window owner)
    {
        this.owner = owner;
        Points = MainViewModelInit.CreateInitPoints();
        SelectedPath = MainViewModelInit.DefaultPath;
        Grid = new GridViewModel();
        Plot = new PlotViewModel();
    }

    public void Attach(DataGrid grid)
    {
        Grid.Initialize(this);
    }
    
    public void Attach(AvaPlot plot)
    {
        Plot.Initialize(this, plot);
    }

    #region Mediator

    private void NotifyGrid()
    {
        Grid.Update();
    }

    private void NotifyGrid(PointViewModel pointViewModel)
    {
        Grid.Update(pointViewModel);
    }

    public void NotifyGrid(Point2d point, int index)
    {
        Grid.Update(point, index);
    }

    public void NotifyPlot()
    {
        Plot.Update();
    }

    #endregion

    #region Commands (File)

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (owner is null)
            return;
        var file = await owner.OpenFileAsync();
        if (file is not null)
        {
            var points = await owner.ReadPointsAsync(file);
            Invalidate(points);
            SelectedPath = file.Path.AbsolutePath;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveFileAsync))]
    private async Task SaveFileAsync()
    {
        if (owner is null)
            return;
        var points = Points.Select(point => new Point2d(point.X, point.Y)).ToList();
        var pointsSaved = await owner.SaveFileAsync(points, SelectedPath);
        if (pointsSaved)
        {
            Invalidate();
        }
    }

    private bool CanSaveFileAsync()
    {
        return File.Exists(SelectedPath) && Points.Any(point => point.HasChanges());
    }

    [RelayCommand]
    private async Task SaveAsFileAsync()
    {
        if (owner is null)
            return;
        var points = Points.Select(point => new Point2d(point.X, point.Y)).ToList();
        var pointsSavedTo = await owner.SaveFileAsync(points);
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
        if (owner is null)
            return;
        var pointViewModel = new PointViewModel(new Point2d(0, 0)); 
        Points.Add(pointViewModel);
        NotifyGrid(pointViewModel);
        NotifyPlot();
    }

    [RelayCommand]
    private void RemovePoint()
    {
        if (owner is null)
            return;
    }

    [RelayCommand(CanExecute = nameof(CanCopyAsync))]
    private async Task CopyAsync()
    {
        if (owner is null)
            return;
        await owner.SetClipboardAsync(Points.Select(
            point => new Point2d(point.X, point.Y)).ToList());
    }

    private bool CanCopyAsync()
    {
        return Points.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanPasteAsync))]
    private async Task PasteAsync()
    {
        if (owner is null)
            return;
        var clipboard = await owner.GetClipboardAsync();
        if (clipboard is not null)
        {
            var points = await owner.ReadPointsAsync(clipboard);
            if (points.Count > 0)
            {
                Invalidate(points);
                SelectedPath = $"{System.Environment.MachineName}/Clipboard";
            }
        }
    }

    private bool CanPasteAsync()
    {
        if (owner is null)
            return false;
        var hasDataTask = owner.HasClipboardAsync();
        return hasDataTask.GetAwaiter().GetResult();
    }

    #endregion

    #region Private Methods
    
    private void Invalidate(List<Point2d> points)
    {
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
        foreach (var point in Points)
        {
            point.Invalidate();
        }
    }

    #endregion
}
