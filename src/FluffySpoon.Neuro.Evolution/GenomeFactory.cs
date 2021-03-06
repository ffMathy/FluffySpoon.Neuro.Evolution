﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    class GenomeFactory<TSimulation> : IGenomeFactory<TSimulation> where TSimulation : ISimulation
    {
        private readonly INeuralNetworkFactory neuralNetworkFactory;
        private readonly IEvolutionSettings<TSimulation> evolutionSettings;

        public GenomeFactory(
            INeuralNetworkFactory neuralNetworkFactory,
            IEvolutionSettings<TSimulation> evolutionSettings)
        {
            this.neuralNetworkFactory = neuralNetworkFactory;
            this.evolutionSettings = evolutionSettings;
        }

        public IGenome<TSimulation> Create()
        {
            var genome = new Genome<TSimulation>(
                neuralNetworkFactory.Create(),
                evolutionSettings);
            genome.Initialize();

            return genome;
        }
    }
}
