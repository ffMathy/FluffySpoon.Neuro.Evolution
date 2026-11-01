using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;
using FluffySpoon.Neuro.Evolution.Sample.Models;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.Neuro.Evolution.Sample.Views;

public partial class MainWindow : Window
{
    /// <summary>
    /// A slow car can take minutes to finish a large map, which would hold up the next run if it was shown tick by tick.
    /// </summary>
    private static readonly TimeSpan MaximumTimeToWatchRun = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Leaves room for drawing the current run and for the UI, so that training doesn't lower the frame rate.
    /// </summary>
    private static readonly int TrainingThreadCount = Math.Max(1, Environment.ProcessorCount - 2);

    /// <summary>
    /// Shorter runs don't have enough frames to say anything reliable about the frame rate.
    /// </summary>
    private static readonly TimeSpan MinimumRunTimeToMeasureFrameRate = TimeSpan.FromSeconds(0.5);

    private readonly Stopwatch _drawnRunStopwatch = new();

    private readonly Random _random;
    private readonly MapGeneratorService _mapGeneratorService;
    private readonly FpsCounter _fpsCounter = new();
    private readonly GenerationsPerBatchController _generationsPerBatchController = new();
    private readonly AdaptiveMutationController _adaptiveMutationController = new();
    private readonly GenomeCountController _genomeCountController = new();
    private readonly EvolutionSettings<CarSimulation> _evolutionSettings;

    private bool _keepRunning;
    private bool _isTraining;
    private bool _isGenerationsTextValid;
    private int _generationsPerBatch = 1;
    private int _generationBeingDrawn = 1;
    private int _generationBeingTrained = 1;
    private int _framesInDrawnRun;

    private Map? _map;
    private IGeneration<CarSimulation> _currentGeneration;

    public MainWindow()
    {
        _random = new Random();
        _mapGeneratorService = new MapGeneratorService(_random);

        InitializeComponent();

        // A map has to fit the window, and the size of the window is not known until it has opened.
        Opened += (_, _) => Dispatcher.UIThread.Post(GenerateNewMap, DispatcherPriority.Background);

        this
            .GetObservable(WindowStateProperty)
            .Subscribe(KeepWindowMaximized);

        GenerationsTextBox
            .GetObservable(TextBox.TextProperty)
            .Subscribe(GenerationsTextBox_TextChanged);

        _evolutionSettings = new EvolutionSettings<CarSimulation>()
        {
            AmountOfGenomesInPopulation = GenomeCountController.InitialGenomeCount,
            AmountOfWorstGenomesToRemovePerGeneration = GenomeCountController.GetGenomesToRemove(GenomeCountController.InitialGenomeCount),
            NeuronCounts = new[] { 7, 6, 2 },
            MutationProbability = _adaptiveMutationController.MutationProbability,
            MutationStrength = _adaptiveMutationController.MutationStrength,
            RandomnessProvider = _random,
            SimulationFactoryMethod = () => new CarSimulation(_map ?? throw new Exception("No map has been generated yet.")),
            PostTickMethod = RenderGenomes
        };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFluffySpoonNeuroEvolution(_evolutionSettings);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _currentGeneration = serviceProvider.GetRequiredService<IGeneration<CarSimulation>>();

        SimulationView.FrameDrawn += () =>
        {
            _fpsCounter.AddFrame();
            _framesInDrawnRun++;
        };
        DispatcherTimer.Run(() =>
        {
            FpsText.Text = $"FPS: {_fpsCounter.ReadFramesPerSecond():0}";
            UpdateGenerationText();
            return true;
        }, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Minimizing is still allowed, since it only hides the window.
    /// </summary>
    private void KeepWindowMaximized(WindowState state)
    {
        if (state == WindowState.Normal)
            Dispatcher.UIThread.Post(() => WindowState = WindowState.Maximized);
    }

    /// <summary>
    /// Called on the drawing thread after every tick of the run that is drawn.
    /// </summary>
    private void RenderGenomes(Genomes<CarSimulation> genomes)
    {
        var isFastForwarding = _drawnRunStopwatch.Elapsed > MaximumTimeToWatchRun;
        if (isFastForwarding && !SimulationView.IsFrameDue)
            return;

        var cars = genomes.All
            .Select(genome => new CarDrawing(genome.Simulation, false))
            .Concat(genomes.Best.Select(genome => new CarDrawing(genome.Simulation, true)))
            .ToArray();

        if (isFastForwarding)
            SimulationView.DrawCarsWithoutWaiting(cars);
        else
            SimulationView.DrawCars(cars);
    }

    private void GenerateNewMapButton_Click(object sender, RoutedEventArgs e)
    {
        GenerateNewMap();
    }

    private void GenerateNewMap()
    {
        var (columns, rows) = SimulationView.MaximumMapSize;
        SetMap(_mapGeneratorService.GenerateRandomMap(columns, rows));

        // The next generation's simulations were already created on the previous map.
        // Cloning gives them new simulations on this map, while keeping what their networks have learned.
        foreach (var genome in _currentGeneration.Genomes.ToArray())
        {
            _currentGeneration.AddGenome(genome.Clone());
            _currentGeneration.RemoveGenome(genome);
        }
    }

    private async void TrainButton_Click(object sender, RoutedEventArgs e)
    {
        await TrainAsync();
    }

    /// <summary>
    /// Draws a copy of the current generation while training what comes after it at the same time,
    /// so that training isn't slowed down by drawing, and the next run is ready when the one being drawn has finished.
    /// Both happen on background threads so that the UI stays responsive.
    /// The buttons are disabled meanwhile, since only one training run can evolve the current generation,
    /// and since a new map would otherwise be picked up halfway through creating the next generation.
    /// The settings are read before each batch, so they can be changed while it keeps running.
    /// </summary>
    private async Task TrainAsync()
    {
        _isTraining = true;
        UpdateButtons();
        DisplayKeepAwake.Start();

        try
        {
            do
            {
                if (GenerateNewMapEachTimeCheckbox.IsChecked == true)
                    GenerateNewMap();

                var generationToDraw = _currentGeneration.Clone();
                _generationBeingDrawn = Volatile.Read(ref _generationBeingTrained);
                UpdateGenerationText();

                var generationsPerBatch = _generationsPerBatch;

                _framesInDrawnRun = 0;

                var drawing = Task.Run(() => DrawRun(generationToDraw));
                var training = Task.Run(() => TrainBatch(generationsPerBatch, drawing));
                await Task.WhenAll(drawing, training);

                UpdateGenerationsPerBatch();
                UpdateGenomeCount();
            } while (_keepRunning);
        }
        finally
        {
            DisplayKeepAwake.Stop();
            _isTraining = false;
            UpdateButtons();
        }
    }

    private void DrawRun(IGeneration<CarSimulation> generation)
    {
        _drawnRunStopwatch.Restart();
        generation.Simulate();
        _drawnRunStopwatch.Stop();
    }

    /// <summary>
    /// Stops early if the run being drawn has finished, since the next run shouldn't be kept waiting.
    /// </summary>
    private void TrainBatch(int generationsPerBatch, Task drawing)
    {
        for (var i = 0; i < generationsPerBatch; i++)
        {
            // Evolving empties the generation, so the genomes have to be held on to in order to read their fitness.
            var genomes = _currentGeneration.Genomes.ToArray();

            var stopwatch = Stopwatch.StartNew();
            _currentGeneration = _currentGeneration.EvolveInParallel(TrainingThreadCount);
            Interlocked.Increment(ref _generationBeingTrained);
            _generationsPerBatchController.AddRun(stopwatch.Elapsed);

            _adaptiveMutationController.AddGeneration(genomes.Min(genome => genome.Simulation.Fitness));
            ApplyMutationSettings();

            if (drawing.IsCompleted)
                return;
        }
    }

    private void ApplyMutationSettings()
    {
        _evolutionSettings.MutationProbability = _adaptiveMutationController.MutationProbability;
        _evolutionSettings.MutationStrength = _adaptiveMutationController.MutationStrength;
    }

    /// <summary>
    /// Only called between batches, since nothing is reading the settings then. The frame rate is measured
    /// from the drawn run alone, since waiting for training afterwards doesn't say anything about the cost of drawing.
    /// </summary>
    private void UpdateGenomeCount()
    {
        var runTime = _drawnRunStopwatch.Elapsed;
        if (runTime < MinimumRunTimeToMeasureFrameRate)
            return;

        var genomeCount = _genomeCountController.GetNextGenomeCount(_framesInDrawnRun / runTime.TotalSeconds);
        _evolutionSettings.AmountOfGenomesInPopulation = genomeCount;
        _evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration = GenomeCountController.GetGenomesToRemove(genomeCount);
    }

    private void UpdateGenerationText()
    {
        GenomeCountText.Text = $"Genomes per generation: {_evolutionSettings.AmountOfGenomesInPopulation}";
        MutationText.Text = $"Mutation: {_evolutionSettings.MutationProbability:P0} chance, {_evolutionSettings.MutationStrength:0.00} strength";
        GenerationText.Text = $"Generation: {_generationBeingDrawn} ({Volatile.Read(ref _generationBeingTrained)} being trained)";
    }

    private void UpdateGenerationsPerBatch()
    {
        var averageRunTime = _generationsPerBatchController.AverageRunTime;
        if (averageRunTime == null)
            return;

        AverageRunTimeText.Text = $"Average run time on this map: {averageRunTime.Value.TotalSeconds:0.000} s";
        GenerationsTextBox.Text = _generationsPerBatchController
            .GetNextGenerationsPerBatch(_generationsPerBatch, _drawnRunStopwatch.Elapsed)
            .ToString();
    }

    /// <summary>
    /// Invalid text keeps the last valid amount, which a running training session continues with.
    /// </summary>
    private void GenerationsTextBox_TextChanged(string? text)
    {
        _isGenerationsTextValid = int.TryParse(text, out var generations) && generations > 0;
        if (_isGenerationsTextValid)
            _generationsPerBatch = generations;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        TrainButton.IsEnabled = !_isTraining && _isGenerationsTextValid;
        GenerateNewMapButton.IsEnabled = !_isTraining;
    }

    private void SetMap(Map map)
    {
        _map = map;
        _generationsPerBatchController.ForgetRuns();
        _adaptiveMutationController.ForgetProgress();
        ApplyMutationSettings();
        SimulationView.SetMap(map);
    }

    private void KeepRunningCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        _keepRunning = true;
    }

    private void KeepRunningCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        _keepRunning = false;
    }
}
