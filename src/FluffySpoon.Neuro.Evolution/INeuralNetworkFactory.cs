using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public interface INeuralNetworkFactory
    {
        INeuralNetwork Create();
    }
}
