using System;

namespace FluffySpoon.Neuro.Evolution.Infrastructure.Extensions;

internal static class RandomExtensions
{
    public static float NextFloat(this Random random, float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }

    /// <summary>
    /// Returns a normally distributed value with mean 0 and standard deviation 1 (Box-Muller transform).
    /// </summary>
    public static float NextGaussian(this Random random)
    {
        var u1 = 1d - random.NextDouble();
        var u2 = random.NextDouble();
        return (float)(Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2));
    }
}