using System;
using System.Collections.Generic;
using FluffySpoon.Neuro.Evolution.Sample.Models;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    public class MapGeneratorService
    {
        /// <summary>
        /// How much of the available tiles a random map tries to use. Higher leaves the track little room to wind.
        /// </summary>
        private const double MinimumRandomMapCoverage = 0.5;
        private const double MaximumRandomMapCoverage = 0.7;

        private readonly Random _random;

        public MapGeneratorService(
            Random random)
        {
            _random = random;
        }

        private static MapBuilder CreateMapBuilder()
        {
            return new MapBuilder();
        }

        /// <summary>
        /// Grows a small rectangular loop by repeatedly pushing one of its edges outwards into free tiles.
        /// Every step keeps the track a closed loop that never touches itself, so no retrying is needed.
        /// The edges around the origin are left alone, since cars start there driving towards the top.
        /// The map is centered around the origin and never gets wider or taller than the given amount of tiles.
        /// </summary>
        public Map GenerateRandomMap(int maximumColumns, int maximumRows)
        {
            var radiusX = Math.Max(1, (maximumColumns - 1) / 2);
            var radiusY = Math.Max(1, (maximumRows - 1) / 2);

            var side = _random.Next(2) == 0 ? -1 : 1;
            var tiles = new List<(int X, int Y)>
            {
                (0, 0),
                (0, -1),
                (side, -1),
                (side, 0),
                (side, 1),
                (0, 1)
            };
            var occupiedTiles = new HashSet<(int X, int Y)>(tiles);

            var availableTileCount = ((radiusX * 2) + 1) * ((radiusY * 2) + 1);
            var coverage = MinimumRandomMapCoverage + (_random.NextDouble() * (MaximumRandomMapCoverage - MinimumRandomMapCoverage));
            var targetTileCount = (int)(availableTileCount * coverage);
            while (tiles.Count < targetTileCount && TryGrowLoop(tiles, occupiedTiles, radiusX, radiusY))
            {
            }

            var mapBuilder = CreateMapBuilder();
            for (var i = 0; i < tiles.Count; i++)
                mapBuilder.MoveInDirection(GetDirectionBetween(tiles[i], tiles[(i + 1) % tiles.Count]));

            return mapBuilder.Build();
        }

        private bool TryGrowLoop(
            List<(int X, int Y)> tiles,
            HashSet<(int X, int Y)> occupiedTiles,
            int radiusX,
            int radiusY)
        {
            bool IsFreeTile((int X, int Y) tile) =>
                Math.Abs(tile.X) <= radiusX &&
                Math.Abs(tile.Y) <= radiusY &&
                !occupiedTiles.Contains(tile);

            var candidates = new List<(int InsertionIndex, (int X, int Y) First, (int X, int Y) Second)>();
            for (var i = 1; i < tiles.Count - 1; i++)
            {
                var from = tiles[i];
                var to = tiles[i + 1];

                // Pushing an edge outwards moves it sideways, which is the edge's direction rotated by 90 degrees.
                var sidewaysX = to.Y - from.Y;
                var sidewaysY = to.X - from.X;

                foreach (var sign in new[] { -1, 1 })
                {
                    var first = (from.X + sidewaysX * sign, from.Y + sidewaysY * sign);
                    var second = (to.X + sidewaysX * sign, to.Y + sidewaysY * sign);

                    if (IsFreeTile(first) && IsFreeTile(second))
                        candidates.Add((i + 1, first, second));
                }
            }

            if (candidates.Count == 0)
                return false;

            var candidate = candidates[_random.Next(candidates.Count)];
            tiles.InsertRange(candidate.InsertionIndex, new[] { candidate.First, candidate.Second });
            occupiedTiles.Add(candidate.First);
            occupiedTiles.Add(candidate.Second);

            return true;
        }

        private static Direction GetDirectionBetween((int X, int Y) from, (int X, int Y) to)
        {
            return (to.X - from.X, to.Y - from.Y) switch
            {
                (0, -1) => Direction.Top,
                (0, 1) => Direction.Bottom,
                (-1, 0) => Direction.Left,
                (1, 0) => Direction.Right,

                _ => throw new InvalidOperationException("Tiles must be next to each other.")
            };
        }
    }
}
