using System;
using System.Collections.Generic;
using FluffySpoon.Neuro.Evolution.Sample.Models;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    public class MapGeneratorService
    {
        /// <summary>
        /// How many tiles a random map may reach from the origin, which is what keeps it inside the window.
        /// </summary>
        private const int RandomMapRadius = 3;

        private const int MinimumRandomMapTileCount = 24;
        private const int MaximumRandomMapTileCount = 36;

        private readonly Random _random;

        public MapGeneratorService(
            Random random)
        {
            _random = random;
        }

        public Map PickRandomPredefinedMap()
        {
            var maps = new List<Map>();

            //maps.Add(CreateMapBuilder()
            //    .MoveInDirection(Direction.Top)
            //    .MoveInDirection(Direction.Top)
            //    .MoveInDirection(Direction.Top)
            //    .MoveInDirection(Direction.Right)
            //    .MoveInDirection(Direction.Right)
            //    .MoveInDirection(Direction.Right)
            //    .MoveInDirection(Direction.Right)
            //    .MoveInDirection(Direction.Bottom)
            //    .MoveInDirection(Direction.Bottom)
            //    .MoveInDirection(Direction.Bottom)
            //    .MoveInDirection(Direction.Bottom)
            //    .MoveInDirection(Direction.Left)
            //    .MoveInDirection(Direction.Left)
            //    .MoveInDirection(Direction.Left)
            //    .MoveInDirection(Direction.Left)
            //    .MoveInDirection(Direction.Top)
            //    .Build());

            maps.Add(CreateMapBuilder()
                .MoveInDirection(Direction.Top)
                .MoveInDirection(Direction.Top)
                .MoveInDirection(Direction.Top)
                .MoveInDirection(Direction.Right)
                //.MoveInDirection(Direction.Right)
                //.MoveInDirection(Direction.Right)
                //.MoveInDirection(Direction.Top)
                //.MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Top)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Top)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Top)
                .MoveInDirection(Direction.Left)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Bottom)
                .MoveInDirection(Direction.Right)
                .MoveInDirection(Direction.Top)
                .Build());

            var index = _random.Next(0, maps.Count);
            return maps[index];
        }

        private static MapBuilder CreateMapBuilder()
        {
            return new MapBuilder();
        }

        /// <summary>
        /// Grows a small rectangular loop by repeatedly pushing one of its edges outwards into free tiles.
        /// Every step keeps the track a closed loop that never touches itself, so no retrying is needed.
        /// The edges around the origin are left alone, since cars start there driving towards the top.
        /// </summary>
        public Map GenerateRandomMap()
        {
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

            var targetTileCount = _random.Next(MinimumRandomMapTileCount, MaximumRandomMapTileCount + 1);
            while (tiles.Count < targetTileCount && TryGrowLoop(tiles))
            {
            }

            var mapBuilder = CreateMapBuilder();
            for (var i = 0; i < tiles.Count; i++)
                mapBuilder.MoveInDirection(GetDirectionBetween(tiles[i], tiles[(i + 1) % tiles.Count]));

            return mapBuilder.Build();
        }

        private bool TryGrowLoop(List<(int X, int Y)> tiles)
        {
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

                    if (IsFreeTile(tiles, first) && IsFreeTile(tiles, second))
                        candidates.Add((i + 1, first, second));
                }
            }

            if (candidates.Count == 0)
                return false;

            var candidate = candidates[_random.Next(candidates.Count)];
            tiles.InsertRange(candidate.InsertionIndex, new[] { candidate.First, candidate.Second });

            return true;
        }

        private static bool IsFreeTile(List<(int X, int Y)> tiles, (int X, int Y) tile)
        {
            return
                Math.Abs(tile.X) <= RandomMapRadius &&
                Math.Abs(tile.Y) <= RandomMapRadius &&
                !tiles.Contains(tile);
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
