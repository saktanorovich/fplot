using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FPlot.Math;

[DebuggerDisplay("X={X}, Y={Y}")]
public struct Point2d : IEquatable<Point2d>
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

    public override bool Equals(object? obj)
    {
        if (obj is Point2d other)
        {
            return Equals(other);
        }
        return false;
    }

    public bool Equals(Point2d other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}
