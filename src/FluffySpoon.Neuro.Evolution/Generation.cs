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
        private LinkedList<IGenome<TSimulation>> bestGenomes;

        public IReadOnlyCollection<IGenome<TSimulation>> Genomes => genomes;

        public Generation(
            IEvolutionSettings<TSimulation> evolutionSettings,
            IGenomeFactory<TSimulation> genomeFactory)
        {
            genomes = new HashSet<IGenome<TSimulation>>();
            bestGenomes = new LinkedList<IGenome<TSimulation>>();

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

        /// <returns>True if all simulations have ended.</returns>
        private async Task<bool> TickAsync()
        {
            await InitializeIfNeededAsync();

            var endedCount = 0;

            bestGenomes.Clear();

            foreach (var genome in genomes)
            {
                await genome.TickAsync();

                if (genome.Simulation.HasEnded)
                    endedCount++;

                CheckIfGenomeIsAmongBestGenomes(genome);
            }

            evolutionSettings.PostTickMethod?.Invoke(new Genomes<TSimulation>(
                genomes,
                bestGenomes));

            return endedCount == genomes.Count;
        }

        private void CheckIfGenomeIsAmongBestGenomes(IGenome<TSimulation> genome)
        {
            var lowestBestGenomeSoFar = bestGenomes.Last?.Value;
            var currentFitness = genome.Simulation.Fitness;
            if (lowestBestGenomeSoFar != null && currentFitness >= lowestBestGenomeSoFar.Simulation.Fitness)
                return;

            var nodeToInsertAt = bestGenomes.Last;
            while (nodeToInsertAt?.Value != null && nodeToInsertAt.Value.Simulation.Fitness < currentFitness)
                nodeToInsertAt = nodeToInsertAt.Previous;

            if (nodeToInsertAt == null)
            {
                bestGenomes.AddFirst(genome);
            }
            else
            {
                bestGenomes.AddBefore(nodeToInsertAt, genome);
            }

            var amountOfGenomesToKeep =
                evolutionSettings.AmountOfGenomesInPopulation -
                evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration;
            if (bestGenomes.Count > amountOfGenomesToKeep)
                bestGenomes.RemoveLast();
        }

        private async Task InitializeIfNeededAsync()
        {
            if (genomes.Count > 0)
                return;

            genomes.Add(genomeFactory.Create());
            await BreedNewGenomesAsync(this);
        }

        public async Task<IGeneration<TSimulation>> EvolveAsync()
        {
            while(!await TickAsync());

            var generationClone = await CloneAsync();
            generationClone.RemoveWorstPerformingGenomes();

            await BreedNewGenomesAsync(generationClone);

            return generationClone;
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
            var generation = new Generation<TSimulation>(
                evolutionSettings,
                genomeFactory)
            {
                genomes = new HashSet<IGenome<TSimulation>>(genomesClone)
            };

            return generation;
        }
    }
}
