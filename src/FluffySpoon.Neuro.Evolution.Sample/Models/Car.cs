using System;

namespace FluffySpoon.Neuro.Evolution.Sample.Models
{

    public class Car
    {
        public const int Size = 30;

        public BoundingBox BoundingBox { get; }

        public float SpeedVelocity { get; private set; }

        public float TurnAngle { get; private set; }
        public float TurnAngleVelocity { get; private set; }

        public Line ForwardDirectionLine
        {
            get
            {
                var line = new Line()
                {
                    Start = new Point(0, 0),
                    End = new Point(0, -0.5f)
                };

                return line.RotateAround(new Point(), TurnAngle);
            }
        }

        public Car()
        {
            const int size = Size;

            SpeedVelocity = 5;

            BoundingBox = new BoundingBox()
            {
                Size = new Size()
                {
                    Width = size,
                    Height = size
                },
                Location = new Point()
                {
                    X = -size / 2,
                    Y = -size / 2
                }
            };
        }

        public float Turn(float deltaAngle)
        {
            var previousAngleVelocity = TurnAngleVelocity;
            TurnAngleVelocity += deltaAngle;

            const int threshold = 5;

            if (TurnAngleVelocity < -threshold) { 
                TurnAngleVelocity = -threshold;
            } else if (TurnAngleVelocity > threshold) { 
                TurnAngleVelocity = threshold;
            }

            var delta = TurnAngleVelocity - previousAngleVelocity;
            Accelerate(-Math.Abs(delta) / 2000f);

            return delta;
        }

        public float Accelerate(float deltaVelocity)
        {
            var previousSpeedVelocity = SpeedVelocity;
            SpeedVelocity += deltaVelocity / 2;

            EnsureSpeedWithinBounds();

            return SpeedVelocity - previousSpeedVelocity;
        }

        private void EnsureSpeedWithinBounds()
        {
            const float highThreshold = 15;
            const float lowThreshold = 4;

            if (SpeedVelocity < lowThreshold)
                SpeedVelocity = lowThreshold;

            if (SpeedVelocity > highThreshold)
                SpeedVelocity = highThreshold;
        }

        public void Tick()
        {
            TurnAngle += TurnAngleVelocity;

            BoundingBox.Location = new Point(
                BoundingBox.Location.X + (ForwardDirectionLine.End.X * 2 * SpeedVelocity),
                BoundingBox.Location.Y + (ForwardDirectionLine.End.Y * 2 * SpeedVelocity));
        }
    }
}
