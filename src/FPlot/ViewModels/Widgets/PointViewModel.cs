using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FPlot.Model;
using FPlot.Utils;

namespace FPlot.ViewModels;

public class PointViewModel : ObservableObject
{
    private Point2d mainPoint;
    private Point2d workPoint;

    public PointViewModel(Point2d point)
    {
        mainPoint = point;
        workPoint = point.Copy();
    }

    public double X
    {
        get => workPoint.X;
        set
        {
            if (MathUtils.Sign(workPoint.X - value) != 0)
            {
                workPoint.X = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(XColor));
            }
        }
    }

    public double Y
    {
        get => workPoint.Y;
        set
        {
            if (MathUtils.Sign(workPoint.Y - value) != 0)
            {
                workPoint.Y = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(YColor));
            }
        }
    }

    public IImmutableSolidColorBrush XColor => MakeColor(workPoint.X, mainPoint.X);
    public IImmutableSolidColorBrush YColor => MakeColor(workPoint.Y, mainPoint.Y);

    public bool HasChanges()
    {
        return MathUtils.Sign(mainPoint.X - workPoint.X) != 0 ||
               MathUtils.Sign(mainPoint.Y - workPoint.Y) != 0;
    }

    public void Invalidate()
    {
        var tempP = workPoint;
        mainPoint = workPoint;
        workPoint = tempP.Copy();
    }

    private static IImmutableSolidColorBrush MakeColor(double a, double b)
    {
        if (MathUtils.Sign(a - b) == 0)
            return Brushes.Black;
        else
            return Brushes.LightCoral;
    }
}