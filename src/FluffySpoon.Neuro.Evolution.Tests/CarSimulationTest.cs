using System;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Sample.Models;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class CarSimulationTest
{
    private static Line CreateLine(float startX, float startY, float endX, float endY) => new()
    {
        Start = new Point(startX, startY),
        End = new Point(endX, endY)
    };

    [TestMethod]
    public void IsCrossing_DetectsSegmentsThatCross()
    {
        Assert.IsTrue(CreateLine(0, 0, 10, 10).IsCrossing(CreateLine(0, 10, 10, 0)));
    }

    [TestMethod]
    public void IsCrossing_DetectsSegmentThatEndsOnOther()
    {
        Assert.IsTrue(CreateLine(0, 0, 5, 5).IsCrossing(CreateLine(0, 10, 10, 0)));
    }

    [TestMethod]
    public void IsCrossing_IgnoresSegmentsThatWouldOnlyCrossIfLonger()
    {
        Assert.IsFalse(CreateLine(0, 0, 4, 4).IsCrossing(CreateLine(0, 10, 10, 0)));
    }

    [TestMethod]
    public void IsCrossing_IgnoresParallelSegments()
    {
        Assert.IsFalse(CreateLine(0, 0, 10, 0).IsCrossing(CreateLine(0, 1, 10, 1)));
    }

    [TestMethod]
    public void CalculateFitness_RewardsGettingThereFaster()
    {
        Assert.IsTrue(CarSimulation.CalculateFitness(100, 100) < CarSimulation.CalculateFitness(100, 1000));
    }

    [TestMethod]
    public void CalculateFitness_RewardsGettingFurtherInTheSameTime()
    {
        Assert.IsTrue(CarSimulation.CalculateFitness(200, 1000) < CarSimulation.CalculateFitness(100, 1000));
    }

    [TestMethod]
    public void CalculateFitness_PrefersFarOverFastWhenTheDifferenceIsLarge()
    {
        Assert.IsTrue(CarSimulation.CalculateFitness(200, 4000) < CarSimulation.CalculateFitness(20, 25));
    }

    [TestMethod]
    public void CalculateFitness_NeverRewardsGoingBackwards()
    {
        Assert.AreEqual(0, CarSimulation.CalculateFitness(0, 100));
        Assert.IsTrue(CarSimulation.CalculateFitness(-5, 5) > CarSimulation.CalculateFitness(0, 100));
    }

    /// <summary>
    /// Accelerating as hard as possible while steering randomly is what gets cars into walls at top speed.
    /// </summary>
    [TestMethod]
    public void Tick_NeverLetsCarsThroughWalls()
    {
        var mapGenerator = new MapGeneratorService(new Random(1337));
        var random = new Random(1337);

        for (var i = 0; i < 20; i++)
        {
            var map = mapGenerator.GenerateRandomMap(25, 21);
            var walls = map.Nodes
                .SelectMany(x => x.WallLines)
                .Select(x => x.Line)
                .ToArray();

            for (var j = 0; j < 50; j++)
            {
                var simulation = new CarSimulation(map);
                var steering = random.NextSingle();

                while (!simulation.HasEnded)
                {
                    var previousCenter = simulation.Car.BoundingBox.Center;

                    if (random.Next(10) == 0)
                        steering = random.NextSingle();

                    simulation.GetInputs();
                    simulation.Tick(new[] { 1f, steering });

                    var movement = new Line()
                    {
                        Start = previousCenter,
                        End = simulation.Car.BoundingBox.Center
                    };

                    Assert.IsTrue(
                        simulation.HasEnded || !walls.Any(movement.IsCrossing),
                        $"A car at speed {simulation.Car.SpeedVelocity} went through a wall with {movement}.");
                }
            }
        }
    }
}
