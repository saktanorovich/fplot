using Avalonia.Controls;
using Avalonia.Interactivity;
using FPlot.ViewModels;

namespace FPlot.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.Attach(GridView.GridControl);
            mainViewModel.Attach(PlotView.PlotControl);
        }
    }
}