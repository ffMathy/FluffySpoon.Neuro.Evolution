using System;
using FluffySpoon.Neuro.Evolution.Sample.Helpers;

namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public struct LineFormula
    {
        public float A { get; set; }
        public float B { get; set; }
        public float C { get; set; }
    }

    public struct Line
    {
        private Point _start;
        private Point _end;

        public Point Start
        {
            get => _start;
            set
            {
                _start = value;
                RecalculateValues();
            }
        }

        public Point End
        {
            get => _end;
            set
            {
                _end = value;
                RecalculateValues();
            }
        }

        public Point Center { get; private set; }

        public LineFormula Formula { get; private set; }

        private void RecalculateValues()
        {
            RecalculateFormula();
            RecalculateCenter();
        }

        private void RecalculateCenter()
        {
            Center = new Point(
               Start.X + (End.X - Start.X) / 2,
               Start.Y + (End.Y - Start.Y) / 2);
        }

        private void RecalculateFormula()
        {
            var a = End.Y - Start.Y;
            var b = Start.X - End.X;

            Formula = new LineFormula()
            {
                A = a,
                B = b,
                C = (a * Start.X) + (b * Start.Y)
            };
        }

        public Line RotateAround(Point origin, float angleInDegrees)
        {
            return new Line()
            {
                Start = Start.RotateAround(
                    origin,
                    angleInDegrees),
                End = End.RotateAround(
                    origin,
                    angleInDegrees)
            };
        }

        public Line Rotate(float angleInDegrees)
        {
            return RotateAround(Center, angleInDegrees);
        }

        public double GetAngleTo(Line other)
        {
            var theta1 = Math.Atan2(
                Start.Y - (double)End.Y,
                Start.X - (double)End.X);

            var theta2 = Math.Atan2(
                other.Start.Y - (double)other.End.Y,
                other.Start.X - (double)other.End.X);

            var difference = Math.Abs(theta1 - theta2);

            var angleRadians = Math.Min(difference, Math.Abs(180 - difference));
            var angleDegrees = MathHelper.RadiansToDegrees(angleRadians);

            return angleDegrees;
        }

        public Point? GetIntersectionPointWith(Line other)
        {
            var delta = (Formula.A * other.Formula.B) - (other.Formula.A * Formula.B);
            if (delta == 0)
                return null;

            try
            {
                var point = new Point()
                {
                    X = ((other.Formula.B * Formula.C) - (Formula.B * other.Formula.C)) / delta,
                    Y = ((Formula.A * other.Formula.C) - (other.Formula.A * Formula.C)) / delta
                };

                return point;
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether the two line segments touch or cross each other, unlike <see cref="GetIntersectionPointWith"/> which treats lines as endless.
        /// </summary>
        public bool IsCrossing(Line other)
        {
            var d1 = GetSide(other, Start);
            var d2 = GetSide(other, End);
            var d3 = GetSide(this, other.Start);
            var d4 = GetSide(this, other.End);

            if (d1 * d2 < 0 && d3 * d4 < 0)
                return true;

            return
                (d1 == 0 && IsWithinBoundsOf(other, Start)) ||
                (d2 == 0 && IsWithinBoundsOf(other, End)) ||
                (d3 == 0 && IsWithinBoundsOf(this, other.Start)) ||
                (d4 == 0 && IsWithinBoundsOf(this, other.End));
        }

        private static float GetSide(Line line, Point point)
        {
            return ((line.End.X - line.Start.X) * (point.Y - line.Start.Y)) -
                   ((line.End.Y - line.Start.Y) * (point.X - line.Start.X));
        }

        private static bool IsWithinBoundsOf(Line line, Point point)
        {
            return
                point.X >= Math.Min(line.Start.X, line.End.X) &&
                point.X <= Math.Max(line.Start.X, line.End.X) &&
                point.Y >= Math.Min(line.Start.Y, line.End.Y) &&
                point.Y <= Math.Max(line.Start.Y, line.End.Y);
        }

        public static Line operator *(Line a, int b)
        {
            return new Line()
            {
                Start = a.Start * b,
                End = a.End * b
            };
        }

        public static Line operator /(Line a, int b)
        {
            return new Line()
            {
                Start = a.Start / b,
                End = a.End / b
            };
        }

        public override string ToString()
        {
            return "<" + Start + ":" + End + ">";
        }
    }
}
