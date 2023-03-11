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
