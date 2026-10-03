using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FPlot.Math;

namespace FPlot.ViewModels;

public class PointViewModel : ObservableObject
{
    // Fixed-point format the grid displays (and parses back) values in.
    public const string DisplayFormat = "F6";

    private Point2d mainPoint;
    private Point2d workPoint;
    // A point added in the app is unsaved even though it has not been edited yet.
    private bool added;

    public PointViewModel(Point2d point, bool added = false)
    {
        mainPoint = point;
        workPoint = point.Copy();
        this.added = added;
    }

    public double X
    {
        get => workPoint.X;
        set
        {
            if (MathUtils.Sign(workPoint.X - value) != 0 && !IsDisplayedValue(workPoint.X, value))
            {
                workPoint.X = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(XChanged));
            }
        }
    }

    public double Y
    {
        get => workPoint.Y;
        set
        {
            if (MathUtils.Sign(workPoint.Y - value) != 0 && !IsDisplayedValue(workPoint.Y, value))
            {
                workPoint.Y = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(YChanged));
            }
        }
    }

    public bool XChanged => MathUtils.Sign(workPoint.X - mainPoint.X) != 0;
    public bool YChanged => MathUtils.Sign(workPoint.Y - mainPoint.Y) != 0;

    public bool HasChanges()
    {
        return added || XChanged || YChanged;
    }

    public void Invalidate()
    {
        mainPoint = workPoint;
        added = false;
        OnPropertyChanged(nameof(XChanged));
        OnPropertyChanged(nameof(YChanged));
    }

    // The grid shows values rounded to DisplayFormat and may write that
    // rounded text back; treat it as no edit so loaded values keep their full precision.
    private static bool IsDisplayedValue(double current, double value)
    {
        return current.ToString(DisplayFormat, CultureInfo.InvariantCulture) ==
               value.ToString(DisplayFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats the point as a CSV line "x,y" in the invariant culture.
    /// </summary>
    public static string ToCsv(Point2d point)
    {
        return $"{point.X.ToString(CultureInfo.InvariantCulture)}," +
               $"{point.Y.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Formats the point as a tab-separated row in the given culture, as spreadsheets
    /// (Excel, Numbers) put cells on the clipboard.
    /// </summary>
    public static string ToTabular(Point2d point, CultureInfo culture)
    {
        return $"{point.X.ToString(culture)}\t{point.Y.ToString(culture)}";
    }

    /// <summary>
    /// Parses a CSV line "x,y" (invariant culture) or a tab-separated spreadsheet row
    /// "x\ty" (current culture, falling back to invariant).
    /// </summary>
    public static bool TryParse(string? s, out Point2d point)
    {
        point = new Point2d();
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Contains('\t'))
        {
            var cells = s.Split('\t');
            if (cells.Length < 2 || !TryParseCell(cells[0], out var x) || !TryParseCell(cells[1], out var y))
            {
                return false;
            }

            point = new Point2d(x, y);
            return true;
        }
        var data = s.Split(',');
        if (data.Length < 2 ||
            !double.TryParse(data[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var px) ||
            !double.TryParse(data[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var py))
        {
            return false;
        }

        point = new Point2d(px, py);
        return true;
    }

    private static bool TryParseCell(string cell, out double value)
    {
        cell = cell.Trim();
        return double.TryParse(cell, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
               double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
