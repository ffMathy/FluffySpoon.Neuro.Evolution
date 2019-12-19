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
        private int hashOffset;

        [TestMethod]
        public async Task Evolve_HighwayTest()
        {
            var genomes = new List<IGenome<ISimulation>>();
            for (var i = 0; i < 100; i++)
                genomes.Add(GenerateFakeGenome());

            var fakeGenomeFactory = Substitute.For<IGenomeFactory<ISimulation>>();
            fakeGenomeFactory
                .Create()
                .Returns(genomes[0], genomes.Skip(1).ToArray());

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
                .GetHashCode()
                .Returns(c => ++hashOffset);

            fakeGenome
                .Simulation
                .HasEnded
                .Returns(true);

            return fakeGenome;
        }
    }
}
