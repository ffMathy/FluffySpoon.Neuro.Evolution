using Accord.Neuro;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    class AccordNeuronAdapter : INeuron
    {
        private readonly ActivationNeuron accordNeuron;

        public double Bias
        {
            get => accordNeuron.Threshold;
            set => accordNeuron.Threshold = value;
        }

        public ImmutableArray<double> Weights
        {
            get => accordNeuron.Weights.ToImmutableArray();
            set
            {
                for (var i = 0; i < value.Length; i++)
                    accordNeuron.Weights[i] = value[i];
            }
        }

        public AccordNeuronAdapter(
            ActivationNeuron accordNeuron)
        {
            this.accordNeuron = accordNeuron;
        }
    }
}
