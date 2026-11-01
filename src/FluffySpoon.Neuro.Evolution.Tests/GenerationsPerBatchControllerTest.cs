using System;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class GenerationsPerBatchControllerTest
{
    /// <summary>
    /// Runs batches where every run takes the given time, and returns the amount of generations it ends up at.
    /// </summary>
    private static int RunBatches(GenerationsPerBatchController controller, int generations, TimeSpan runTime, int batches)
    {
        for (var i = 0; i < batches; i++)
        {
            controller.AddRun(runTime);
            generations = controller.GetNextGenerationsPerBatch(generations);
        }

        return generations;
    }

    private static void AssertTrainsForAboutTenSeconds(int generations, TimeSpan runTime)
    {
        var trainingTime = runTime.TotalSeconds * (generations - 1);
        Assert.IsTrue(trainingTime is > 9 and < 11, $"{generations} generations train for {trainingTime} seconds.");
    }

    [TestMethod]
    public void GetNextGenerationsPerBatch_KeepsAmountWithoutRuns()
    {
        Assert.AreEqual(5, new GenerationsPerBatchController().GetNextGenerationsPerBatch(5));
    }

    [TestMethod]
    [DataRow(0.008)]
    [DataRow(0.1)]
    [DataRow(1.5)]
    public void GetNextGenerationsPerBatch_SettlesOnTargetTime(double runTimeInSeconds)
    {
        var runTime = TimeSpan.FromSeconds(runTimeInSeconds);

        var generations = RunBatches(new GenerationsPerBatchController(), 1, runTime, 25);

        AssertTrainsForAboutTenSeconds(generations, runTime);
    }

    [TestMethod]
    public void GetNextGenerationsPerBatch_FollowsChangingRunTimes()
    {
        var controller = new GenerationsPerBatchController();

        var fastRunTime = TimeSpan.FromSeconds(0.01);
        var generations = RunBatches(controller, 1, fastRunTime, 25);
        AssertTrainsForAboutTenSeconds(generations, fastRunTime);

        var slowRunTime = TimeSpan.FromSeconds(0.2);
        generations = RunBatches(controller, generations, slowRunTime, 45);
        AssertTrainsForAboutTenSeconds(generations, slowRunTime);
    }

    [TestMethod]
    public void GetNextGenerationsPerBatch_RespectsLimits()
    {
        Assert.AreEqual(
            GenerationsPerBatchController.MinimumGenerationsPerBatch,
            RunBatches(new GenerationsPerBatchController(), 50, TimeSpan.FromMinutes(10), 25));

        Assert.AreEqual(
            GenerationsPerBatchController.MaximumGenerationsPerBatch,
            RunBatches(new GenerationsPerBatchController(), 50, TimeSpan.Zero, 25));
    }

    [TestMethod]
    public void GetNextGenerationsPerBatch_RecoversQuicklyFromLimit()
    {
        var controller = new GenerationsPerBatchController();
        RunBatches(controller, 50, TimeSpan.FromMinutes(10), 100);

        var runTime = TimeSpan.FromSeconds(0.5);
        var generations = RunBatches(controller, GenerationsPerBatchController.MinimumGenerationsPerBatch, runTime, 45);

        AssertTrainsForAboutTenSeconds(generations, runTime);
    }

    [TestMethod]
    public void GetNextGenerationsPerBatch_ContinuesFromTypedAmount()
    {
        var controller = new GenerationsPerBatchController();
        RunBatches(controller, 1, TimeSpan.FromSeconds(0.1), 25);

        // A hundred generations of 0.1 seconds is right on target, so nothing should change much.
        controller.AddRun(TimeSpan.FromSeconds(0.1));
        var generations = controller.GetNextGenerationsPerBatch(101);

        Assert.IsTrue(generations is > 90 and < 110, $"Jumped to {generations} generations.");
    }

    [TestMethod]
    public void ForgetRuns_ForgetsRunTimes()
    {
        var controller = new GenerationsPerBatchController();
        controller.AddRun(TimeSpan.FromSeconds(1));

        controller.ForgetRuns();

        Assert.IsNull(controller.AverageRunTime);
    }
}
