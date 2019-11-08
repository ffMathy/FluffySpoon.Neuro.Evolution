using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public class EvolutionSettings<TSimulation> : IEvolutionSettings<TSimulation> where TSimulation : ISimulation
    {
        public Random RandomnessProvider { get; set; } = new Random();

        public int AmountOfWorstGenomesToRemovePerGeneration { get; set; }
        public int AmountOfGenomesInPopulation { get; set; }

        public CreateNewModelDelegate<TSimulation> SimulationFactoryMethod { get; set; }
        public TickCallbackDelegate<TSimulation> PostTickMethod { get; set; }

        public double NeuronMutationProbability { get; set; } = 0.2;

        public int[] NeuronCounts { get; set; }

    }
}
