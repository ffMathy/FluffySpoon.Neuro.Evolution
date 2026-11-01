using System;
using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.Neuro.Evolution.Domain;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;
using FluffySpoon.Neuro.Evolution.Domain.Network;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class ConvergenceTest
{
    [TestMethod]
    public void Evolve_KeepsPopulationSizeConstant()
    {
        var (generation, settings) = CreateGeneration();

        IGeneration<XorSimulation> current = generation;
        for (var i = 0; i < 5; i++)
        {
            current = current.Evolve();
            Assert.AreEqual(settings.AmountOfGenomesInPopulation, current.Genomes.Count);
        }
    }

    [TestMethod]
    public void Evolve_LearnsXor()
    {
        var (generation, _) = CreateGeneration();

        IGeneration<XorSimulation> current = generation;
        for (var i = 0; i < 300; i++)
            current = current.Evolve();

        // The first genome of the next generation is the unmutated elite of the previous one.
        var best = current.Genomes.First().Clone();
        while (!best.Simulation.HasEnded)
            best.Tick();

        Assert.AreEqual(0, best.Simulation.Errors, $"Error sum was {best.Simulation.Fitness}");
    }

    [TestMethod]
    public void EvolveInParallel_LearnsXor()
    {
        var (generation, settings) = CreateGeneration();

        IGeneration<XorSimulation> current = generation;
        for (var i = 0; i < 300; i++)
        {
            current = current.EvolveInParallel();
            Assert.AreEqual(settings.AmountOfGenomesInPopulation, current.Genomes.Count);
        }

        var best = current.Genomes.First().Clone();
        while (!best.Simulation.HasEnded)
            best.Tick();

        Assert.AreEqual(0, best.Simulation.Errors, $"Error sum was {best.Simulation.Fitness}");
    }

    [TestMethod]
    public void Mutate_UsesMutationSettingsChangedAfterCreation()
    {
        var (generation, settings) = CreateGeneration();
        var genome = generation.Clone().Genomes.First().Clone();
        var weightsBefore = GetWeights(genome);

        settings.MutationProbability = 0;
        genome.Mutate();
        CollectionAssert.AreEqual(weightsBefore, GetWeights(genome));

        settings.MutationProbability = 1;
        genome.Mutate();
        CollectionAssert.AreNotEqual(weightsBefore, GetWeights(genome));
    }

    private static float[] GetWeights(IGenome<XorSimulation> genome) => genome.NeuralNetwork.Neurons
        .SelectMany(x => x.DendritesTowardsNextLayer)
        .Select(x => x.Weight)
        .ToArray();

    [TestMethod]
    public void Clone_RunsIndependentlyOfOriginal()
    {
        var (generation, settings) = CreateGeneration();

        var clone = generation.Clone();
        clone.Simulate();

        Assert.AreEqual(settings.AmountOfGenomesInPopulation, clone.Genomes.Count);
        Assert.IsTrue(clone.Genomes.All(x => x.Simulation.HasEnded));
        Assert.IsFalse(generation.Genomes.Any(x => x.Simulation.HasEnded));
    }

    private static (Generation<XorSimulation>, EvolutionSettings<XorSimulation>) CreateGeneration()
    {
        var settings = new EvolutionSettings<XorSimulation>
        {
            AmountOfGenomesInPopulation = 50,
            AmountOfWorstGenomesToRemovePerGeneration = 40,
            NeuronCounts = new[] { 2, 4, 1 },
            MutationProbability = 0.2f,
            MutationStrength = 0.5f,
            RandomnessProvider = new Random(42),
            SimulationFactoryMethod = () => new XorSimulation()
        };

        var generation = new Generation<XorSimulation>(
            settings,
            new GenomeFactory<XorSimulation>(new NeuralNetworkFactory(settings), settings));

        return (generation, settings);
    }
}

public class XorSimulation : ISimulation
{
    private static readonly (float A, float B, bool Expected)[] Cases =
    {
        (0, 0, false), (0, 1, true), (1, 0, true), (1, 1, false)
    };

    private int _index;

    public double Fitness { get; private set; }
    public int Errors { get; private set; }
    public bool HasEnded => _index >= Cases.Length;

    public float[] GetInputs() => new[] { Cases[_index].A, Cases[_index].B };

    public void Tick(float[] outputs)
    {
        var expected = Cases[_index].Expected ? 1 : 0;
        Fitness += Math.Pow(outputs[0] - expected, 2);
        if (NeuronInterpretationHelper.InterpretAsBoolean(outputs[0]) != Cases[_index].Expected)
            Errors++;

        _index++;
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
