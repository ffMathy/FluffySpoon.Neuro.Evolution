using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Generation<TModel> : IGeneration<TModel> where TModel : IModel<TModel>
    {
        private readonly IEvolutionSettings<TModel> evolutionSettings;

        private HashSet<IGenome<TModel>> genomes;

        public IReadOnlyCollection<IGenome<TModel>> Genomes => genomes;

        public Generation(
            IEvolutionSettings<TModel> evolutionSettings)
        {
            genomes = new HashSet<IGenome<TModel>>();
            this.evolutionSettings = evolutionSettings;
        }

        public void AddGenome(IGenome<TModel> genome)
        {
            genomes.Add(genome);
        }

        public void RemoveGenome(IGenome<TModel> genome)
        {
            genomes.Remove(genome);
        }

        public async Task<IGenome<TModel>> CrossTwoRandomGenomesAsync()
        {
            var randomGenome1 = PickRandomGenome();
            var randomGenome2 = PickRandomGenome();

            var crossOverSimulation = await randomGenome1.CrossWithAsync(randomGenome2);
            return crossOverSimulation;
        }

        public IGenome<TModel> PickRandomGenome()
        {
            var random = evolutionSettings.RandomnessProvider;
            return genomes
                .Skip(random.Next(0, genomes.Count))
                .First();
        }

        public async Task<IGeneration<TModel>> EvolveAsync()
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
            IGeneration<TModel> sourceGeneration, 
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

        public IGeneration<TModel> Clone()
        {
            return new Generation<TModel>(evolutionSettings)
            {
                genomes = genomes
            };
        }
    }
}
