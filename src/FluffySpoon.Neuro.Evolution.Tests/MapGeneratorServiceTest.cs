using System;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Sample.Helpers;
using FluffySpoon.Neuro.Evolution.Sample.Models;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class MapGeneratorServiceTest
{
    private const int MapsToGenerate = 100;
    private const int MaximumColumns = 25;
    private const int MaximumRows = 21;

    [TestMethod]
    public void GenerateRandomMap_CreatesClosedLoopsThatNeverTouchThemselves()
    {
        var service = new MapGeneratorService(new Random(1337));

        for (var i = 0; i < MapsToGenerate; i++)
        {
            var nodes = service.GenerateRandomMap(MaximumColumns, MaximumRows).Nodes;

            Assert.IsTrue(nodes.Length >= 200, $"Unexpected node count {nodes.Length}.");

            foreach (var node in nodes)
            {
                Assert.AreEqual(
                    DirectionHelper.GetOppositeDirection(node.ExitDirection),
                    node.Next.EntranceDirection);
                Assert.AreSame(node, node.Next.Previous);
                Assert.AreEqual(3, node.ProgressLines.Length);
                Assert.AreEqual(2, node.WallLines.Length);
            }

            var locations = nodes.Select(x => x.BoundingBox.Location).ToArray();
            Assert.AreEqual(nodes.Length, locations.Distinct().Count());

            Assert.IsTrue(nodes.All(x =>
                Math.Abs(x.BoundingBox.Center.X) <= ((MaximumColumns - 1) / 2) * (float)Map.TileSize &&
                Math.Abs(x.BoundingBox.Center.Y) <= ((MaximumRows - 1) / 2) * (float)Map.TileSize));
        }
    }

    [TestMethod]
    public void GenerateRandomMap_StartsWhereCarsStartDriving()
    {
        var service = new MapGeneratorService(new Random(1337));

        for (var i = 0; i < MapsToGenerate; i++)
        {
            var firstNode = service.GenerateRandomMap(MaximumColumns, MaximumRows).Nodes.First();

            Assert.AreEqual(Direction.Bottom, firstNode.EntranceDirection);
            Assert.AreEqual(Direction.Top, firstNode.ExitDirection);
            Assert.AreEqual(0f, firstNode.BoundingBox.Center.X);
            Assert.AreEqual(0f, firstNode.BoundingBox.Center.Y);
        }
    }

    [TestMethod]
    public void GenerateRandomMap_CreatesDifferentMaps()
    {
        var service = new MapGeneratorService(new Random(1337));

        var layouts = Enumerable
            .Range(0, 100)
            .Select(_ => string.Join(",", service.GenerateRandomMap(MaximumColumns, MaximumRows).Nodes.Select(x => x.ExitDirection)))
            .Distinct()
            .Count();

        Assert.IsTrue(layouts > 90, $"Only {layouts} different layouts were generated.");
    }

    [TestMethod]
    public void GenerateRandomMap_CanBeDrivenOn()
    {
        var service = new MapGeneratorService(new Random(1337));
        var random = new Random(1337);

        for (var i = 0; i < 50; i++)
        {
            var simulation = new CarSimulation(service.GenerateRandomMap(MaximumColumns, MaximumRows));

            while (!simulation.HasEnded)
            {
                simulation.GetInputs();
                simulation.Tick(new[] { random.NextSingle(), random.NextSingle() });
            }
        }
    }
}
