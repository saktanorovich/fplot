using System;
using System.Diagnostics;
using System.Globalization;

namespace FPlot.Model;

[DebuggerDisplay("{X={X}, Y={Y}")]
public struct Point2d
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public Point2d(double x, double y)
    {
        X = x;
        Y = y;
    }

    public Point2d Copy()
    {
        return new Point2d(X, Y);
    }

    public override string ToString()
    {
        return $"{X.ToString(CultureInfo.InvariantCulture)}," +
               $"{Y.ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool TryParse(string? s, out Point2d point)
    {
        point = new Point2d();
        if (string.IsNullOrWhiteSpace(s))
            return false;
        var data = s.Split(',');
        try
        {
            point.X = double.Parse(data[0], CultureInfo.InvariantCulture);
            point.Y = double.Parse(data[1], CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}