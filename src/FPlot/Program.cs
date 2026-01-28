using Avalonia;
using System;

namespace FPlot;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // In production, log or show error
            Console.WriteLine(ex.Message + ex.StackTrace);
        }
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions
            {
                DisableDefaultApplicationMenuItems = false
            })
            .WithInterFont()
            .LogToTrace();
}
