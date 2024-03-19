using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
///   Handles communicating between the general codebase and the Steam sdk requiring code.
///   This ensures that non Steam versions of the game can be built with normal Godot tools.
///   If Steam library is not loaded most methods in here do nothing to allow easily calling them without checking
///   first.
/// </summary>
[GodotAutoload]
public partial class SteamHandler : Node, ISteamSignalReceiver
{
    /// <summary>
    ///   All valid tags. Need to be the same as: https://partner.steamgames.com/apps/workshoptags/1779200
    /// </summary>
    public static readonly string[] ValidTags = { "graphics", "gameplay", "microbe" };

    public static readonly string[] RecommendedFileEndings = { ".jpg", ".png", ".gif" };

    private static SteamHandler? instance;

    private bool wePaused;

    private ISteamClient? steamClient;

    public SteamHandler()
    {
        instance = this;
    }

    public static SteamHandler Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   All the valid tags for Thrive's Steam workshop items
    /// </summary>
    public static IEnumerable<string> Tags => ValidTags;

    /// <summary>
    ///   True if Steam has been properly loaded
    /// </summary>
    public bool IsLoaded => steamClient is { IsLoaded: true };

    /// <summary>
    ///   True if Steam loading was attempted and error messages can be retrieved
    /// </summary>
    public bool WasLoadAttempted { get; private set; }

    public string DisplayName
    {
        get
        {
            ThrowIfNotLoaded();
            return steamClient!.DisplayName;
        }
    }

    /// <summary>
    ///   Checks if current running code is an exported Thrive version for Steam
    /// </summary>
    /// <returns>Returns true if this is the Steam export</returns>
    /// <remarks>
    ///   <para>
    ///     This is different from detecting if Steam features are loaded, as correct license information must be
    ///     displayed even if Steam loading fails, for example.
    ///   </para>
    /// </remarks>
    public static bool IsTaggedSteamRelease()
    {
        return OS.HasFeature("steam");
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        OnSteamInit();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        steamClient?.Dispose();
        steamClient = null;
    }

    public override void _Process(double delta)
    {
        steamClient?.Process(delta);
    }

    /// <summary>
    ///   Starts workshop item creation
    /// </summary>
    /// <param name="callback">Callback to be called when it is ready</param>
    public void CreateWorkshopItem(Action<WorkshopResult> callback)
    {
        ThrowIfNotLoaded();

        steamClient!.CreateWorkshopItem(callback);
    }

    /// <summary>
    ///   Updates a workshop item
    /// </summary>
    /// <param name="item">The item information to update</param>
    /// <param name="changeNotes">Optional change notes to set</param>
    /// <param name="callback">Callback to be called when everything has been uploaded or failed</param>
    /// <exception cref="ArgumentException">If something is wrong with the given data</exception>
    public void UpdateWorkshopItem(WorkshopItemData item, string? changeNotes, Action<WorkshopResult> callback)
    {
        ThrowIfNotLoaded();

        var handle = steamClient!.StartWorkshopItemUpdate(item.Id);

        GD.Print("Using workshop update handle: ", handle);

        if (!steamClient.SetWorkshopItemTitle(handle, item.Title))
            throw new ArgumentException("Invalid title");

        if (!steamClient.SetWorkshopItemDescription(handle, item.Description))
            throw new ArgumentException("Description is invalid");

        if (!steamClient.SetWorkshopItemVisibility(handle, item.Visibility))
            throw new ArgumentException("Visibility is invalid");

        if (!steamClient.SetWorkshopItemContentFolder(handle, item.ContentFolder))
            throw new ArgumentException("Content folder is invalid");

        if (!steamClient.SetWorkshopItemPreview(handle, item.PreviewImagePath))
            throw new ArgumentException("Preview image is invalid");

        if (!steamClient.SetWorkshopItemTags(handle, item.Tags))
            throw new ArgumentException("Item tags are invalid");

        steamClient.SubmitWorkshopItemUpdate(handle, changeNotes, callback);
    }

    /// <summary>
    ///   Returns the folders of installed workshop items
    /// </summary>
    /// <returns>List of folders</returns>
    public List<string> GetWorkshopItemFolders()
    {
        ThrowIfNotLoaded();

        return steamClient!.GetInstalledWorkshopItemFolders();
    }

    /// <summary>
    ///   Opens a workshop item in the Steam in-game browser overlay
    /// </summary>
    public void OpenWorkshopItemInOverlayBrowser(ulong itemId)
    {
        ThrowIfNotLoaded();

        steamClient!.OpenWorkshopItemInOverlayBrowser(itemId);
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
        if (active && !wePaused)
        {
            PauseManager.Instance.AddPause(nameof(SteamHandler));
            wePaused = true;
        }
        else if (!active && wePaused)
        {
            PauseManager.Instance.Resume(nameof(SteamHandler));
            wePaused = false;
        }

        steamClient?.OverlayStatusChanged(active);
    }

    public void CurrentUserStatsReceived(ulong game, int result, ulong user)
    {
        steamClient?.CurrentUserStatsReceived(game, result, user);
    }

    public void UserStatsReceived(ulong game, int result, ulong user)
    {
        steamClient?.UserStatsReceived(game, result, user);
    }

    public void UserStatsStored(ulong game, int result)
    {
        steamClient?.UserStatsStored(game, result);
    }

    public void LowPower(int batteryLeftMinutes)
    {
        // TODO: show a warning popup (once)
        steamClient?.LowPower(batteryLeftMinutes);
    }

    public void APICallComplete(ulong asyncCall, int callback, uint parameter)
    {
        steamClient?.APICallComplete(asyncCall, callback, parameter);
    }

    public void ShutdownRequested()
    {
        steamClient?.ShutdownRequested();

        GD.Print("Shutdown through Steam requested, closing the game");
        SceneManager.Instance.QuitThrive();
    }

    public void WorkshopItemCreated(int result, ulong fileId, bool acceptTermsOfService)
    {
        steamClient?.WorkshopItemCreated(result, fileId, acceptTermsOfService);
    }

    public void WorkshopItemDownloadedLocally(int result, ulong fileId, ulong appId)
    {
        steamClient?.WorkshopItemDownloadedLocally(result, fileId, appId);
    }

    public void WorkshopItemInstalledOrUpdatedLocally(ulong appId, ulong fileId)
    {
        if (appId != steamClient?.AppId)
            return;

        steamClient?.WorkshopItemInstalledOrUpdatedLocally(appId, fileId);

        // TODO: notify mod manager
    }

    public void WorkshopItemDeletedRemotely(int result, ulong fileId)
    {
        steamClient?.WorkshopItemDeletedRemotely(result, fileId);
    }

    public void WorkshopItemInfoUpdateFinished(int result, bool acceptTermsOfService)
    {
        steamClient?.WorkshopItemInfoUpdateFinished(result, acceptTermsOfService);
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

        RegisterSteamClient((ISteamClient?)Activator.CreateInstance(type) ??
            throw new Exception("Failed to create Steam client class type"));
    }

    private void ThrowIfNotLoaded()
    {
        if (!IsLoaded || steamClient == null)
            throw new InvalidOperationException("Steam is not loaded");
    }
}
