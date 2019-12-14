using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFluffySpoonNeuroEvolution(new EvolutionSettings<CarSimulation>() {
                AmountOfGenomesInPopulation = 100,
                AmountOfWorstGenomesToRemovePerGeneration = 10,

                //3 input neurons, 4 neurons in 1st hidden layer, 5 neurons in 2nd hidden layer, 2 output neurons
                NeuronCounts = new [] { 3, 4, 5, 2 },
                
                SimulationFactoryMethod = () => new CarSimulation()
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var firstGeneration = serviceProvider.GetRequiredService<IGeneration<CarSimulation>>();
            var secondGeneration = await firstGeneration.EvolveAsync();
            var thirdGeneration = await secondGeneration.EvolveAsync();
            //etc

            var carSimulationsOfThirdGeneration = thirdGeneration.Genomes.Select(x => x.Simulation);
            foreach(var carSimulationOfThirdGeneration in carSimulationsOfThirdGeneration)
            {
                //render the car simulation or something else.
            }
        }
    }

    class CarSimulation : ISimulation
    {
        enum SteeringType
        {
            TurnLeft,
            DoNothing,
            TurnRight
        }

        enum AccelerationType
        {
            Accelerate,
            DoNothing,
            Decelerate
        }

        public int DistanceTravelled { get; private set; }

        public double Fitness => -DistanceTravelled;

        public bool HasEnded => DistanceTravelled > 10;

        /// <summary>
        /// This is called by the learning algorithm to determine the inputs to feed into the neural network.
        /// </summary>
        /// <returns></returns>
        public async Task<double[]> GetInputsAsync()
        { 
            //we simulate that 3 LIDAR sensor readings come in. 
            //the left one reads 100 meters, the middle one reads 80 meters and the right one reads 30 meters. 
            //these will be fed as input neurons.

            return new double[]
            {
                100, //left sensor input neuron
                80, //center sensor input neuron
                30 //right sensor input neuron
            };
        }

        /// <summary>
        /// This is called by the learning algorithm with the neural network's suggestion on what to do based on the input that was given in <see cref="GetInputsAsync"/>.
        /// </summary>
        public async Task TickAsync(double[] outputs)
        {
            var steeringType = NeuronInterpretationHelper.InterpretAsEnum<SteeringType>(outputs[0]);
            var accelerationType = NeuronInterpretationHelper.InterpretAsEnum<AccelerationType>(outputs[1]);

            //do something here based on the steering type and acceleration type the neural net suggested.

            if(accelerationType == AccelerationType.Accelerate)
                DistanceTravelled++;
        }

        public async Task ResetAsync()
        {
            //reset car positions.
        }
    }
}
