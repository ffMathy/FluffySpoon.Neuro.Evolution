namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public class ProgressLine
    {
        public Line Line { get; set; }
        public int Offset { get; set; }
        public MapNode MapNode { get; set; }
    }

    public class WallLine
    {
        public Line Line { get; set; }
        public Direction Direction { get; set; }
        public MapNode MapNode { get; set; }
    }

    public class MapNode
    {
        public Direction EntranceDirection { get; set; }
        public Direction ExitDirection { get; set; }

        public BoundingBox BoundingBox { get; set; }

        public ProgressLine[] ProgressLines { get; set; }

        public WallLine[] WallLines { get; set; }
        public WallLine[] OpeningLines { get; set; }

        public MapNode Next { get; set; }

        public MapNode Previous { get; set; }

        public int Offset { get; set; }
    }
}
