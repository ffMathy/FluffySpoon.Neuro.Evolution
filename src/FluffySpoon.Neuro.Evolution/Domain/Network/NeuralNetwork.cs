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
            for(var neuronIndex = 0; neuronIndex < layerNeuronCount; neuronIndex++)
            {
                var neuron = new Neuron(layer)
                {
                    Bias = _settings.RandomnessProvider.NextFloat(-0.5f, 0.5f),
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
        
        var firstLayer = clonedLayers.First();
        for (var index = 0; index < firstLayer.Neurons.Count; index++)
        {
            var neuron = firstLayer.Neurons[index];
            neuron.Bias = inputs[index];
        }

        for (var currentLayerIndex = 1; currentLayerIndex < clonedLayers.Length; currentLayerIndex++)
        {
            var currentLayerClone = clonedLayers[currentLayerIndex];
            
            var previousLayerIndex = currentLayerIndex - 1;
            var previousLayerClone = clonedLayers[previousLayerIndex];

            for (var currentLayerNeuronIndex = 0; currentLayerNeuronIndex < currentLayerClone.Neurons.Count; currentLayerNeuronIndex++)
            {
                var sum = previousLayerClone.Neurons
                    .Select((previousLayerNeuron, previousLayerNeuronIndex) =>
                    {
                        var dendriteWeightFromPreviousLayer = previousLayerClone.Neurons[previousLayerNeuronIndex]
                            .DendritesTowardsNextLayer[currentLayerNeuronIndex]
                            .Weight;
                        return dendriteWeightFromPreviousLayer * previousLayerNeuron.Bias;
                    })
                    .Sum();

                currentLayerClone.Neurons[currentLayerNeuronIndex].Bias = Activate(sum + currentLayerClone.Neurons[currentLayerNeuronIndex].Bias);
            }
        }

        return clonedLayers[^1].Neurons
            .Select(x => x.Bias)
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
                    Bias = neuron.Bias
                });
            }

            clonedLayers.Add(clonedLayer);
        }
        
        for (var layerIndex = 1; layerIndex < clonedLayers.Count; layerIndex++)
        {
            var currentLayer = clonedLayers[layerIndex];
            var previousLayer = clonedLayers[layerIndex - 1];

            currentLayer.Previous = previousLayer;
            previousLayer.Next = currentLayer;
            
            foreach (var previousLayerNeuron in previousLayer.Neurons)
            {
                foreach (var currentLayerNeuron in currentLayer.Neurons)
                {
                    foreach (var dendriteToNextLayer in previousLayerNeuron.DendritesTowardsNextLayer)
                    {
                        var clonedDendrite = new Dendrite(previousLayerNeuron, currentLayerNeuron)
                        {
                            Weight = dendriteToNextLayer.Weight
                        };
                        previousLayerNeuron.DendritesTowardsNextLayer.Add(clonedDendrite);
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