using System.Collections.Generic;
using System.Threading.Tasks;
using FPlot.Math;

namespace FPlot.Services;

public interface IPointsIoService
{
    Task<(List<Point2d> Points, string Path)?> OpenAsync();

    Task<bool> SaveAsync(List<Point2d> points, string? path);

    Task<string?> SaveAsAsync(List<Point2d> points);

    Task CopyAsync(List<Point2d> points);

    Task<List<Point2d>?> PasteAsync();
}
