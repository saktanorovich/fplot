using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FPlot.Model;

namespace FPlot.ViewModels;

public class GridViewModel : ObservableObject
{
    private MainViewModel? owner;

    public ObservableCollection<PointViewModel>? Points => owner?.Points;

    public void Initialize(MainViewModel owner)
    {
        this.owner = owner;
        Update();
    }

    public void Update()
    {
        if (owner is null)
            return;
        foreach (var point in owner.Points)
        {
            Update(point);
        }
        OnPropertyChanged(nameof(Points));
    }

    public void Update(PointViewModel pointViewModel)
    {
        pointViewModel.PropertyChanged -= PointViewModelOnPropertyChanged;
        pointViewModel.PropertyChanged += PointViewModelOnPropertyChanged;
    }

    public void Update(Point2d point, int index)
    {
        if (owner is null)
            return;
        var pointViewModel = owner.Points[index];
        pointViewModel.PropertyChanged -= PointViewModelOnPropertyChanged;
        pointViewModel.X = point.X;
        pointViewModel.Y = point.Y;
        pointViewModel.PropertyChanged += PointViewModelOnPropertyChanged;
    }

    private void PointViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        owner?.NotifyPlot();
    }
}