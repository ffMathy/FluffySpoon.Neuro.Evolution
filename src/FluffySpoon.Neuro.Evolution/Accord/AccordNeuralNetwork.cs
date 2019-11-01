using Accord.Neuro;
using Accord.Neuro.Learning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    class AccordNeuralNetwork : INeuralNetwork
    {
        private readonly ActivationNetwork network;
        private readonly BackPropagationLearning teacher;

        private readonly INeuralNetworkSettings neuralNetworkSettings;

        public AccordNeuralNetwork(
            INeuralNetworkSettings neuralNetworkSettings,
            ActivationNetwork activationNetwork)
        {
            this.network = activationNetwork ?? new ActivationNetwork(
                new ThresholdFunction(),
                neuralNetworkSettings.NeuronCounts[0],
                neuralNetworkSettings.NeuronCounts.Skip(1).ToArray());

            this.teacher = new BackPropagationLearning(this.network);
            this.neuralNetworkSettings = neuralNetworkSettings;
        }

        public double[] Ask(double[] input)
        {
            return network.Compute(input);
        }

        public Task<INeuralNetwork> CloneAsync()
        {
            return Task.FromResult<INeuralNetwork>(
                new AccordNeuralNetwork(
                    neuralNetworkSettings,
                    Copy(this.network)));
        }

        private ActivationNetwork Copy(ActivationNetwork network)
        {
            using (var memoryStream = new MemoryStream())
            {
                network.Save(memoryStream);
                memoryStream.Position = 0;

                return (ActivationNetwork)Network.Load(memoryStream);
            }
        }

        public IReadOnlyList<INeuron> GetAllNeurons()
        {
            return network.Layers
                .SelectMany(x => x.Neurons)
                .Cast<ActivationNeuron>()
                .Select(x => new AccordNeuronAdapter(x))
                .ToArray();
        }

        public Task TrainAsync(IEnumerable<double[]> input, IEnumerable<double[]> expectedOutput)
        {
            teacher.RunEpoch(
                input.ToArray(), 
                expectedOutput.ToArray());

            return Task.CompletedTask;
        }

        public void WipeAllTraining()
        {
            this.network.Randomize();
        }
    }
}
