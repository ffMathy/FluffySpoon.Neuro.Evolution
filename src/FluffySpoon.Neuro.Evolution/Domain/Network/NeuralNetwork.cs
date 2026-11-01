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
        return NeuralNetwork.CreateRandom(_settings);
    }
}

/// <summary>
/// A fully connected feed-forward neural network.
/// Hidden layers use tanh, the output layer uses a logistic sigmoid so that outputs are always in the range [0, 1].
/// </summary>
public class NeuralNetwork : INeuralNetwork
{
    private readonly Layer[] _layers;

    public IReadOnlyList<Layer> Layers => _layers;

    public Neuron[] Neurons => _layers
        .SelectMany(x => x.Neurons)
        .ToArray();

    private NeuralNetwork(Layer[] layers)
    {
        if (layers.Length < 2)
            throw new ArgumentException("A neural network needs at least an input and an output layer.", nameof(layers));

        _layers = layers;
    }

    public static NeuralNetwork CreateRandom(INeuralNetworkSettings settings)
    {
        var random = settings.RandomnessProvider ?? throw new ArgumentException("A randomness provider is required.", nameof(settings));
        var neuronCounts = settings.NeuronCounts ?? throw new ArgumentException("Neuron counts are required.", nameof(settings));

        var layers = neuronCounts
            .Select(count =>
            {
                var layer = new Layer();
                for (var i = 0; i < count; i++)
                    layer.Neurons.Add(new Neuron(layer) { Bias = random.NextFloat(-1f, 1f) });

                return layer;
            })
            .ToArray();

        ConnectLayers(layers, (layerIndex, _, _) =>
        {
            // Xavier/Glorot-style initialization keeps the activations away from saturation.
            var fanIn = layers[layerIndex - 1].Neurons.Count;
            var limit = (float)Math.Sqrt(6d / (fanIn + 1));
            return random.NextFloat(-limit, limit);
        });

        return new NeuralNetwork(layers);
    }

    /// <summary>
    /// Wires up every neuron in a layer with every neuron in the next layer.
    /// The dendrite at index <c>j</c> of a neuron always points to neuron <c>j</c> of the next layer.
    /// </summary>
    /// <param name="getWeight">Called with (index of the destination layer, source neuron index, destination neuron index).</param>
    private static void ConnectLayers(Layer[] layers, Func<int, int, int, float> getWeight)
    {
        for (var layerIndex = 1; layerIndex < layers.Length; layerIndex++)
        {
            var previousLayer = layers[layerIndex - 1];
            var currentLayer = layers[layerIndex];

            previousLayer.Next = currentLayer;
            currentLayer.Previous = previousLayer;

            for (var previousIndex = 0; previousIndex < previousLayer.Neurons.Count; previousIndex++)
            {
                var source = previousLayer.Neurons[previousIndex];
                source.DendritesTowardsNextLayer.Clear();

                for (var currentIndex = 0; currentIndex < currentLayer.Neurons.Count; currentIndex++)
                {
                    source.DendritesTowardsNextLayer.Add(
                        new Dendrite(source, currentLayer.Neurons[currentIndex])
                        {
                            Weight = getWeight(layerIndex, previousIndex, currentIndex)
                        });
                }
            }
        }
    }

    public float[] Ask(float[] inputs)
    {
        var inputLayer = _layers[0];
        if (inputs.Length != inputLayer.Neurons.Count)
            throw new ArgumentException(
                $"Input size ({inputs.Length}) does not match the number of neurons in the input layer ({inputLayer.Neurons.Count}).",
                nameof(inputs));

        var activations = (float[])inputs.Clone();

        for (var layerIndex = 1; layerIndex < _layers.Length; layerIndex++)
        {
            var previousLayer = _layers[layerIndex - 1];
            var currentLayer = _layers[layerIndex];
            var isOutputLayer = layerIndex == _layers.Length - 1;

            var nextActivations = new float[currentLayer.Neurons.Count];
            for (var currentIndex = 0; currentIndex < nextActivations.Length; currentIndex++)
            {
                var sum = currentLayer.Neurons[currentIndex].Bias;
                for (var previousIndex = 0; previousIndex < activations.Length; previousIndex++)
                {
                    sum += previousLayer.Neurons[previousIndex].DendritesTowardsNextLayer[currentIndex].Weight *
                           activations[previousIndex];
                }

                nextActivations[currentIndex] = isOutputLayer ? Sigmoid(sum) : Tanh(sum);
            }

            activations = nextActivations;
        }

        return activations;
    }

    private static float Tanh(float value) => (float)Math.Tanh(value);

    private static float Sigmoid(float value) => (float)(1d / (1d + Math.Exp(-value)));

    public INeuralNetwork Clone()
    {
        var clonedLayers = _layers
            .Select(layer =>
            {
                var clonedLayer = new Layer();
                foreach (var neuron in layer.Neurons)
                    clonedLayer.Neurons.Add(new Neuron(clonedLayer) { Bias = neuron.Bias });

                return clonedLayer;
            })
            .ToArray();

        ConnectLayers(clonedLayers, (layerIndex, previousIndex, currentIndex) =>
            _layers[layerIndex - 1].Neurons[previousIndex].DendritesTowardsNextLayer[currentIndex].Weight);

        return new NeuralNetwork(clonedLayers);
    }
}
