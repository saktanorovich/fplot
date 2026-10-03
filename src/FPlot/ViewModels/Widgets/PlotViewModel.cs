using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FPlot.Math;

namespace FPlot.ViewModels;

// Plot state and user intents; PlotView does the rendering and hit-testing.
public class PlotViewModel : ObservableObject
{
    private MainViewModel? owner;
    private int? dragIndex;

    // The points changed: the view redraws everything and rescales the axes.
    public event Action? DataChanged;
    // Only the moved/clicked point markers (or inverse visibility) changed.
    public event Action? MarkersChanged;

    public IReadOnlyList<PointViewModel> Points => owner?.Points ?? (IReadOnlyList<PointViewModel>)[];
    public bool ShowInverse => owner?.ShowInverse == true;
    public PointViewModel? ClickedPoint => owner?.ClickedPoint;
    // Index of the point being dragged, if any.
    public int? DragIndex => dragIndex;

    public void Initialize(MainViewModel owner)
    {
        this.owner = owner;
        Update();
    }

    public void Update()
    {
        DataChanged?.Invoke();
    }

    public void UpdateMarkers()
    {
        MarkersChanged?.Invoke();
    }

    public void BeginDrag(int index)
    {
        dragIndex = index;
        owner?.NotifyClick(index);
        UpdateMarkers();
    }

    public void DragTo(double x, double y)
    {
        if (dragIndex is not { } index)
        {
            return;
        }

        owner?.NotifyGrid(new Point2d(x, y), index);
        UpdateMarkers();
    }

    public void EndDrag()
    {
        dragIndex = null;
    }
}
