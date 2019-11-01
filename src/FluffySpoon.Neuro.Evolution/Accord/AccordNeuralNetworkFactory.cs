using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    class AccordNeuralNetworkFactory : INeuralNetworkFactory
    {
        private readonly INeuralNetworkSettings settings;

        public AccordNeuralNetworkFactory(
            INeuralNetworkSettings settings)
        {
            this.settings = settings;
        }

        public INeuralNetwork Create()
        {
            return new AccordNeuralNetwork(
                settings,
                null);
        }
    }
}
