using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Genome : IGenome
    {
        private readonly IDictionary<double[], double[]> basePairs;

        private readonly INeuralNetwork neuralNetwork;
        private readonly IEvolutionSettings evolutionSettings;

        private bool hasTrained;

        public Genome(
            INeuralNetwork neuralNetwork,
            IEvolutionSettings evolutionSettings)
        {
            this.basePairs = new Dictionary<double[], double[]>();

            this.neuralNetwork = neuralNetwork;
            this.evolutionSettings = evolutionSettings;
        }

        public async Task<INeuron[]> GetTrainedNeuronsAsync()
        {
            await TrainIfNeededAsync();
            return neuralNetwork.GetAllNeurons();
        }

        public void AddBasePair(double[] inputs, double[] expectedOutputs)
        {
            hasTrained = false;
            basePairs.Add(inputs, expectedOutputs);
        }

        public void RemoveBasePair(double[] inputs)
        {
            hasTrained = false;
            basePairs.Remove(inputs);
        }

        public async Task<double[]> AskAsync(double[] input)
        {
            await TrainIfNeededAsync();
            return neuralNetwork.Ask(input);
        }

        private async Task TrainIfNeededAsync()
        {
            if (hasTrained)
                return;

            neuralNetwork.WipeAllTraining();

            await neuralNetwork.TrainAsync(basePairs.Keys, basePairs.Values);
            hasTrained = true;
        }

        public async Task MutateAsync()
        {
            await TrainIfNeededAsync();

            var random = evolutionSettings.RandomnessProvider;

            var neurons = neuralNetwork.GetAllNeurons(); 
            foreach (var neuron in neurons)
            {
                if (random.NextDouble() < evolutionSettings.NeuronMutationProbability)
                    continue;

                neuron.Bias = MutateNeuronValue(neuron.Bias);

                neuron.Weights = neuron.Weights
                    .Select(MutateNeuronValue)
                    .ToImmutableArray();
            }
        }

        private double MutateNeuronValue(double value)
        {
            //adjust between -2 to 2.
            //TODO: perhaps make it adjustable - maybe it's not the best way.
            var random = evolutionSettings.RandomnessProvider;
            return value * (random.NextDouble() - 0.5) * 3 + (random.NextDouble() - 0.5);
        }

        public async Task CrossWithAsync(IGenome other)
        {
            var neuronsA = await GetTrainedNeuronsAsync();
            var neuronsB = await other.GetTrainedNeuronsAsync();

            RandomSwap(ref neuronsA, ref neuronsB);

            var slicePoint = evolutionSettings.RandomnessProvider.Next(0, neuronsA.Length + 1);
            for (var i = slicePoint; i < neuronsA.Length; i++)
            {
                SwapNeuronBiases(
                    neuronsA,
                    neuronsB,
                    i);
            }
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            var temporary = a;
            a = b;
            b = temporary;
        }

        private void RandomSwap<T>(ref T a, ref T b)
        {
            if (evolutionSettings.RandomnessProvider.NextDouble() <= 0.5)
                return;

            Swap(ref a, ref b);
        }

        private static void SwapNeuronBiases(
            IReadOnlyList<INeuron> aNetworkNeurons,
            IReadOnlyList<INeuron> bNetworkNeurons,
            int i)
        {
            var temporary = aNetworkNeurons[i].Bias;
            aNetworkNeurons[i].Bias = bNetworkNeurons[i].Bias;
            bNetworkNeurons[i].Bias = temporary;
        }

        public void SwapWith(IGenome other)
        {
            throw new NotImplementedException();
        }
    }
}
