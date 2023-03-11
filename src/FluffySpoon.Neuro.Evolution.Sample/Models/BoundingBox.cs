using System.Linq;

namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public class BoundingBox
    {
        public Size Size { get; set; }

        public Point Center => new Point(
            Location.X + (Size.Width / 2), 
            Location.Y + (Size.Height / 2));

        public Point Location { get; set; }

        public bool IsWithin(BoundingBox other)
        {
            return 
                Center.X >= other.Location.X &&
                Center.Y >= other.Location.Y &&
                Center.X <= other.Location.X + other.Size.Width &&
                Center.Y <= other.Location.Y + other.Size.Height;
        }

        public bool IsWithin(BoundingBox[] others)
        {
            return others.Any(IsWithin);
        }

        public BoundingBox()
        {
            Location = new Point();
            Size = new Size();
        }

        public override string ToString()
        {
            return Location.ToString() + " " + Size.ToString();
        }
    }
}
