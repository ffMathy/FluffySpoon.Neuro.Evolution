namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public class Size
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public override string ToString()
        {
            return "(" + Width + "|" + Height + ")";
        }
    }
}
