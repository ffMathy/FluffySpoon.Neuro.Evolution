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
        for (var layerIndex = 0; layerIndex < settings.NeuronCounts.Length; layerIndex++)
        {
            var layer = new Layer();
            for(var neuronIndex = 0; neuronIndex < settings.NeuronCounts[layerIndex]; neuronIndex++)
            {
                var neuron = new Neuron();
                neuron.Bias = _settings.RandomnessProvider.NextFloat(-0.5f, 0.5f);
                
                layer.Neurons.Add(neuron);
            }
            
            layers.Add(layer);
        }
        

        _neurons = settings.NeuronCounts
            .Select(count => new Layer()
            {
                Neurons = InitNeurons(count)
            })
            .Select(InitNeurons)
            .ToArray();
        InitWeights();
    }

    private INeuron[] InitNeurons(int count)
    {
        return Enumerable.Range(1, count)
            .Select(_ => new Neuron()
            {
                Bias = _settings.RandomnessProvider.NextFloat(-0.5f, 0.5f)
            })
            .ToArray();
    }

    public float[] Ask(float[] inputs)
    {
        var neuronMesh = CloneNeurons();
        for (var i = 0; i < inputs.Length; i++)
        {
            neuronMesh[0][i].Bias = inputs[i];
        }

        for (var currentLayer = 1; currentLayer < neuronMesh.Length; currentLayer++)
        {
            var priorLayer = currentLayer - 1;
            for (var j = 0; j < neuronMesh[currentLayer].Length; j++)
            {
                var value = 0f;
                for (var k = 0; k < neuronMesh[priorLayer].Length; k++)
                {
                    value += neuronMesh[priorLayer][j].Weights[k] * neuronMesh[priorLayer][k].Bias;
                }

                neuronMesh[currentLayer][j].Bias = Activate(value + neuronMesh[currentLayer][j].Bias);
            }
        }

        return neuronMesh[^1]
            .Select(x => x.Bias)
            .ToArray();
    }

    private INeuron[][] CloneNeurons()
    {
        return _neurons
            .Select(x => x
                .Select(y => y.Clone())
                .ToArray())
            .ToArray();
    }

    private void InitWeights()
    {
        for (var currentLayer = 1; currentLayer < _neurons.Length; currentLayer++)
        {
            var neuronsInPreviousLayer = _neurons[currentLayer - 1];
            var neuronsInCurrentLayer = _neurons[currentLayer];
            for (var neuronIndex = 0; neuronIndex < neuronsInCurrentLayer.Length; neuronIndex++)
            {
                var weights = new List<float>();
                for (var k = 0; k < neuronsInPreviousLayer.Length; k++)
                {
                    weights.Add(_settings.RandomnessProvider.NextFloat(-0.5f, 0.5f));
                }

                neuronsInPreviousLayer[neuronIndex].Weights = weights.ToArray();
                neuronsInCurrentLayer[neuronIndex].Weights = Array.Empty<float>();
            }
        }
    }

    private static float Activate(float value)
    {
        return (float)Math.Tanh(value);
    }

    public INeuralNetwork Clone()
    {
        var neuralNetwork = new NeuralNetwork(_settings);
        neuralNetwork._neurons = CloneNeurons();

        return neuralNetwork;
    }
}