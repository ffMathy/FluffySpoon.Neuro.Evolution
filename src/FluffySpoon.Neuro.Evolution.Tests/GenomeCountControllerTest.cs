using System;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class GenomeCountControllerTest
{
    /// <summary>
    /// A frame rate that is capped at 60, and falls in proportion to the genome count once drawing can't keep up.
    /// </summary>
    private static double GetFramesPerSecond(int genomeCount, int genomeCountAtFiftyFramesPerSecond)
    {
        return Math.Min(60, 50d * genomeCountAtFiftyFramesPerSecond / genomeCount);
    }

    private static int RunBatches(GenomeCountController controller, int genomeCount, int genomeCountAtFiftyFramesPerSecond, int batches)
    {
        for (var i = 0; i < batches; i++)
        {
            genomeCount = controller.GetNextGenomeCount(
                GetFramesPerSecond(genomeCount, genomeCountAtFiftyFramesPerSecond));
        }

        return genomeCount;
    }

    [TestMethod]
    public void GetNextGenomeCount_GrowsWhileAboveTarget()
    {
        var controller = new GenomeCountController();

        Assert.IsTrue(controller.GetNextGenomeCount(60) > GenomeCountController.InitialGenomeCount);
    }

    [TestMethod]
    public void GetNextGenomeCount_ShrinksWhileBelowTarget()
    {
        var controller = new GenomeCountController();

        Assert.IsTrue(controller.GetNextGenomeCount(30) < GenomeCountController.InitialGenomeCount);
    }

    [TestMethod]
    [DataRow(60)]
    [DataRow(500)]
    public void GetNextGenomeCount_SettlesOnTargetFrameRate(int genomeCountAtFiftyFramesPerSecond)
    {
        var genomeCount = RunBatches(
            new GenomeCountController(),
            GenomeCountController.InitialGenomeCount,
            genomeCountAtFiftyFramesPerSecond,
            200);

        var framesPerSecond = GetFramesPerSecond(genomeCount, genomeCountAtFiftyFramesPerSecond);
        Assert.IsTrue(framesPerSecond is > 47 and < 53, $"{genomeCount} genomes gives {framesPerSecond} FPS.");
    }

    [TestMethod]
    public void GetNextGenomeCount_RespectsLimits()
    {
        var growing = new GenomeCountController();
        var shrinking = new GenomeCountController();

        for (var i = 0; i < 500; i++)
        {
            growing.GetNextGenomeCount(60);
            shrinking.GetNextGenomeCount(1);
        }

        Assert.AreEqual(GenomeCountController.MaximumGenomeCount, growing.GetNextGenomeCount(60));
        Assert.AreEqual(GenomeCountController.MinimumGenomeCount, shrinking.GetNextGenomeCount(1));
    }

    [TestMethod]
    public void GetNextGenomeCount_HandlesNoFramesAtAll()
    {
        Assert.IsTrue(new GenomeCountController().GetNextGenomeCount(0) >= GenomeCountController.MinimumGenomeCount);
    }

    [TestMethod]
    [DataRow(125, 100)]
    [DataRow(10, 8)]
    [DataRow(1, 0)]
    public void GetGenomesToRemove_KeepsSurvivalRate(int genomeCount, int expected)
    {
        Assert.AreEqual(expected, GenomeCountController.GetGenomesToRemove(genomeCount));
    }
}
