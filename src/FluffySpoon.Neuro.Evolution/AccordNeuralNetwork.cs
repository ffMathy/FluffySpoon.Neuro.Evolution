using Accord.Neuro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    class AccordNeuralNetwork : INeuralNetwork
    {
        private readonly ActivationNetwork network;

        public AccordNeuralNetwork(INeuralNetworkSettings neuralNetworkSettings)
        {
            this.network = new ActivationNetwork(
                new ThresholdFunction(),
                neuralNetworkSettings.NeuronCounts[0],
                neuralNetworkSettings.NeuronCounts.Skip(1).ToArray());
        }

        public double[] Ask(double[] input)
        {
            throw new NotImplementedException();
        }

        public Task<INeuralNetwork> CloneAsync()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<INeuron> GetAllNeurons()
        {
            throw new NotImplementedException();
        }

        public Task TrainAsync(IEnumerable<double[]> input, IEnumerable<double[]> expectedOutput)
        {
            throw new NotImplementedException();
        }

        public void WipeAllTraining()
        {
            throw new NotImplementedException();
        }
    }
}
