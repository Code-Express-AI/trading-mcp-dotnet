namespace TradingDataGenerator.Generation;

internal static class RandomExtensions
{
    public static T Pick<T>(this Random random, IReadOnlyList<T> items) => items[random.Next(items.Count)];

    // Box-Muller transform: turns two uniform random numbers into one from a normal distribution
    // (mean 0, standard deviation 1). 1 - NextDouble() avoids taking the log of zero.
    public static double NextStandardNormal(this Random random)
    {
        var uniform1 = 1.0 - random.NextDouble();
        var uniform2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(uniform1)) * Math.Cos(2.0 * Math.PI * uniform2);
    }
}
