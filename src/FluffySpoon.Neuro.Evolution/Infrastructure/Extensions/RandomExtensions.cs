using System;

namespace FluffySpoon.Neuro.Evolution.Infrastructure.Extensions;

internal static class RandomExtensions
{
    public static float NextFloat(this Random random, float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }
}