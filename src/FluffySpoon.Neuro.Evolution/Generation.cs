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
        private readonly IGenomeFactory<TSimulation> genomeFactory;

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
            this.genomeFactory = genomeFactory;
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

        private async Task<bool> TickAsync()
        {
            var endedCount = 0;

            foreach (var genome in genomes) { 
                await genome.TickAsync();

                if(genome.Simulation.HasEnded)
                    endedCount++;
            }

            evolutionSettings.PostTickMethod?.Invoke(genomes);

            return endedCount == genomes.Count;
        }

        public async Task<IGeneration<TSimulation>> EvolveAsync()
        {
            while(!await TickAsync());

            var clone = await CloneAsync();
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

        private async Task BreedNewGenomeAsync(IGeneration<TSimulation> sourceGeneration)
        {
            var crossOver = await sourceGeneration.CrossTwoRandomGenomesAsync();
            await crossOver.MutateAsync(evolutionSettings.NeuronMutationProbability);

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

        public async Task<IGeneration<TSimulation>> CloneAsync()
        {
            var genomesClone = await Task.WhenAll(genomes
                .Select(x => x.CloneAsync()));
            return new Generation<TSimulation>(
                evolutionSettings,
                genomeFactory)
            {
                genomes = new HashSet<IGenome<TSimulation>>(genomesClone)
            };
        }
    }
}
