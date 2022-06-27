using System;
using Godot;
using HarmonyLib;

public class AutoEvoExploringTool : ControlWithInput
{
    private GameProperties? gameProperties;
    private AutoEvoConfiguration autoEvoConfiguration;

    [Signal]
    public delegate void OnAutoEvoExploringToolClosed();

    public void OpenFromMainMenu()
    {
        if (Visible)
            return;

        Init();

        Show();
    }

    private void Init()
    {
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        autoEvoConfiguration = (AutoEvoConfiguration)SimulationParameters.Instance.AutoEvoConfiguration.Clone();
    }

    [RunOnKeyDown("ui_cancel")]
    private void OnBackButtonPressed()
    {
        if (!Visible)
            return;

        EmitSignal(nameof(OnAutoEvoExploringToolClosed));
    }
}
