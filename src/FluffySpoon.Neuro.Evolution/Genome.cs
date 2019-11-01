using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Genome : IGenome
    {
        private readonly IDictionary<double[], double[]> dnaStringChains;
        private readonly Func<double> fitnessCalculationFunction;

        public Genome(
            Func<double> fitnessCalculationFunction)
        {
            this.fitnessCalculationFunction = fitnessCalculationFunction;

            this.dnaStringChains = new Dictionary<double[], double[]>();
        }

        public void AddBasePair(double[] inputs, double[] outputs)
        {
            dnaStringChains.Add(inputs, outputs);
        }

        public Task<double[]> AskAsync(double[] input)
        {
            throw new NotImplementedException();
        }

        public Task<IGenome> CrossWithAsync(IGenome other)
        {
            throw new NotImplementedException();
        }

        public Task MutateAsync()
        {
            throw new NotImplementedException();
        }

        public void RemoveBasePair(double[] inputs)
        {
            dnaStringChains.Remove(inputs);
        }
    }
}
