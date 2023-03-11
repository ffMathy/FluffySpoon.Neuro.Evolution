using FluffySpoon.Neuro.Evolution.Sample.Models;

namespace FluffySpoon.Neuro.Evolution.Sample.Helpers
{
    public sealed class DistanceHelper
    {
        public static Point FindClosestPointOnLine(
            Point origin,
            Line line)
        {
            var p1 = line.Start;
            var p2 = line.End;

            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
                return p1;

            var t = ((origin.X - p1.X) * dx +
                (origin.Y - p1.Y) * dy) /
                (dx * dx + dy * dy);

            if (t < 0)
            {
                return new Point(p1.X, p1.Y);
            }
            else if (t > 1)
            {
                return new Point(p2.X, p2.Y);
            }

            return new Point(p1.X + t * dx, p1.Y + t * dy);
        }
    }
}
