using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Interface specifying the Steam operations we use. Implemented in the SteamClient.cs file
/// </summary>
public interface ISteamClient : ISteamSignalReceiver, IDisposable
{
    /// <summary>
    ///   True if the Steam client has been initialized
    /// </summary>
    public bool IsLoaded { get; }

    public string DisplayName { get; }

    public ulong AppId { get; }

    public void Init();

    /// <summary>
    ///   Sets up receiver to receive callbacks
    /// </summary>
    /// <param name="receiver">The receiver</param>
    /// <typeparam name="T">Typeof the receiver object</typeparam>
    /// <remarks>
    ///   <para>
    ///     Note that for many of the received signals they need to be forwarded to this object for further processing.
    ///   </para>
    /// </remarks>
    public void ConnectSignals<T>(T receiver)
        where T : GodotObject, ISteamSignalReceiver;

    public void Process(double delta);
    public void CreateWorkshopItem(Action<WorkshopResult> callback);
    public ulong StartWorkshopItemUpdate(ulong itemId);
    public bool SetWorkshopItemTitle(ulong updateHandle, string title);
    public bool SetWorkshopItemDescription(ulong updateHandle, string? description);
    public bool SetWorkshopItemVisibility(ulong updateHandle, SteamItemVisibility visibility);
    public bool SetWorkshopItemContentFolder(ulong updateHandle, string contentFolder);
    public bool SetWorkshopItemPreview(ulong updateHandle, string previewImage);
    public void SubmitWorkshopItemUpdate(ulong updateHandle, string? changeNotes, Action<WorkshopResult> callback);
    public SteamUploadProgress GetWorkshopItemUpdateProgress(ulong itemId);
    public bool SetWorkshopItemTags(ulong updateHandle, List<string>? tags);
    public List<string> GetInstalledWorkshopItemFolders();
    public void OpenWorkshopItemInOverlayBrowser(ulong itemId);
}
