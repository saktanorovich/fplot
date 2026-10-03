using System;
using System.Collections.ObjectModel;
using FPlot.Math;

namespace FPlot.ViewModels;

internal static class MainViewModelInit
{
    public static readonly string DefaultPath = "Default: F(x) = e^x / 100";

    public static ObservableCollection<PointViewModel> CreateInitPoints()
    {
        var points = new ObservableCollection<PointViewModel>();
        for (var x = 0; x < 10; ++x)
        {
            var point = new Point2d(x, System.Math.Round(System.Math.Pow(System.Math.E, x) / 100, 2));
            var pointViewModel = new PointViewModel(point);
            points.Add(pointViewModel);
        }
        return points;
    }
}