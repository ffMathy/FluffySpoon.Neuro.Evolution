namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public class Map
    {
        public const double TileSize = 130;
        public const double HalfTileSize = TileSize / 2;

        public MapNode[] Nodes { get; set; }
    }
}
