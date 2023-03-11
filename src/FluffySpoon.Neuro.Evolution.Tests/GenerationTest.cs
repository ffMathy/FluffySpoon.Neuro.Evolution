using System;
using System.Threading.Tasks;
using FluffySpoon.Neuro.Evolution.Domain;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class GenerationTest
{
    [TestMethod]
    public async Task Evolve_HighwayTest()
    {
        var genome1 = GenerateFakeGenome();
        var genome2 = GenerateFakeGenome();
        var genome3 = GenerateFakeGenome();
        var genome4 = GenerateFakeGenome();
        var genome5 = GenerateFakeGenome();

        var fakeGenomeFactory = Substitute.For<IGenomeFactory<ISimulation>>();
        fakeGenomeFactory
            .Create()
            .Returns(
                genome1,
                genome2,
                genome3,
                genome4,
                genome5);

        var generation = new Generation<ISimulation>(
            new EvolutionSettings<ISimulation>() {
                AmountOfGenomesInPopulation = 5,
                AmountOfWorstGenomesToRemovePerGeneration = 2
            },
            fakeGenomeFactory);

        generation.Evolve();
    }

    private IGenome<ISimulation> GenerateFakeGenome()
    {
        var fakeGenome = Substitute.For<IGenome<ISimulation>>();

        fakeGenome
            .Simulation
            .Returns(new AlreadyEndedSimulation());

        return fakeGenome;
    }
}

public class AlreadyEndedSimulation : ISimulation
{
    public double Fitness { get; }
    public bool HasEnded { get; } = true;
    
    public float[] GetInputs()
    {
        return Array.Empty<float>();
    }

    public void Tick(float[] outputs)
    {
    }
}