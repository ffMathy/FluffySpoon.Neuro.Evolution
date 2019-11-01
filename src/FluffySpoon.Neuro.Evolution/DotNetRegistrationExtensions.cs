using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public static class DotNetRegistrationExtensions
    {
        public static void AddFluffySpoonNeuroEvolution<TSimulation>(
            this IServiceCollection services,
            IEvolutionSettings<TSimulation> settings
        ) where TSimulation : ISimulation
        {
            services.AddSingleton(settings);
            services.AddSingleton<INeuralNetworkSettings>(settings);

            services.AddScoped(typeof(IGeneration<>), typeof(Generation<>));
            services.AddScoped(typeof(IGenomeFactory<>), typeof(GenomeFactory<>));

            services.AddScoped(typeof(INeuralNetworkFactory), typeof(AccordNeuralNetworkFactory));
        }
    }
}
