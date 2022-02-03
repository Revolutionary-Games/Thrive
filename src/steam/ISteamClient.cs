using System;
using System.Collections.Generic;
using Object = Godot.Object;

/// <summary>
///   Interface specifying the Steam operations we use. Implemented in the SteamClient.cs file
/// </summary>
public interface ISteamClient : ISteamSignalReceiver
{
    /// <summary>
    ///   True if the Steam client has been initialized
    /// </summary>
    bool IsLoaded { get; }

    string DisplayName { get; }

    public uint AppId { get; }

    void Init();

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
    void ConnectSignals<T>(T receiver)
        where T : Object, ISteamSignalReceiver;

    void Process(float delta);
    void CreateWorkshopItem(Action<WorkshopResult> callback);
    ulong StartWorkshopItemUpdate(ulong itemId);
    bool SetWorkshopItemTitle(ulong updateHandle, string title);
    bool SetWorkshopItemDescription(ulong updateHandle, string description);
    bool SetWorkshopItemVisibility(ulong updateHandle, SteamItemVisibility visibility);
    bool SetWorkshopItemContentFolder(ulong updateHandle, string contentFolder);
    bool SetWorkshopItemPreview(ulong updateHandle, string previewImage);
    void SubmitWorkshopItemUpdate(ulong updateHandle, string? changeNotes, Action<WorkshopResult> callback);
    SteamUploadProgress GetWorkshopItemUpdateProgress(ulong itemId);
    bool SetWorkshopItemTags(ulong updateHandle, List<string> tags);
    List<string> GetInstalledWorkshopItemFolders();
    void OpenWorkshopItemInOverlayBrowser(ulong itemId);
}
