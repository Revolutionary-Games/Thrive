using System;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
///   Handles communicating between the general codebase and the Steam sdk requiring code.
///   This ensures that non Steam versions of the game can be built with normal Godot tools.
///   If Steam library is not loaded most methods in here do nothing to allow easily calling them without checking
///   first.
/// </summary>
public class SteamHandler : Node, ISteamSignalReceiver
{
    private static SteamHandler instance;

    private bool wePaused;

    private ISteamClient steamClient;

    public SteamHandler()
    {
        instance = this;
    }

    public static SteamHandler Instance => instance;

    /// <summary>
    ///   True if Steam has been properly loaded
    /// </summary>
    public bool IsLoaded => steamClient is { IsLoaded: true };

    /// <summary>
    ///   True if Steam loading was attempted and error messages can be retrieved
    /// </summary>
    public bool WasLoadAttempted { get; private set; }

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;

        OnSteamInit();
    }

    public override void _Process(float delta)
    {
        steamClient?.Process(delta);
    }

    /// <summary>
    ///   Should only be called by the Steam handling library when its loaded
    /// </summary>
    /// <param name="client">The Steam handler to register</param>
    /// <exception cref="ArgumentException">If client is invalid</exception>
    /// <exception cref="InvalidOperationException">If a client has already registered</exception>
    public void RegisterSteamClient(ISteamClient client)
    {
        if (steamClient != null)
            throw new InvalidOperationException("Steam client interface object already registered");

        steamClient = client ?? throw new ArgumentException("client can't be null");
    }

    public void GenericSteamworksError(string failedSignal, string message)
    {
        // TODO: show a popup panel with the error

        steamClient?.GenericSteamworksError(failedSignal, message);
    }

    /// <summary>
    ///   Pauses the game while the overlay is active
    /// </summary>
    /// <param name="active">True if overlay active</param>
    public void OverlayStatusChanged(bool active)
    {
        if (active && !GetTree().Paused)
        {
            wePaused = true;
            GetTree().Paused = true;
        }
        else if (!active && GetTree().Paused && wePaused)
        {
            wePaused = false;
            GetTree().Paused = false;
        }

        steamClient?.OverlayStatusChanged(active);
    }

    private void OnSteamInit()
    {
        if (steamClient == null)
            CreateSteamClientIfClassPresent();

        if (steamClient != null)
        {
            WasLoadAttempted = true;
            steamClient.Init();

            if (IsLoaded)
                steamClient.ConnectSignals(this);
        }
    }

    private void CreateSteamClientIfClassPresent()
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.GetExecutingAssembly();
        }
        catch (Exception e)
        {
            GD.PrintErr("Could not get executing assembly due to: ", e, " can't load Steam library");
            return;
        }

        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == "SteamClient");

        if (type == null)
        {
            GD.Print("No SteamClient class found, not initializing Steam");
            return;
        }

        RegisterSteamClient((ISteamClient)Activator.CreateInstance(type));
    }
}
