using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public class NeuralNetworkSettings<TModel> : IEvolutionSettings<TModel>
    {
        public Random RandomnessProvider { get; set; } = new Random();

        public int AmountOfWorstGenomesToRemovePerGeneration { get; set; }
        public int AmountOfGenomesInPopulation { get; set; }

        public CalculateFitnessOfModelDelegate<TModel> FitnessCalculationMethod { get; set; }
        public CreateNewModelDelegate<TModel> ModelFactoryMethod { get; set; }

        public double NeuronMutationProbability { get; set; } = 0.2;

        public int[] NeuronCounts { get; set; }
    }
}
