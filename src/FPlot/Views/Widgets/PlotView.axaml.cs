using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using FPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;

namespace FPlot.Views;

public partial class PlotView : UserControl
{
    private static readonly Avalonia.Input.Cursor CursorHand  = new(StandardCursorType.Hand);
    private static readonly Avalonia.Input.Cursor CursorArrow = new(StandardCursorType.Arrow);
    // Same orange as the "Unsaved changes" indicator in MenuView.
    private static readonly Color MovedColor = Color.FromHex("#E67E22");
    // Same red as the Delete button in MenuView.
    private static readonly Color InverseColor = Color.FromHex("#C0392B");
    private const float PointSize = 10;
    private const float ClickedPointSize = PointSize * 1.4f;

    private PlotViewModel? viewModel;
    private Scatter? scatter;
    private Scatter? inverse;
    private double[]? xs;
    private double[]? ys;
    private readonly List<Marker> markers = new();

    public PlotView()
    {
        InitializeComponent();
        PlotControl.Plot.XLabel("X");
        PlotControl.Plot.YLabel("Y = f(X)");
        PlotControl.PointerPressed += OnMouseDown;
        PlotControl.PointerReleased += OnMouseUp;
        PlotControl.PointerMoved += OnMouseMove;
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (viewModel is not null)
        {
            viewModel.DataChanged -= Redraw;
            viewModel.MarkersChanged -= RedrawMarkers;
        }
        viewModel = DataContext as PlotViewModel;
        if (viewModel is not null)
        {
            viewModel.DataChanged += Redraw;
            viewModel.MarkersChanged += RedrawMarkers;
            Redraw();
        }
    }

    private void Redraw()
    {
        if (viewModel is null)
        {
            return;
        }

        var plot = PlotControl.Plot;
        plot.Clear();
        markers.Clear();
        xs = viewModel.Points.Select(point => point.X).ToArray();
        ys = viewModel.Points.Select(point => point.Y).ToArray();
        // f⁻¹ is f mirrored across y = x: the same arrays with X and Y swapped, so an
        // in-place drag of a main point moves its inverse point too. Added first so the
        // draggable main curve is drawn on top; it is never hit-tested, so it can't be dragged.
        inverse = plot.Add.Scatter(ys, xs, InverseColor);
        inverse.MarkerSize = PointSize;
        inverse.Smooth = true;
        UpdateInverse();
        // Pin the main curve to the palette's first color: ScottPlot otherwise picks the
        // next palette color, which changes once the inverse has been added before it.
        scatter = plot.Add.Scatter(xs, ys, plot.Add.Palette.GetColor(0));
        scatter.MarkerSize = PointSize;
        scatter.Smooth = true;
        AddMarkers();
        plot.Axes.AutoScale();
        PlotControl.Refresh();
    }

    // Redraws the moved/clicked point markers without rescaling the axes.
    private void RedrawMarkers()
    {
        UpdateInverse();
        AddMarkers();
        PlotControl.Refresh();
    }

    private void UpdateInverse()
    {
        if (inverse is not null)
        {
            inverse.IsVisible = viewModel?.ShowInverse == true;
        }
    }

    private void AddMarkers()
    {
        if (viewModel is null || scatter is null || xs is null || ys is null)
        {
            return;
        }

        foreach (var marker in markers)
        {
            PlotControl.Plot.Remove(marker);
        }
        markers.Clear();
        var points = viewModel.Points;
        var clickedPoint = viewModel.ClickedPoint;
        for (var i = 0; i < points.Count; i++)
        {
            var moved = points[i].HasChanges();
            var clicked = points[i] == clickedPoint;
            if (!moved && !clicked)
            {
                continue;
            }

            var color = moved ? MovedColor : scatter.Color;
            var size = clicked ? ClickedPointSize : PointSize;
            markers.Add(PlotControl.Plot.Add.Marker(xs[i], ys[i], MarkerShape.FilledCircle, size, color));
        }
    }

    private DataPoint GetNearest(PointerEventArgs e, out Coordinates mouseLocation)
    {
        var pos = e.GetPosition(PlotControl);
        mouseLocation = PlotControl.Plot.GetCoordinates(new Pixel(pos.X, pos.Y));
        return scatter!.Data.GetNearest(mouseLocation, PlotControl.Plot.LastRender);
    }

    private void OnMouseDown(object? sender, PointerEventArgs e)
    {
        if (viewModel is null || scatter is null)
        {
            return;
        }

        var nearest = GetNearest(e, out _);
        if (nearest.IsReal)
        {
            PlotControl.UserInputProcessor.Disable();
            viewModel.BeginDrag(nearest.Index);
        }
    }

    private void OnMouseUp(object? sender, PointerEventArgs e)
    {
        viewModel?.EndDrag();
        PlotControl.UserInputProcessor.Enable();
        PlotControl.Refresh();
    }

    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        if (viewModel is null || scatter is null)
        {
            return;
        }

        var nearest = GetNearest(e, out var mouseLocation);
        PlotControl.Cursor = nearest.IsReal ? CursorHand : CursorArrow;
        if (viewModel.DragIndex is { } index && xs is not null && ys is not null)
        {
            // Move the plotted point in place; the view model updates the grid and markers.
            xs[index] = mouseLocation.X;
            ys[index] = mouseLocation.Y;
            viewModel.DragTo(mouseLocation.X, mouseLocation.Y);
        }
    }
}
