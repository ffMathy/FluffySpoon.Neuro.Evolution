using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class AdaptiveMutationControllerTest
{
    private const float Tolerance = 0.0001f;

    private static void AssertMutation(AdaptiveMutationController controller, float probability, float strength)
    {
        Assert.AreEqual(probability, controller.MutationProbability, Tolerance);
        Assert.AreEqual(strength, controller.MutationStrength, Tolerance);
    }

    private static void AddGenerations(AdaptiveMutationController controller, int generations, double fitness)
    {
        for (var i = 0; i < generations; i++)
            controller.AddGeneration(fitness);
    }

    [TestMethod]
    public void StartsInTheMiddle()
    {
        AssertMutation(new AdaptiveMutationController(), 0.1f, 0.3f);
    }

    [TestMethod]
    public void AddGeneration_FirstGenerationIsNotAnImprovement()
    {
        var controller = new AdaptiveMutationController();

        controller.AddGeneration(-100);

        AssertMutation(controller, 0.1f, 0.3f);
    }

    [TestMethod]
    public void AddGeneration_MutatesGentlyAfterImprovement()
    {
        var controller = new AdaptiveMutationController();

        controller.AddGeneration(-100);
        controller.AddGeneration(-200);

        AssertMutation(controller, 0.05f, 0.1f);
    }

    [TestMethod]
    public void AddGeneration_MutatesBoldlyWhenStalled()
    {
        var controller = new AdaptiveMutationController();

        AddGenerations(controller, 500, -100);

        AssertMutation(controller, 0.15f, 0.5f);
    }

    [TestMethod]
    public void AddGeneration_GetsBolderGradually()
    {
        var controller = new AdaptiveMutationController();
        controller.AddGeneration(-100);
        controller.AddGeneration(-200);

        AddGenerations(controller, 50, -200);

        AssertMutation(controller, 0.1f, 0.3f);
    }

    [TestMethod]
    public void AddGeneration_IgnoresTinyImprovements()
    {
        var controller = new AdaptiveMutationController();

        controller.AddGeneration(-1000);
        for (var i = 1; i <= 500; i++)
            controller.AddGeneration(-1000 - (i * 0.001));

        AssertMutation(controller, 0.15f, 0.5f);
    }

    [TestMethod]
    public void AddGeneration_GettingWorseIsNotAnImprovement()
    {
        var controller = new AdaptiveMutationController();
        controller.AddGeneration(-200);

        AddGenerations(controller, 500, -100);

        AssertMutation(controller, 0.15f, 0.5f);
    }

    [TestMethod]
    public void ForgetProgress_StartsOver()
    {
        var controller = new AdaptiveMutationController();
        controller.AddGeneration(-1000);
        AddGenerations(controller, 500, -1000);

        controller.ForgetProgress();
        AssertMutation(controller, 0.1f, 0.3f);

        // A worse fitness than on the previous map still becomes the new baseline.
        controller.AddGeneration(-10);
        controller.AddGeneration(-20);
        AssertMutation(controller, 0.05f, 0.1f);
    }
}
