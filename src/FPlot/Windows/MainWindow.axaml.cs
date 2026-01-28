using Avalonia.Controls;
using FPlot.ViewModels;

namespace FPlot.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(this);
    }
}