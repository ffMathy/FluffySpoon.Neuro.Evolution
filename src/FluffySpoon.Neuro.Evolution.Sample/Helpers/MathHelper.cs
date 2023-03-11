using System;

namespace FluffySpoon.Neuro.Evolution.Sample.Helpers
{
    public class MathHelper
    {
        private readonly Random _random;

        public MathHelper(
            Random random)
        {
            _random = random;
        }

        public static bool IsEqualWithinRange(float a, float b, double delta)
        {
            return Math.Abs(a - (double)b) <= delta;
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }
    }
}
