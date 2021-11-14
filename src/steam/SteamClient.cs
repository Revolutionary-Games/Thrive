using System;
using Godot;

/// <summary>
///   Concrete implementation of the Steam integration handling. Only compiled for the Steam version of the game.
/// </summary>
public class SteamClient : ISteamClient
{
    private bool initStarted;

    public bool IsLoaded { get; private set; }
    public string LoadError { get; private set; }
    public string ExtraErrorInfo { get; private set; }

    public bool? IsOnline { get; private set; }
    public ulong? SteamId { get; private set; }
    public bool? IsOwned { get; private set; }

    public string DisplayName => Steam.GetPersonaName();

    public void Init()
    {
        if (initStarted)
            throw new InvalidOperationException("Can't initialize steam more than once");

        initStarted = true;
        GD.Print("Starting Steam load");

        var stats = Steam.SteamInit(true);

        if (!stats.Contains("status") || stats["status"] as int? != 1)
        {
            GD.Print("Steam load failed with code:", stats["status"]);
            GD.PrintErr("Verbal status: ", stats["verbal"]);
            SetError(TranslationServer.Translate("STEAM_CLIENT_INIT_FAILED"), stats["verbal"] as string);
            return;
        }

        GD.Print("Steam load finished");
        IsLoaded = true;

        RefreshCurrentUserInfo();

        if (IsOwned == true)
            GD.Print("Game is owned by current Steam user");
    }

    public void ConnectSignals(Godot.Object receiver)
    {
        // Signal documentation: https://gramps.github.io/GodotSteam/signals-modules.html
        Steam.Singleton.Connect("steamworks_error", receiver, nameof(ISteamSignalReceiver.GenericSteamworksError));
        Steam.Singleton.Connect("overlay_toggled", receiver, nameof(ISteamSignalReceiver.OverlayStatusChanged));

        Steam.Singleton.Connect("current_stats_received", receiver,
            nameof(ISteamSignalReceiver.CurrentUserStatsReceived));
        Steam.Singleton.Connect("user_stats_received", receiver, nameof(ISteamSignalReceiver.UserStatsReceived));
        Steam.Singleton.Connect("user_stats_stored", receiver, nameof(ISteamSignalReceiver.UserStatsStored));

        Steam.Singleton.Connect("low_power", receiver, nameof(ISteamSignalReceiver.LowPower));
        Steam.Singleton.Connect("steam_api_call_completed", receiver, nameof(ISteamSignalReceiver.APICallComplete));
        Steam.Singleton.Connect("steam_shutdown", receiver, nameof(ISteamSignalReceiver.ShutdownRequested));
    }

    public void Process(float delta)
    {
        if (!IsLoaded)
            return;

        Steam.RunCallbacks();
    }

    public void GenericSteamworksError(string failedSignal, string message)
    {
        GD.PrintErr("Steamworks error ", failedSignal, ": ", message);
    }

    public void OverlayStatusChanged(bool active)
    {
    }

    public void CurrentUserStatsReceived(int game, int result, int user)
    {
        if (result == Steam.ResultOk)
        {
            GD.Print("Received stats for current user");
        }
        else if (result == Steam.ResultFail)
        {
            GD.Print("Failed to receive stats for current user");
        }
        else
        {
            GD.PrintErr("Unknown result for CurrentUserStatsReceived: ", result);
        }
    }

    public void UserStatsReceived(int game, int result, int user)
    {
    }

    public void UserStatsStored(int game, int result)
    {
    }

    public void LowPower(int power)
    {
    }

    public void APICallComplete(int asyncCall, int callback, int parameter)
    {
    }

    public void ShutdownRequested()
    {
    }

    private void RefreshCurrentUserInfo()
    {
        IsOnline = Steam.LoggedOn();
        SteamId = Steam.GetSteamID();
        IsOwned = Steam.IsSubscribed();
    }

    private void SetError(string error, string extraDescription = null)
    {
        LoadError = error;
        ExtraErrorInfo = extraDescription;
    }
}
