using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Game-process entry point for Auto-Evo. Emits one JSON result line, then exits with a status code.
/// </summary>
public partial class AutoEvoCommandLine : Node
{
    private AutoEvoSimulation? simulation;
    private AutoEvoCommandLineOptions? options;
    private long? actualSeed;
    private volatile bool cancelRequested;
    private bool reported;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        Console.CancelKeyPress += OnCancelKeyPress;

        // Autoloads finished successfully. Argument or workload failures are not game startup crashes.
        SafeModeStartupHandler.ReportGameStartSuccessful();

        try
        {
            options = AutoEvoCommandLineOptions.Parse(OS.GetCmdlineUserArgs());
            var settings = new WorldGenerationSettings();
            if (options.Seed.HasValue)
                settings.Seed = options.Seed.Value;

            actualSeed = settings.Seed;
            simulation = new AutoEvoSimulation(GameProperties.StartNewMicrobeGame(settings).GameWorld,
                options.Generations);
        }
        catch (Exception e)
        {
            ReportAndQuit(e.ToString());
        }
    }

    public override void _ExitTree()
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
    }

    public override void _Process(double delta)
    {
        if (reported || simulation == null)
            return;

        if (cancelRequested)
            simulation.Cancel();

        simulation.Process();
        if (simulation.Finished)
            ReportAndQuit(simulation.Error);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            cancelRequested = true;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true;
        cancelRequested = true;
        simulation?.Cancel();
    }

    private void ReportAndQuit(string? error)
    {
        if (reported)
            return;

        reported = true;
        if (cancelRequested)
            error ??= "Auto-Evo cancelled";

        var success = error == null && simulation?.WasSuccessful == true;

        try
        {
            GD.Print("AUTO_EVO_RESULT " + JsonConvert.SerializeObject(new
            {
                Success = success,
                RequestedGenerations = options?.Generations,
                CompletedGenerations = simulation?.CompletedGenerations ?? 0,
                Seed = actualSeed,
                simulation?.Generations,
                Error = error,
            }));
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to write Auto-Evo results: ", e);
            success = false;
        }

        GetTree().Quit(success ? 0 : 1);
    }
}
