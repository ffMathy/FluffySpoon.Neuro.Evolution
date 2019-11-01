using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public static class DotNetRegistrationExtensions
    {
        public static void AddFluffySpoonNeuroEvolution<TSimulation>(this IServiceCollection services, NeuralNetworkSettings<TSimulation> neuralNetworkSettings)
        {

        }
    }
}
