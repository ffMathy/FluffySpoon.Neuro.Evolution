using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public class Genome : IGenome
    {
        private readonly IDictionary<double[], double[]> dnaStrings;
        private readonly Func<double> fitnessCalculationFunction;

        public Genome(
            Func<double> fitnessCalculationFunction)
        {
            this.fitnessCalculationFunction = fitnessCalculationFunction;

            dnaStrings = new Dictionary<double[], double[]>();
        }

        public Task AddChainAsync(double[] inputs, double[] outputs)
        {
            throw new NotImplementedException();
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
    }
}
