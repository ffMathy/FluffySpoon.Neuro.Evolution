using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public static class DotNetRegistrationExtensions
    {
        public static void AddFluffySpoonNeuroEvolution<TModel>(this IServiceCollection services, NeuralNetworkSettings<TModel> neuralNetworkSettings)
        {

        }
    }
}
