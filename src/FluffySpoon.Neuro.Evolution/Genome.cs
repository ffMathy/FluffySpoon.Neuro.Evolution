using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Genome<TSimulation> : IGenome<TSimulation> where TSimulation : ISimulation
    {
        private readonly IEvolutionSettings<TSimulation> evolutionSettings;

        public INeuralNetwork NeuralNetwork { get; }
        public TSimulation Simulation { get; }

        public Genome(
            INeuralNetwork neuralNetwork,
            IEvolutionSettings<TSimulation> evolutionSettings)
        {
            this.evolutionSettings = evolutionSettings;

            Simulation = evolutionSettings.SimulationFactoryMethod();
            NeuralNetwork = neuralNetwork;
        }

        public async Task<double[]> AskAsync(double[] input)
        {
            return NeuralNetwork.Ask(input);
        }

        public async Task MutateAsync(double mutationProbability)
        {
            var random = evolutionSettings.RandomnessProvider;

            var neurons = NeuralNetwork.GetAllNeurons();
            foreach (var neuron in neurons)
            {
                if (random.NextDouble() < mutationProbability)
                    continue;

                MutateNeuron(neuron);
            }
        }

        private void MutateNeuron(INeuron neuron)
        {
            neuron.Bias = MutateNeuronValue(neuron.Bias);

            neuron.Weights = neuron.Weights
                .Select(MutateNeuronValue)
                .ToImmutableArray();
        }

        private double MutateNeuronValue(double value)
        {
            //adjust between -2 to 2.
            //TODO: perhaps make it adjustable - maybe it's not the best way.
            var random = evolutionSettings.RandomnessProvider;
            return value * (random.NextDouble() - 0.5) * 3 + (random.NextDouble() - 0.5);
        }

        public async Task<IGenome<TSimulation>> CrossWithAsync(IGenome<TSimulation> other)
        {
            var a = (IGenome<TSimulation>)this;
            var b = other;

            RandomSwap(
                ref a,
                ref b);

            var cloneA = await a.NeuralNetwork.CloneAsync();
            var cloneB = await b.NeuralNetwork.CloneAsync();

            SwapNeuralNetworkNeuronBiases(
                cloneA,
                cloneB);

            return new Genome<TSimulation>(
                cloneA,
                evolutionSettings);
        }

        private void SwapNeuralNetworkNeuronBiases(
            INeuralNetwork cloneA,
            INeuralNetwork cloneB)
        {
            var neuronsA = cloneA.GetAllNeurons();
            var neuronsB = cloneB.GetAllNeurons();

            var slicePoint = evolutionSettings.RandomnessProvider.Next(0, neuronsA.Count + 1);
            for (var i = slicePoint; i < neuronsA.Count; i++)
            {
                SwapNeuronBiases(
                    neuronsA,
                    neuronsB,
                    i);
            }
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

        public async Task SwapWithAsync(IGenome<TSimulation> other)
        {
            SwapNeuralNetworkNeuronBiases(
                NeuralNetwork,
                other.NeuralNetwork);
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

        public void Dispose()
        {
            if (Simulation is IDisposable disposable)
                disposable.Dispose();
        }

        public async Task TickAsync()
        {
            if (Simulation.HasEnded)
                return;

            var inputs = await Simulation.GetInputsAsync();
            var outputs = await AskAsync(inputs);

            await Simulation.TickAsync(outputs);
        }

        public async Task<IGenome<TSimulation>> CloneAsync()
        {
            return new Genome<TSimulation>(
                await NeuralNetwork.CloneAsync(),
                evolutionSettings);
        }

        public void Initialize()
        {
            foreach(var neuron in NeuralNetwork.GetAllNeurons())
                MutateNeuron(neuron);
        }
    }
}
