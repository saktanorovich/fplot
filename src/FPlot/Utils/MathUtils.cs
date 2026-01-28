namespace FPlot.Utils;

public static class MathUtils
{
    public static readonly double EPS = 1e-9;

    public static int Sign(double x)
    {
        if (x + EPS < 0) return -1;
        if (x - EPS > 0) return +1;
        return 0;
    }
}