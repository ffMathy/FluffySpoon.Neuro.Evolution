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
            IEvolutionSettings<TSimulation> evolutionSettings)
        {
            genomes = new HashSet<IGenome<TSimulation>>();
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

        public async Task<IGeneration<TSimulation>> EvolveAsync()
        {
            var clone = Clone();

            var currentGenomeCount = clone.Genomes.Count;

            clone.RemoveWorstPerformingGenomes();

            foreach (var genome in clone.Genomes.AsParallel())
                await genome.EnsureTrainedAsync();

            await BreedNewGenomesAsync(clone, currentGenomeCount);

            return clone;
        }

        private static async Task BreedNewGenomesAsync(
            IGeneration<TSimulation> sourceGeneration, 
            int targetGenomeCount)
        {
            while (sourceGeneration.Genomes.Count < targetGenomeCount)
            {
                var crossOver = await sourceGeneration.CrossTwoRandomGenomesAsync();
                await crossOver.MutateAsync();

                sourceGeneration.AddGenome(crossOver);
            }
        }

        public void RemoveWorstPerformingGenomes()
        {
            var worstPerformingGenomes = genomes
                .OrderByDescending(x => x.Simulation.Fitness)
                .Take(evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration);
            foreach(var genome in worstPerformingGenomes)
                RemoveGenome(genome);
        }

        public IGeneration<TSimulation> Clone()
        {
            return new Generation<TSimulation>(evolutionSettings)
            {
                genomes = genomes
            };
        }
    }
}
