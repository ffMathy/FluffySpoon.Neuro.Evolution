using System;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Domain;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;
using FluffySpoon.Neuro.Evolution.Domain.Network;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class NeuralNetworkTest
{
    private static EvolutionSettings<ISimulation> CreateSettings(params int[] neuronCounts) => new()
    {
        NeuronCounts = neuronCounts,
        RandomnessProvider = new Random(1337),
        MutationProbability = 1f,
        MutationStrength = 0.5f
    };

    [TestMethod]
    public void Create_ConnectsEveryNeuronToEveryNeuronInTheNextLayer()
    {
        var network = NeuralNetwork.CreateRandom(CreateSettings(3, 4, 2));

        Assert.AreEqual(3, network.Layers.Count);
        foreach (var neuron in network.Layers[0].Neurons)
            Assert.AreEqual(4, neuron.DendritesTowardsNextLayer.Count);
        foreach (var neuron in network.Layers[1].Neurons)
            Assert.AreEqual(2, neuron.DendritesTowardsNextLayer.Count);
        foreach (var neuron in network.Layers[2].Neurons)
            Assert.AreEqual(0, neuron.DendritesTowardsNextLayer.Count);
    }

    [TestMethod]
    public void Ask_ReturnsOutputsInZeroToOneRange()
    {
        var network = NeuralNetwork.CreateRandom(CreateSettings(3, 4, 2));

        var outputs = network.Ask(new[] { 100f, -100f, 0.5f });

        Assert.AreEqual(2, outputs.Length);
        Assert.IsTrue(outputs.All(x => x is >= 0 and <= 1));
    }

    [TestMethod]
    public void Ask_ComputesWeightedSumThroughLayers()
    {
        var network = NeuralNetwork.CreateRandom(CreateSettings(2, 1));
        var inputs = network.Layers[0].Neurons;
        inputs[0].DendritesTowardsNextLayer[0].Weight = 2;
        inputs[1].DendritesTowardsNextLayer[0].Weight = -1;
        network.Layers[1].Neurons[0].Bias = 0.5f;

        var output = network.Ask(new[] { 1f, 3f })[0];

        var expected = 1 / (1 + Math.Exp(-(2 * 1 - 1 * 3 + 0.5)));
        Assert.AreEqual(expected, output, 1e-5);
    }

    [TestMethod]
    public void Clone_ProducesIdenticalButIndependentNetwork()
    {
        var network = NeuralNetwork.CreateRandom(CreateSettings(3, 5, 4, 2));
        var input = new[] { 0.1f, 0.7f, -0.3f };

        var clone = (NeuralNetwork)network.Clone();

        CollectionAssert.AreEqual(network.Ask(input), clone.Ask(input));
        Assert.AreEqual(network.Neurons.Length, clone.Neurons.Length);
        for (var i = 0; i < network.Neurons.Length; i++)
            Assert.AreEqual(
                network.Neurons[i].DendritesTowardsNextLayer.Count,
                clone.Neurons[i].DendritesTowardsNextLayer.Count);

        clone.Layers[1].Neurons[0].Bias += 10;
        Assert.AreNotEqual(network.Ask(input)[0], clone.Ask(input)[0]);
        Assert.AreNotEqual(network.Layers[1].Neurons[0].Bias, clone.Layers[1].Neurons[0].Bias);
    }

    [TestMethod]
    public void Mutate_NudgesGenesInsteadOfReplacingThem()
    {
        var settings = CreateSettings(3, 4, 2);
        settings.MutationStrength = 0.01f;
        settings.SimulationFactoryMethod = () => new AlreadyEndedSimulation();

        var network = NeuralNetwork.CreateRandom(settings);
        var before = network.Neurons.Select(x => x.Bias).ToArray();

        var genome = new Genome<ISimulation>(network, settings);
        genome.Mutate();

        var after = network.Neurons.Select(x => x.Bias).ToArray();
        for (var i = 0; i < before.Length; i++)
        {
            Assert.AreNotEqual(before[i], after[i]);
            Assert.AreEqual(before[i], after[i], 0.1);
        }
    }
}

[TestClass]
public class NeuronInterpretationHelperTest
{
    public enum Steering { Left, None, Right }

    [DataTestMethod]
    [DataRow(0.0, Steering.Left)]
    [DataRow(0.2, Steering.Left)]
    [DataRow(0.5, Steering.None)]
    [DataRow(0.9, Steering.Right)]
    [DataRow(1.0, Steering.Right)]
    public void InterpretAsEnum_SplitsRangeEvenly(double output, Steering expected)
    {
        Assert.AreEqual(expected, NeuronInterpretationHelper.InterpretAsEnum<Steering>(output));
    }

    [TestMethod]
    public void ConvertEnumToNeuronInput_RoundTrips()
    {
        foreach (var value in Enum.GetValues<Steering>())
            Assert.AreEqual(value, NeuronInterpretationHelper.InterpretAsEnum<Steering>(
                NeuronInterpretationHelper.ConvertEnumToNeuronInput(value)));
    }
}
