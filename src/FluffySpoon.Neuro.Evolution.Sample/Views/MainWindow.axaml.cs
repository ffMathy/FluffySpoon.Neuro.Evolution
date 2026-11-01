using System;
using System.Diagnostics;
using System.Linq;
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
    /// A slow car can take minutes to finish a large map, which would hold up the training if it was shown tick by tick.
    /// </summary>
    private static readonly TimeSpan MaximumTimeToWatchRun = TimeSpan.FromSeconds(15);

    private readonly Stopwatch _drawnRunStopwatch = new();

    private readonly Random _random;
    private readonly MapGeneratorService _mapGeneratorService;
    private readonly FpsCounter _fpsCounter = new();
    private readonly GenerationsPerBatchController _generationsPerBatchController = new();

    private bool _keepRunning;
    private bool _isTraining;
    private bool _isGenerationsTextValid;
    private int _generationsPerBatch = 1;

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

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFluffySpoonNeuroEvolution(
            new EvolutionSettings<CarSimulation>()
            {
                AmountOfGenomesInPopulation = 50,
                AmountOfWorstGenomesToRemovePerGeneration = 40,
                NeuronCounts = new[] { 7, 6, 2 },
                MutationProbability = 0.15f,
                MutationStrength = 0.5f,
                RandomnessProvider = _random,
                SimulationFactoryMethod = () => new CarSimulation(_map ?? throw new Exception("No map has been generated yet.")),
                PostTickMethod = RenderGenomes
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _currentGeneration = serviceProvider.GetRequiredService<IGeneration<CarSimulation>>();

        SimulationView.FrameDrawn += _fpsCounter.AddFrame;
        DispatcherTimer.Run(() =>
        {
            FpsText.Text = $"FPS: {_fpsCounter.ReadFramesPerSecond():0}";            return true;
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
    /// Called on the training thread after every tick of a generation that is drawn.
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
    /// Trains on a background thread so that the UI stays responsive, drawing only the last generation of each batch.
    /// The other generations aren't slowed down by drawing, and are the ones that decide how many generations the next batch has.
    /// The buttons are disabled meanwhile, since only one training run can evolve the current generation,
    /// and since a new map would otherwise be picked up halfway through creating the next generation.
    /// The settings are read before each batch, so they can be changed while it keeps running.
    /// </summary>
    private async Task TrainAsync()
    {
        _isTraining = true;
        UpdateButtons();

        try
        {
            do
            {
                if (GenerateNewMapEachTimeCheckbox.IsChecked == true)
                    GenerateNewMap();

                var generationsPerBatch = _generationsPerBatch;
                await Task.Run(() => TrainBatch(generationsPerBatch));

                UpdateGenerationsPerBatch();
            } while (_keepRunning);
        }
        finally
        {
            _isTraining = false;
            UpdateButtons();
        }
    }

    private void TrainBatch(int generationsPerBatch)
    {
        for (var i = 0; i < generationsPerBatch - 1; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            _currentGeneration = _currentGeneration.EvolveInParallel();
            _generationsPerBatchController.AddRun(stopwatch.Elapsed);
        }

        _drawnRunStopwatch.Restart();
        _currentGeneration = _currentGeneration.Evolve();

        // Drawing makes this run far slower than the others, so it only counts when there is nothing better.
        if (_generationsPerBatchController.AverageRunTime == null)
            _generationsPerBatchController.AddRun(_drawnRunStopwatch.Elapsed);
    }

    private void UpdateGenerationsPerBatch()
    {
        var averageRunTime = _generationsPerBatchController.AverageRunTime;
        if (averageRunTime == null)
            return;

        AverageRunTimeText.Text = $"Average run time on this map: {averageRunTime.Value.TotalSeconds:0.000} s";
        GenerationsTextBox.Text = _generationsPerBatchController
            .GetNextGenerationsPerBatch(_generationsPerBatch)
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
