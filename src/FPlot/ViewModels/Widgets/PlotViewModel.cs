using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FPlot.Model;

namespace FPlot.ViewModels;

public class PlotViewModel : ObservableObject
{
    private static readonly Avalonia.Input.Cursor CursorHand  = new(StandardCursorType.Hand);
    private static readonly Avalonia.Input.Cursor CursorArrow = new(StandardCursorType.Arrow);

    private MainViewModel? owner;
    private IList<PointViewModel>? points;
    private AvaPlot? avaPlot;
    private Scatter? scatter;
    private double[]? xs;
    private double[]? ys;
    private int? pointIndex;

    public void Initialize(MainViewModel owner, AvaPlot avaPlot)
    {
        this.owner = owner;
        this.avaPlot = avaPlot;
        var plot = avaPlot.Plot;
        plot.Title("Plot");
        plot.XLabel("X Value");
        plot.YLabel("Y Value");
        avaPlot.PointerPressed += OnMouseDown;
        avaPlot.PointerReleased += OnMouseUp;
        avaPlot.PointerMoved += OnMouseMove;
        Update();
    }
    
    public void Update()
    {
        points = owner?.Points;
        if (points is null)
            return;
        avaPlot?.Plot.Clear();
        xs = points.Select(point => point.X).ToArray();
        ys = points.Select(point => point.Y).ToArray();
        scatter = avaPlot?.Plot.Add.Scatter(xs, ys);
        if (scatter is not null)
        {
            scatter.MarkerSize = 10;
            scatter.Smooth = true;
        }
        avaPlot?.Plot.Axes.AutoScale();
        avaPlot?.Refresh();
    }

    private void OnMouseDown(object? sender, PointerEventArgs e)
    {
        if (avaPlot is null || scatter is null)
            return;
        var pos = e.GetPosition(avaPlot);
        var mousePixel = new Pixel(pos.X, pos.Y);
        var mouseLocation = avaPlot.Plot.GetCoordinates(mousePixel);
        var nearest = scatter.Data.GetNearest(mouseLocation, avaPlot.Plot.LastRender);
        pointIndex = nearest.IsReal ? nearest.Index : null;
        if (pointIndex.HasValue)
        {
            avaPlot.UserInputProcessor.Disable();
        }
    }

    private void OnMouseUp(object? sender, PointerEventArgs e)
    {
        pointIndex = null;
        avaPlot?.UserInputProcessor.Enable();
        avaPlot?.Refresh();
    }

    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        if (avaPlot is null || scatter is null)
            return;
        var pos = e.GetPosition(avaPlot);
        var mousePixel = new Pixel(pos.X, pos.Y);
        var mouseLocation = avaPlot.Plot.GetCoordinates(mousePixel);
        var nearest = scatter.Data.GetNearest(mouseLocation, avaPlot.Plot.LastRender);
        avaPlot.Cursor = nearest.IsReal ? CursorHand : CursorArrow;
        if (pointIndex.HasValue && xs is not null && ys is not null)
        {
            var point = new Point2d(mouseLocation.X, mouseLocation.Y);
            var index = pointIndex.Value;
            xs[index] = mouseLocation.X;
            ys[index] = mouseLocation.Y;
            avaPlot.Refresh();
            owner?.NotifyGrid(point, index);
        }
    }
}