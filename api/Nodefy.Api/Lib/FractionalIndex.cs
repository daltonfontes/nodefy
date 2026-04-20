namespace Nodefy.Api.Lib;

public static class FractionalIndex
{
    public static double Between(double? prev, double? next)
    {
        double lo = prev ?? 0.0;
        double hi = next ?? 1_000_000.0;
        return (lo + hi) / 2.0;
    }

    public static double After(double last) => last + 1_000_000.0;

    public static double Before(double first) =>
        first > 1e-9 ? first / 2.0 : first - 1_000_000.0;

    public static bool NeedsRebalance(IEnumerable<double> positions) =>
        positions.Any(p => p < 0 || p < 1e-9 || p > 1e15);

    public static double[] Rebalance(int count) =>
        Enumerable.Range(1, count).Select(i => (double)i * 1_000_000.0).ToArray();
}
