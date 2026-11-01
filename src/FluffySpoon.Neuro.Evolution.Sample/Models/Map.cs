using System;
using System.Collections.Generic;
using System.Linq;

namespace FluffySpoon.Neuro.Evolution.Sample.Models
{
    public class Map
    {
        public const double TileSize = 130;
        public const double HalfTileSize = TileSize / 2;

        /// <summary>
        /// How far sensors can see. Anything further away reads the same as seeing nothing.
        /// </summary>
        public const int SensorRangeInTiles = 3;

        private readonly Dictionary<(int X, int Y), Line[]> _wallLinesInSensorRangeByTile;

        public MapNode[] Nodes { get; }

        public ProgressLine FirstProgressLine { get; }

        public int ProgressLineCount { get; }

        public Map(MapNode[] nodes)
        {
            Nodes = nodes;

            var progressLines = nodes
                .SelectMany(x => x.ProgressLines)
                .OrderBy(x => x.Offset)
                .ToArray();
            FirstProgressLine = progressLines.First();
            ProgressLineCount = progressLines.Last().Offset;

            _wallLinesInSensorRangeByTile = GetWallLinesInSensorRangeByTile(nodes);
        }

        /// <summary>
        /// Looking only at nearby walls is what keeps the cost of a sensor from growing with the size of the map.
        /// </summary>
        public Line[] GetWallLinesInSensorRange(Point point)
        {
            return _wallLinesInSensorRangeByTile.TryGetValue(GetTile(point), out var lines)
                ? lines
                : Array.Empty<Line>();
        }

        private static Dictionary<(int X, int Y), Line[]> GetWallLinesInSensorRangeByTile(MapNode[] nodes)
        {
            var linesByTile = new Dictionary<(int X, int Y), List<Line>>();
            foreach (var node in nodes)
            {
                var tile = GetTile(node.BoundingBox.Center);
                for (var x = tile.X - SensorRangeInTiles; x <= tile.X + SensorRangeInTiles; x++)
                {
                    for (var y = tile.Y - SensorRangeInTiles; y <= tile.Y + SensorRangeInTiles; y++)
                    {
                        if (!linesByTile.TryGetValue((x, y), out var lines))
                            linesByTile[(x, y)] = lines = new List<Line>();

                        lines.AddRange(node.WallLines.Select(wallLine => wallLine.Line));
                    }
                }
            }

            return linesByTile.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }

        private static (int X, int Y) GetTile(Point point)
        {
            return (
                (int)Math.Round(point.X / TileSize),
                (int)Math.Round(point.Y / TileSize));
        }
    }
}
