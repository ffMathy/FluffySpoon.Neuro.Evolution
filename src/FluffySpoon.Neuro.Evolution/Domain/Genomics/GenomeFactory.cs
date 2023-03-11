using FluffySpoon.Neuro.Evolution.Domain.Network;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

class GenomeFactory<TSimulation> : IGenomeFactory<TSimulation> where TSimulation : ISimulation
{
    private readonly INeuralNetworkFactory _neuralNetworkFactory;
    private readonly IEvolutionSettings<TSimulation> _evolutionSettings;

    public GenomeFactory(
        INeuralNetworkFactory neuralNetworkFactory,
        IEvolutionSettings<TSimulation> evolutionSettings)
    {
        _neuralNetworkFactory = neuralNetworkFactory;
        _evolutionSettings = evolutionSettings;
    }

    public IGenome<TSimulation> Create()
    {
        var genome = new Genome<TSimulation>(
            _neuralNetworkFactory.Create(),
            _evolutionSettings);
        return genome;
    }
}