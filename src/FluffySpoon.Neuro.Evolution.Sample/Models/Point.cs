using System;

namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public struct Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Point(float factor, float x, float y) : this(x * factor, y * factor)
        {
        }

        public Point RotateAround(Point centerPoint, float angleInDegrees)
        {
            if(angleInDegrees == 0)
                return new Point(X, Y);

            var pointToRotate = this;

            var angleInRadians = angleInDegrees * (Math.PI / 180);
            var cosTheta = (float)Math.Cos(angleInRadians);
            var sinTheta = (float)Math.Sin(angleInRadians);

            return new Point
            {
                X = (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        public float GetDistanceTo(Point other)
        {
            var a = Math.Abs(other.X - X);
            var b = Math.Abs(other.Y - Y);

            return (float)Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }

        public override string ToString()
        {
            return "(" + X + "|" + Y + ")";
        }

        public static Point operator +(Point a, int b)
        {
            return new Point(a.X + b, a.Y + b);
        }

        public static Point operator -(Point a, int b)
        {
            return new Point(a.X - b, a.Y - b);
        }

        public static Point operator *(Point a, int b)
        {
            return new Point(a.X * b, a.Y * b);
        }

        public static Point operator /(Point a, int b)
        {
            return new Point(a.X / b, a.Y / b);
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }
    }
}
