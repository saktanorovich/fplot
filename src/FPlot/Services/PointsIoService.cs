using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FPlot.Math;
using FPlot.Utils;
using FPlot.ViewModels;

namespace FPlot.Services;

public class PointsIoService : IPointsIoService
{
    private readonly Window owner;

    public PointsIoService(Window owner)
    {
        this.owner = owner;
    }

    public async Task<(List<Point2d> Points, string Path)?> OpenAsync()
    {
        var file = await owner.OpenFileAsync();
        if (file is null)
        {
            return null;
        }

        var lines = await owner.ReadLinesAsync(file);
        return (Parse(lines), file.TryGetLocalPath() ?? file.Path.LocalPath);
    }

    public Task<bool> SaveAsync(List<Point2d> points, string? path)
    {
        return owner.SaveFileAsync(points.Select(PointViewModel.ToCsv), path);
    }

    public Task<string?> SaveAsAsync(List<Point2d> points)
    {
        return owner.SaveFileAsync(points.Select(PointViewModel.ToCsv));
    }

    public Task CopyAsync(List<Point2d> points)
    {
        return owner.SetClipboardAsync(points.Select(
            point => PointViewModel.ToTabular(point, CultureInfo.CurrentCulture)));
    }

    public async Task<List<Point2d>?> PasteAsync()
    {
        var clipboard = await owner.GetClipboardAsync();
        if (clipboard is null)
        {
            return null;
        }

        return Parse(await owner.ReadLinesAsync(clipboard));
    }

    private static List<Point2d> Parse(IEnumerable<string> lines)
    {
        var result = new List<Point2d>();
        foreach (var line in lines)
        {
            if (PointViewModel.TryParse(line, out var point))
            {
                result.Add(point);
            }
        }
        return result;
    }
}
