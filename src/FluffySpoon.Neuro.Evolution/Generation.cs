using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Generation : IGeneration
    {
        private readonly IEvolutionSettings evolutionSettings;

        private HashSet<IGenome> genomes;

        public IReadOnlyCollection<IGenome> Genomes => genomes;

        public Generation(
            IEvolutionSettings evolutionSettings)
        {
            genomes = new HashSet<IGenome>();
            this.evolutionSettings = evolutionSettings;
        }

        public void AddGenome(IGenome genome)
        {
            genomes.Add(genome);
        }

        public void RemoveGenome(IGenome genome)
        {
            genomes.Remove(genome);
        }

        public async Task<IGenome> CrossTwoRandomGenomesAsync()
        {
            var randomGenome1 = PickRandomGenome();
            var randomGenome2 = PickRandomGenome();

            var crossOverSimulation = await randomGenome1.CrossWithAsync(randomGenome2);
            return crossOverSimulation;
        }

        public IGenome PickRandomGenome()
        {
            var random = evolutionSettings.RandomnessProvider;
            return genomes
                .Skip(random.Next(0, genomes.Count))
                .First();
        }

        public async Task<IGeneration> EvolveAsync()
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
            IGeneration sourceGeneration, 
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
            //var worstPerformingGenomes = genomes
            //    .OrderByDescending(x => x.Fitness);
        }

        public IGeneration Clone()
        {
            return new Generation(evolutionSettings)
            {
                genomes = genomes
            };
        }
    }
}
