using System;
using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.Neuro.Evolution.Domain.Network;
using FluffySpoon.Neuro.Evolution.Infrastructure.Extensions;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

public class Genome<TSimulation> : IGenome<TSimulation> where TSimulation : ISimulation
{
    private readonly IEvolutionSettings<TSimulation> _evolutionSettings;

    public INeuralNetwork NeuralNetwork { get; }
    public TSimulation Simulation { get; }

    public Genome(
        INeuralNetwork neuralNetwork,
        IEvolutionSettings<TSimulation> evolutionSettings)
    {
        _evolutionSettings = evolutionSettings;

        Simulation = evolutionSettings.SimulationFactoryMethod();
        NeuralNetwork = neuralNetwork;
    }

    public void Mutate()
    {
        foreach (var neuron in NeuralNetwork.Neurons)
        {
            MutateNeuron(neuron);
        }
    }

    private void MutateNeuron(Neuron neuron)
    {
        neuron.Bias = MutateNeuronValue(neuron.Bias);

        foreach (var dendrite in neuron.DendritesTowardsNextLayer)
        {
            dendrite.Weight = MutateNeuronValue(dendrite.Weight);
        }
    }

    private float MutateNeuronValue(float value)
    {
        value = _evolutionSettings.RandomnessProvider.NextFloat(0f, _evolutionSettings.MutationProbability) <= 5
            ? _evolutionSettings.RandomnessProvider.NextFloat(
                -_evolutionSettings.MutationStrength, 
                _evolutionSettings.MutationStrength)
            : value;
        return value;
    }

    public void Tick()
    {
        if (Simulation.HasEnded)
            return;

        var inputs = Simulation.GetInputs();
        var outputs = NeuralNetwork.Ask(inputs);
        Simulation.Tick(outputs);
    }

    public IGenome<TSimulation> Clone()
    {
        return new Genome<TSimulation>(
            NeuralNetwork.Clone(),
            _evolutionSettings);
    }

    public async ValueTask DisposeAsync()
    {
        if (Simulation is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }

    public void Dispose()
    {
        if (Simulation is IDisposable disposable)
            disposable.Dispose();
    }
}