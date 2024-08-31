using System;
using System.Collections.Generic;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Infrastructure.Extensions;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

namespace FluffySpoon.Neuro.Evolution.Domain.Network;

public class NeuralNetworkFactory : INeuralNetworkFactory
{
    private readonly INeuralNetworkSettings _settings;

    public NeuralNetworkFactory(
        INeuralNetworkSettings settings)
    {
        _settings = settings;
    }

    public INeuralNetwork Create()
    {
        return new NeuralNetwork(_settings);
    }
}

public class NeuralNetwork : INeuralNetwork
{
    private readonly INeuralNetworkSettings _settings;

    private Layer[] _layers;

    public Neuron[] Neurons => _layers
        .SelectMany(x => x.Neurons)
        .ToArray();

    public NeuralNetwork(
        INeuralNetworkSettings settings)
    {
        _settings = settings;

        var layers = new List<Layer>();
        foreach (var layerNeuronCount in settings.NeuronCounts)
        {
            var layer = new Layer();
            for (var neuronIndex = 0; neuronIndex < layerNeuronCount; neuronIndex++)
            {
                var neuron = new Neuron(layer)
                {
                    Bias = _settings.RandomnessProvider.NextFloat(-0.01f, 0.01f),
                };

                layer.Neurons.Add(neuron);
            }

            layers.Add(layer);
        }

        for (var layerIndex = 1; layerIndex < settings.NeuronCounts.Length; layerIndex++)
        {
            var currentLayer = layers[layerIndex];
            var previousLayer = layers[layerIndex - 1];

            currentLayer.Previous = previousLayer;
            previousLayer.Next = currentLayer;

            foreach (var previousLayerNeuron in previousLayer.Neurons)
            {
                foreach (var currentLayerNeuron in currentLayer.Neurons)
                {
                    var dendrite = new Dendrite(previousLayerNeuron, currentLayerNeuron)
                    {
                        Weight = _settings.RandomnessProvider.NextFloat(-0.5f, 0.5f),
                    };
                    previousLayerNeuron.DendritesTowardsNextLayer.Add(dendrite);
                }
            }
        }

        _layers = layers.ToArray();
    }

    public float[] Ask(float[] inputs)
    {
        var clonedLayers = CloneLayers();

        var layerComputationStates = clonedLayers
            .Select(layer => layer.Neurons
                .Select(neuron => new NeuronComputation(neuron))
                .ToArray())
            .ToArray();

        // Set activations of the first layer based on inputs
        var firstLayerComputationStates = layerComputationStates.First();
        if (inputs.Length != firstLayerComputationStates.Length)
            throw new ArgumentException("Input size does not match the number of neurons in the input layer.");

        for (var index = 0; index < firstLayerComputationStates.Length; index++)
        {
            firstLayerComputationStates[index].Activation = inputs[index];
        }

        for (var currentLayerIndex = 1; currentLayerIndex < layerComputationStates.Length; currentLayerIndex++)
        {
            var currentLayerComputationStates = layerComputationStates[currentLayerIndex];
            var previousLayerComputationStates = layerComputationStates[currentLayerIndex - 1];

            for (var currentLayerNeuronIndex = 0;
                 currentLayerNeuronIndex < currentLayerComputationStates.Length;
                 currentLayerNeuronIndex++)
            {
                if (previousLayerComputationStates.Any(p =>
                        p.Neuron.DendritesTowardsNextLayer.Count <= currentLayerNeuronIndex))
                {
                    throw new IndexOutOfRangeException(
                        $"Mismatch in dendrite counts. Expected dendrites towards next layer to have at least {currentLayerNeuronIndex + 1} elements.");
                }

                var sum = previousLayerComputationStates
                    .Select((previousNeuronComputation, previousNeuronIndex) =>
                    {
                        var weight = previousNeuronComputation.Neuron.DendritesTowardsNextLayer[currentLayerNeuronIndex]
                            .Weight;
                        return weight * previousNeuronComputation.Activation;
                    })
                    .Sum();

                var bias = currentLayerComputationStates[currentLayerNeuronIndex].Neuron.Bias;
                currentLayerComputationStates[currentLayerNeuronIndex].Activation = Activate(sum + bias);
            }
        }

        return layerComputationStates[^1]
            .Select(state => state.Activation)
            .ToArray();
    }

    private Layer[] CloneLayers()
    {
        var clonedLayers = new List<Layer>();
        foreach (var layer in _layers)
        {
            var clonedLayer = new Layer();
            foreach (var neuron in layer.Neurons)
            {
                clonedLayer.Neurons.Add(new Neuron(layer)
                {
                    Bias = neuron.Bias,
                });
            }

            clonedLayers.Add(clonedLayer);
        }

        for (var layerIndex = 1; layerIndex < clonedLayers.Count; layerIndex++)
        {
            var currentLayer = _layers[layerIndex];
            var previousLayer = _layers[layerIndex - 1];
            
            var currentLayerClone = clonedLayers[layerIndex];
            var previousLayerClone = clonedLayers[layerIndex - 1];

            currentLayerClone.Previous = previousLayerClone;
            previousLayerClone.Next = currentLayerClone;

            for (var previousLayerNeuronIndex = 0; previousLayerNeuronIndex < previousLayer.Neurons.Count; previousLayerNeuronIndex++)
            {
                var previousLayerNeuron = previousLayer.Neurons[previousLayerNeuronIndex];
                var previousLayerNeuronClone = previousLayerClone.Neurons[previousLayerNeuronIndex];
                
                for (var currentLayerNeuronIndex = 0; currentLayerNeuronIndex < currentLayer.Neurons.Count; currentLayerNeuronIndex++)
                {
                    var currentLayerNeuronClone = currentLayerClone.Neurons[currentLayerNeuronIndex];
                    
                    foreach (var dendriteToNextLayer in previousLayerNeuron.DendritesTowardsNextLayer)
                    {
                        var clonedDendrite = new Dendrite(previousLayerNeuronClone, currentLayerNeuronClone)
                        {
                            Weight = dendriteToNextLayer.Weight
                        };
                        previousLayerNeuronClone.DendritesTowardsNextLayer.Add(clonedDendrite);
                    }
                }
            }
        }

        return clonedLayers.ToArray();
    }

    private static float Activate(float value)
    {
        return (float)Math.Tanh(value);
    }

    public INeuralNetwork Clone()
    {
        var neuralNetwork = new NeuralNetwork(_settings);
        neuralNetwork._layers = CloneLayers();

        return neuralNetwork;
    }
}