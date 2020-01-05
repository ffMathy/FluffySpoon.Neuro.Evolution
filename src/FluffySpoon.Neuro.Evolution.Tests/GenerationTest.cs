using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace FluffySpoon.Neuro.Evolution.Tests
{
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

            await generation.EvolveAsync();
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

        public async Task ResetAsync()
        {
        }

        public async Task<double[]> GetInputsAsync()
        {
            return Array.Empty<double>();
        }

        public async Task TickAsync(double[] outputs)
        {
        }
    }
}
