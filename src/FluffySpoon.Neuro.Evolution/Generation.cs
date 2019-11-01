using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Generation<TSimulation> : IGeneration<TSimulation> where TSimulation : ISimulation
    {
        private readonly IEvolutionSettings<TSimulation> evolutionSettings;

        private HashSet<IGenome<TSimulation>> genomes;

        public IReadOnlyCollection<IGenome<TSimulation>> Genomes => genomes;

        public Generation(
            IEvolutionSettings<TSimulation> evolutionSettings,
            IGenomeFactory<TSimulation> genomeFactory)
        {
            genomes = new HashSet<IGenome<TSimulation>>();

            for (var i = 0; i < evolutionSettings.AmountOfGenomesInPopulation; i++)
                genomes.Add(genomeFactory.Create());

            this.evolutionSettings = evolutionSettings;
        }

        public void AddGenome(IGenome<TSimulation> genome)
        {
            genomes.Add(genome);
        }

        public void RemoveGenome(IGenome<TSimulation> genome)
        {
            genomes.Remove(genome);
            genome.Dispose();
        }

        public async Task<IGenome<TSimulation>> CrossTwoRandomGenomesAsync()
        {
            var randomGenome1 = PickRandomGenome();
            var randomGenome2 = PickRandomGenome();

            var crossOverSimulation = await randomGenome1.CrossWithAsync(randomGenome2);
            return crossOverSimulation;
        }

        public IGenome<TSimulation> PickRandomGenome()
        {
            var random = evolutionSettings.RandomnessProvider;
            return genomes
                .Skip(random.Next(0, genomes.Count))
                .First();
        }

        public async Task TickAsync()
        {
            foreach(var genome in genomes)
                await genome.TickAsync();
        }

        public async Task<IGeneration<TSimulation>> EvolveAsync()
        {
            var clone = Clone();

            clone.RemoveWorstPerformingGenomes();

            foreach (var genome in clone.Genomes.AsParallel())
                await genome.EnsureTrainedAsync();

            await BreedNewGenomesAsync(clone);

            return clone;
        }

        private async Task BreedNewGenomesAsync(
            IGeneration<TSimulation> sourceGeneration)
        {
            while (sourceGeneration.Genomes.Count < evolutionSettings.AmountOfGenomesInPopulation)
                await BreedNewGenomeAsync(sourceGeneration);
        }

        private static async Task BreedNewGenomeAsync(IGeneration<TSimulation> sourceGeneration)
        {
            var crossOver = await sourceGeneration.CrossTwoRandomGenomesAsync();
            await crossOver.MutateAsync();

            sourceGeneration.AddGenome(crossOver);
        }

        public void RemoveWorstPerformingGenomes()
        {
            var worstPerformingGenomes = genomes
                .OrderByDescending(x => x.Simulation.Fitness)
                .Take(evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration);

            foreach (var genome in worstPerformingGenomes)
                RemoveGenome(genome);
        }

        public IGeneration<TSimulation> Clone()
        {
            return new Generation<TSimulation>(
                evolutionSettings,
                null)
            {
                genomes = genomes
            };
        }
    }
}
