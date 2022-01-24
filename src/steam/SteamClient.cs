using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Object = Godot.Object;
using Path = System.IO.Path;

/// <summary>
///   Concrete implementation of the Steam integration handling. Only compiled for the Steam version of the game.
/// </summary>
public class SteamClient : ISteamClient
{
    private bool initStarted;

    private Action<WorkshopResult> workshopCreateCallback;
    private Action<WorkshopResult> workshopUpdateCallback;

    public bool IsLoaded { get; private set; }
    public string LoadError { get; private set; }
    public string ExtraErrorInfo { get; private set; }

    public bool? IsOnline { get; private set; }
    public ulong? SteamId { get; private set; }
    public bool? IsOwned { get; private set; }

    public uint AppId { get; private set; }

    public string DisplayName => Steam.GetPersonaName();

    public void Init()
    {
        if (initStarted)
            throw new InvalidOperationException("Can't initialize Steam more than once");

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

        // TODO: remove this cast once GodotSteam is updated
        AppId = (uint)Steam.GetAppID();

        GD.Print("Our app id is: ", AppId);
    }

    public void ConnectSignals<T>(T receiver)
        where T : Object, ISteamSignalReceiver
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

        // Workshop
        Steam.Singleton.Connect("item_created", receiver, nameof(ISteamSignalReceiver.WorkshopItemCreated));
        Steam.Singleton.Connect("item_downloaded", receiver,
            nameof(ISteamSignalReceiver.WorkshopItemDownloadedLocally));
        Steam.Singleton.Connect("item_installed", receiver,
            nameof(ISteamSignalReceiver.WorkshopItemInstalledOrUpdatedLocally));
        Steam.Singleton.Connect("item_deleted", receiver, nameof(ISteamSignalReceiver.WorkshopItemDeletedRemotely));
        Steam.Singleton.Connect("item_updated", receiver, nameof(ISteamSignalReceiver.WorkshopItemInfoUpdateFinished));
    }

    public void Process(float delta)
    {
        if (!IsLoaded)
            return;

        Steam.RunCallbacks();
    }

    public void CreateWorkshopItem(Action<WorkshopResult> callback)
    {
        if (workshopCreateCallback != null)
            throw new InvalidOperationException("Workshop create is already in-progress");

        workshopCreateCallback = callback ?? throw new ArgumentException("callback is required");

        GD.Print("Attempting new workshop item create");
        Steam.CreateItem(AppId, Steam.WorkshopFileTypeCommunity);
    }

    public ulong StartWorkshopItemUpdate(ulong itemId)
    {
        GD.Print("Beginning workshop update of: ", itemId);
        return Steam.StartItemUpdate(AppId, itemId);
    }

    public bool SetWorkshopItemTitle(ulong updateHandle, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");

        if (title.Length > Steam.PublishedDocumentTitleMax)
            throw new ArgumentException("Title is too long");

        return Steam.SetItemTitle(updateHandle, title);
    }

    public bool SetWorkshopItemDescription(ulong updateHandle, string description)
    {
        description ??= string.Empty;

        if (description.Length > Steam.PublishedDocumentDescriptionMax)
            throw new ArgumentException("Description is too long");

        return Steam.SetItemDescription(updateHandle, description);
    }

    public bool SetWorkshopItemVisibility(ulong updateHandle, SteamItemVisibility visibility)
    {
        int value;

        switch (visibility)
        {
            case SteamItemVisibility.Public:
                value = Steam.RemoteStoragePublishedVisiblityPublic;
                break;
            case SteamItemVisibility.FriendsOnly:
                value = Steam.RemoteStoragePublishedVisiblityFriendsOnly;
                break;
            case SteamItemVisibility.Private:
                value = Steam.RemoteStoragePublishedVisiblityPrivate;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
        }

        return Steam.SetItemVisibility(updateHandle, value);
    }

    public bool SetWorkshopItemContentFolder(ulong updateHandle, string contentFolder)
    {
        if (!Directory.Exists(contentFolder))
            throw new ArgumentException("content folder doesn't exist");

        return Steam.SetItemContent(updateHandle, Path.GetFullPath(contentFolder));
    }

    public bool SetWorkshopItemPreview(ulong updateHandle, string previewImage)
    {
        if (previewImage == null || !File.Exists(previewImage))
            throw new ArgumentException("preview image doesn't exist");

        if (!SteamHandler.RecommendedFileEndings.Contains(Path.GetExtension(previewImage)))
        {
            throw new ArgumentException("Non-recommended image type given as preview image");
        }

        return Steam.SetItemPreview(updateHandle, Path.GetFullPath(previewImage));
    }

    public bool SetWorkshopItemTags(ulong updateHandle, List<string> tags)
    {
        var array = new Array();

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                array.Add(tag);
            }
        }

        return Steam.SetItemTags(updateHandle, array);
    }

    public void SubmitWorkshopItemUpdate(ulong updateHandle, string changeNotes, Action<WorkshopResult> callback)
    {
        if (workshopUpdateCallback != null)
            throw new InvalidOperationException("Workshop update is already in-progress");

        workshopUpdateCallback = callback ?? throw new ArgumentException("callback is required");

        // Steam docs say this constant is unused, but at least currently it seems to be the same value as the document
        // description max length
        if (changeNotes?.Length > Steam.PublishedDocumentChangeDescriptionMax)
        {
            callback.Invoke(new WorkshopResult()
            {
                Success = false,
                TranslatedError = TranslationServer.Translate("CHANGE_DESCRIPTION_IS_TOO_LONG"),
            });
            return;
        }

        GD.Print("Submitting workshop update with handle: ", updateHandle);

        Steam.SubmitItemUpdate(updateHandle, changeNotes);
    }

    public List<string> GetInstalledWorkshopItemFolders()
    {
        var result = new List<string>();

        foreach (var itemId in GetSubscribedWorkshopItems())
        {
            if (GetWorkshopItemLocalState(itemId).Installed)
            {
                var installInfo = Steam.GetItemInstallInfo(itemId);

                if (!installInfo.Contains("ret") || installInfo["ret"] as bool? != true)
                {
                    GD.PrintErr("Workshop item ", itemId, " failed to retrieve install info");
                }
                else
                {
                    result.Add((string)installInfo["folder"]);
                }
            }
        }

        return result;
    }

    public SteamUploadProgress GetWorkshopItemUpdateProgress(ulong itemId)
    {
        var result = new SteamUploadProgress();

        var rawData = Steam.GetItemUpdateProgress(itemId);

        if (rawData.Contains("status") && rawData["status"] is int status)
        {
            result.ErrorHappened = status != Steam.ResultOk;
        }
        else
        {
            result.ErrorHappened = true;
            GD.PrintErr($"Failed to read status in {nameof(GetWorkshopItemUpdateProgress)}");
        }

        if (rawData.Contains("processed") && rawData["processed"] is ulong processed)
        {
            result.ProcessedBytes = processed;
        }

        if (rawData.Contains("total") && rawData["total"] is ulong total)
        {
            result.ProcessedBytes = total;
        }

        return result;
    }

    public void OpenWorkshopItemInOverlayBrowser(ulong itemId)
    {
        Steam.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{itemId}");
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

    public void WorkshopItemCreated(int result, ulong fileId, bool acceptTermsOfService)
    {
        GD.Print("Workshop item create result: ", result, " file: ", fileId, " TOS: ", acceptTermsOfService);

        if (workshopCreateCallback == null)
        {
            GD.PrintErr($"Got {nameof(WorkshopItemCreated)} even with no active callbacks");
            return;
        }

        var convertedResult = new WorkshopResult
        {
            TermsOfServiceSigningRequired = acceptTermsOfService,
            ItemId = fileId,
        };

        if (result == Steam.ResultOk)
        {
            convertedResult.Success = true;
        }
        else
        {
            convertedResult.Success = false;
            convertedResult.TranslatedError = GetDescriptiveSteamError(result);
        }

        workshopCreateCallback.Invoke(convertedResult);
        workshopCreateCallback = null;
    }

    public void WorkshopItemDownloadedLocally(int result, ulong fileId, int appId)
    {
        /*if (appId != AppId)
            return;*/
    }

    public void WorkshopItemInstalledOrUpdatedLocally(int appId, ulong fileId)
    {
        GD.Print("Workshop item downloaded or updated, file: ", fileId);
    }

    public void WorkshopItemDeletedRemotely(int result, ulong fileId)
    {
    }

    public void WorkshopItemInfoUpdateFinished(int result, bool acceptTermsOfService)
    {
        GD.Print("Workshop item update result: ", result, " TOS: ", acceptTermsOfService);

        if (workshopUpdateCallback == null)
        {
            GD.PrintErr($"Got {nameof(WorkshopItemInfoUpdateFinished)} even with no active callbacks");
            return;
        }

        var convertedResult = new WorkshopResult
        {
            TermsOfServiceSigningRequired = acceptTermsOfService,
        };

        if (result == Steam.ResultOk)
        {
            convertedResult.Success = true;
        }
        else
        {
            convertedResult.Success = false;
            convertedResult.TranslatedError = GetDescriptiveSteamError(result);
        }

        workshopUpdateCallback.Invoke(convertedResult);
        workshopUpdateCallback = null;
    }

    private void RefreshCurrentUserInfo()
    {
        IsOnline = Steam.LoggedOn();
        SteamId = Steam.GetSteamID();
        IsOwned = Steam.IsSubscribed();
    }

    private IEnumerable<ulong> GetSubscribedWorkshopItems()
    {
        foreach (var item in Steam.GetSubscribedItems())
        {
            // TODO: GodotSteam bug that it doesn't return the proper type here
            // yield return (ulong)item;

            var raw = BitConverter.GetBytes((int)item);

            uint hacked = BitConverter.ToUInt32(raw, 0);

            yield return hacked;
        }
    }

    private WorkshopItemState GetWorkshopItemLocalState(ulong itemId)
    {
        var result = new WorkshopItemState();

        var state = Steam.GetItemState(itemId);

        if (state == 0)
        {
            GD.PrintErr("Local state queried for workshop item that is not locally tracked: ", itemId);
            result.Untracked = true;
        }

        if ((state & Steam.ItemStateSubscribed) != 0)
        {
            result.Subscribed = true;
        }

        // Not handled
        if ((state & Steam.ItemStateLegacyItem) != 0)
        {
        }

        if ((state & Steam.ItemStateInstalled) != 0)
        {
            result.Installed = true;
        }

        if ((state & Steam.ItemStateNeedsUpdate) != 0)
        {
            result.NeedsUpdate = true;
        }

        if ((state & Steam.ItemStateDownloading) != 0)
        {
            result.Downloading = true;
        }

        if ((state & Steam.ItemStateDownloadPending) != 0)
        {
            result.DownloadPending = true;
        }

        return result;
    }

    private string GetDescriptiveSteamError(int result)
    {
        // Note: the exact problem varies a bit based on the action being performed, but for faster implementation
        // these are not separated by operation type TODO: (yet)
        switch (result)
        {
            case Steam.ResultOk:
                return null;

            case Steam.ResultInsufficientPrivilege:
                return TranslationServer.Translate("STEAM_ERROR_INSUFFICIENT_PRIVILEGE");
            case Steam.ResultBanned:
                return TranslationServer.Translate("STEAM_ERROR_BANNED");
            case Steam.ResultTimeout:
                return TranslationServer.Translate("STEAM_ERROR_TIMEOUT");
            case Steam.ResultNotLoggedOn:
                return TranslationServer.Translate("STEAM_ERROR_NOT_LOGGED_IN");
            case Steam.ResultServiceUnavailable:
                return TranslationServer.Translate("STEAM_ERROR_UNAVAILABLE");
            case Steam.ResultInvalidParam:
                return TranslationServer.Translate("STEAM_ERROR_INVALID_PARAMETER");
            case Steam.ResultLimitExceeded:
                return TranslationServer.Translate("STEAM_ERROR_CLOUD_LIMIT_EXCEEDED");
            case Steam.ResultFileNotFound:
                return TranslationServer.Translate("STEAM_ERROR_FILE_NOT_FOUND");
            case Steam.ResultDuplicateRequest:
                return TranslationServer.Translate("STEAM_ERROR_ALREADY_UPLOADED");
            case Steam.ResultDuplicateName:
                return TranslationServer.Translate("STEAM_ERROR_DUPLICATE_NAME");
            case Steam.ResultServiceReadOnly:
                return TranslationServer.Translate("STEAM_ERROR_ACCOUNT_READ_ONLY");
            case Steam.ResultAccessDenied:
                return TranslationServer.Translate("STEAM_ERROR_ACCOUNT_DOES_NOT_OWN_PRODUCT");
            case Steam.ResultLockingFailed:
                return TranslationServer.Translate("STEAM_ERROR_LOCKING_FAILED");
            default:
                return TranslationServer.Translate("STEAM_ERROR_UNKNOWN");
        }
    }

    private void SetError(string error, string extraDescription = null)
    {
        LoadError = error;
        ExtraErrorInfo = extraDescription;
    }
}
