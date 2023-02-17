using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Steamworks;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Object = Godot.Object;
using Path = System.IO.Path;

/// <summary>
///   Concrete implementation of the Steam integration handling. Only compiled for the Steam version of the game.
/// </summary>
public sealed class SteamClient : ISteamClient
{
    private bool initStarted;

    private AppId_t appId;

    private Action<WorkshopResult>? workshopCreateCallback;
    private Action<WorkshopResult>? workshopUpdateCallback;

    // We need to keep these variables around for the GC to not eat these
    private Callback<GameOverlayActivated_t>? overlayToggledCallback;
    private Callback<UserStatsReceived_t>? statsReceivedCallback;
    private Callback<UserStatsStored_t>? statsStoredCallback;
    private Callback<SteamShutdown_t>? shutdownCallback;
    private CallResult<CreateItemResult_t>? createItemCallback;
    private Callback<DownloadItemResult_t>? downloadItemCallback;
    private Callback<ItemInstalled_t>? installItemCallback;
    private CallResult<DeleteItemResult_t>? deleteItemCallback;
    private CallResult<SubmitItemUpdateResult_t>? updateItemCallback;
    private Callback<SteamAPICallCompleted_t>? apiCallCompletedCallback;
    private Callback<LowBatteryPower_t>? lowPowerCallback;

    public bool IsLoaded { get; private set; }
    public string? LoadError { get; private set; }
    public string? ExtraErrorInfo { get; private set; }

    public bool? IsOnline { get; private set; }
    public CSteamID SteamId { get; private set; }
    public bool? IsOwned { get; private set; }

    public ulong AppId { get; private set; }

    public string DisplayName => SteamFriends.GetPersonaName();

    public void Init()
    {
        if (initStarted)
            throw new InvalidOperationException("Can't initialize Steam more than once");

        initStarted = true;

        // TODO: should we do this here? NOTE: doesn't work with steam_appid.txt existing
        // if (SteamAPI.RestartAppIfNecessary(new AppId_t(1779200)))
        // {
        //     GD.Print("Restarting to properly launch through Steam");
        //
        //     // TODO: how do we quit here?
        // }

        GD.Print("Starting Steam load");

        if (!SteamAPI.Init())
        {
            GD.PrintErr("Failed to init Steam");
            SetError(TranslationServer.Translate("STEAM_CLIENT_INIT_FAILED"), "Steamworks initialization failed");
            return;
        }

        if (!SteamAPI.IsSteamRunning())
        {
            GD.PrintErr("Steam is not running");
            SetError(TranslationServer.Translate("STEAM_CLIENT_INIT_FAILED"), "Steam is not running");
            return;
        }

        GD.Print("Steam load finished");
        IsLoaded = true;

        RefreshCurrentUserInfo();

        if (IsOwned == true)
        {
            GD.Print("Game is owned by current Steam user");

            if (!SteamUserStats.RequestCurrentStats())
                GD.PrintErr("Failed to request current Steam stats");
        }

        appId = SteamUtils.GetAppID();
        AppId = (ulong)appId;

        GD.Print("Our app id is: ", AppId);
    }

    public void ConnectSignals<T>(T receiver)
        where T : Object, ISteamSignalReceiver
    {
        if (!IsLoaded)
            return;

        // TODO: some of these still need to check if they need to use Callback or CallResult

        // clientErrorCallback = new Callback<IPCFailure_t>(t => receiver.GenericSteamworksError(t.m_eFailureType))
        overlayToggledCallback =
            new Callback<GameOverlayActivated_t>(t => receiver.OverlayStatusChanged(t.m_bActive != 0));

        statsReceivedCallback =
            new Callback<UserStatsReceived_t>(t =>
                receiver.UserStatsReceived(t.m_nGameID, (int)t.m_eResult, (ulong)t.m_steamIDUser));
        statsStoredCallback =
            new Callback<UserStatsStored_t>(t =>
                receiver.UserStatsStored(t.m_nGameID, (int)t.m_eResult));

        shutdownCallback =
            new Callback<SteamShutdown_t>(_ => receiver.ShutdownRequested());

        // Workshop
        createItemCallback =
            new CallResult<CreateItemResult_t>((t, ioError) => receiver.WorkshopItemCreated(
                ioError ? (int)EResult.k_EResultIOFailure : (int)t.m_eResult,
                (ulong)t.m_nPublishedFileId, t.m_bUserNeedsToAcceptWorkshopLegalAgreement));
        downloadItemCallback =
            new Callback<DownloadItemResult_t>(t =>
                receiver.WorkshopItemDownloadedLocally((int)t.m_eResult, (ulong)t.m_nPublishedFileId,
                    (ulong)t.m_unAppID));
        installItemCallback =
            new Callback<ItemInstalled_t>(t =>
                receiver.WorkshopItemInstalledOrUpdatedLocally((ulong)t.m_unAppID, (ulong)t.m_nPublishedFileId));
        deleteItemCallback =
            new CallResult<DeleteItemResult_t>((t, ioError) =>
                receiver.WorkshopItemDeletedRemotely(ioError ? (int)EResult.k_EResultIOFailure : (int)t.m_eResult,
                    (ulong)t.m_nPublishedFileId));
        updateItemCallback =
            new CallResult<SubmitItemUpdateResult_t>((t, ioError) =>
                receiver.WorkshopItemInfoUpdateFinished(ioError ? (int)EResult.k_EResultIOFailure : (int)t.m_eResult,
                    t.m_bUserNeedsToAcceptWorkshopLegalAgreement));

        apiCallCompletedCallback =
            new Callback<SteamAPICallCompleted_t>(t =>
                receiver.APICallComplete((ulong)t.m_hAsyncCall, t.m_iCallback, t.m_cubParam));

        lowPowerCallback = new Callback<LowBatteryPower_t>(t => receiver.LowPower(t.m_nMinutesBatteryLeft));
    }

    public void Process(float delta)
    {
        if (!IsLoaded)
            return;

        SteamAPI.RunCallbacks();
    }

    public void CreateWorkshopItem(Action<WorkshopResult> callback)
    {
        if (workshopCreateCallback != null)
            throw new InvalidOperationException("Workshop create is already in-progress");

        if (createItemCallback == null)
            throw new InvalidOperationException("Listeners not created");

        workshopCreateCallback = callback ?? throw new ArgumentException("callback is required");

        GD.Print("Attempting new workshop item create");

        var apiCall = SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        createItemCallback.Set(apiCall);
    }

    public ulong StartWorkshopItemUpdate(ulong itemId)
    {
        GD.Print("Beginning workshop update of: ", itemId);
        return (ulong)SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(itemId));
    }

    public bool SetWorkshopItemTitle(ulong updateHandle, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");

        if (title.Length > Steamworks.Constants.k_cchPublishedDocumentTitleMax)
            throw new ArgumentException("Title is too long");

        return SteamUGC.SetItemTitle(new UGCUpdateHandle_t(updateHandle), title);
    }

    public bool SetWorkshopItemDescription(ulong updateHandle, string? description)
    {
        description ??= string.Empty;

        if (description.Length > Steamworks.Constants.k_cchPublishedDocumentDescriptionMax)
            throw new ArgumentException("Description is too long");

        return SteamUGC.SetItemDescription(new UGCUpdateHandle_t(updateHandle), description);
    }

    public bool SetWorkshopItemVisibility(ulong updateHandle, SteamItemVisibility visibility)
    {
        ERemoteStoragePublishedFileVisibility value;

        switch (visibility)
        {
            case SteamItemVisibility.Public:
                value = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
                break;
            case SteamItemVisibility.FriendsOnly:
                value = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly;
                break;
            case SteamItemVisibility.Private:
                value = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
        }

        return SteamUGC.SetItemVisibility(new UGCUpdateHandle_t(updateHandle), value);
    }

    public bool SetWorkshopItemContentFolder(ulong updateHandle, string contentFolder)
    {
        if (!Directory.Exists(contentFolder))
            throw new ArgumentException("content folder doesn't exist");

        return SteamUGC.SetItemContent(new UGCUpdateHandle_t(updateHandle), Path.GetFullPath(contentFolder));
    }

    public bool SetWorkshopItemPreview(ulong updateHandle, string previewImage)
    {
        if (!File.Exists(previewImage))
            throw new ArgumentException("preview image doesn't exist");

        if (!SteamHandler.RecommendedFileEndings.Contains(Path.GetExtension(previewImage)))
        {
            throw new ArgumentException("Non-recommended image type given as preview image");
        }

        return SteamUGC.SetItemPreview(new UGCUpdateHandle_t(updateHandle), Path.GetFullPath(previewImage));
    }

    public bool SetWorkshopItemTags(ulong updateHandle, List<string>? tags)
    {
        return SteamUGC.SetItemTags(new UGCUpdateHandle_t(updateHandle), tags ?? new List<string>());
    }

    public void SubmitWorkshopItemUpdate(ulong updateHandle, string? changeNotes, Action<WorkshopResult> callback)
    {
        if (workshopUpdateCallback != null)
            throw new InvalidOperationException("Workshop update is already in-progress");

        if (updateItemCallback == null)
            throw new InvalidOperationException("Listeners not created");

        workshopUpdateCallback = callback ?? throw new ArgumentException("callback is required");

        // Steam docs say this constant is unused, but at least currently it seems to be the same value as the document
        // description max length
        if (changeNotes?.Length > Steamworks.Constants.k_cchPublishedDocumentChangeDescriptionMax)
        {
            callback.Invoke(
                WorkshopResult.CreateFailure(TranslationServer.Translate("CHANGE_DESCRIPTION_IS_TOO_LONG")));
            return;
        }

        GD.Print("Submitting workshop update with handle: ", updateHandle);

        var apiCall = SteamUGC.SubmitItemUpdate(new UGCUpdateHandle_t(updateHandle), changeNotes);
        updateItemCallback.Set(apiCall);
    }

    public List<string> GetInstalledWorkshopItemFolders()
    {
        var result = new List<string>();

        foreach (var itemId in GetSubscribedWorkshopItems())
        {
            if (GetWorkshopItemLocalState(itemId).Installed)
            {
                var installResult = SteamUGC.GetItemInstallInfo(itemId, out _, out var folder,
                    Constants.MAX_PATH_LENGTH, out _);

                if (!installResult || string.IsNullOrEmpty(folder))
                {
                    GD.PrintErr("Workshop item ", itemId, " failed to retrieve install info");
                }
                else
                {
                    result.Add(folder);
                }
            }
        }

        return result;
    }

    public SteamUploadProgress GetWorkshopItemUpdateProgress(ulong itemId)
    {
        var result = new SteamUploadProgress();

        var status =
            SteamUGC.GetItemUpdateProgress(new UGCUpdateHandle_t(itemId), out var bytesProcessed, out var bytesTotal);

        if (status == EItemUpdateStatus.k_EItemUpdateStatusInvalid)
        {
            result.ErrorHappened = true;
            GD.PrintErr($"Failed to read status in {nameof(GetWorkshopItemUpdateProgress)}");
        }

        result.ProcessedBytes = bytesProcessed;
        result.TotalBytes = bytesTotal;

        return result;
    }

    public void OpenWorkshopItemInOverlayBrowser(ulong itemId)
    {
        SteamFriends.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{itemId}");
    }

    public void GenericSteamworksError(string failedSignal, string message)
    {
        GD.PrintErr("Steamworks error ", failedSignal, ": ", message);
    }

    public void OverlayStatusChanged(bool active)
    {
    }

    public void CurrentUserStatsReceived(ulong game, int result, ulong user)
    {
        if (result == (int)EResult.k_EResultOK)
        {
            GD.Print("Received stats for current user");
        }
        else if (result == (int)EResult.k_EResultFail)
        {
            GD.Print("Failed to receive stats for current user");
        }
        else
        {
            GD.PrintErr("Unknown result for CurrentUserStatsReceived: ", result);
        }
    }

    public void UserStatsReceived(ulong game, int result, ulong user)
    {
    }

    public void UserStatsStored(ulong game, int result)
    {
    }

    public void LowPower(int batteryLeftMinutes)
    {
    }

    public void APICallComplete(ulong asyncCall, int callback, uint parameter)
    {
    }

    public void ShutdownRequested()
    {
    }

    public void WorkshopItemCreated(int result, ulong fileId, bool acceptTermsOfService)
    {
        GD.Print("Workshop item create result: ", result, " file: ", fileId, " TOS: ", acceptTermsOfService);
        var translatedResult = (EResult)result;

        if (workshopCreateCallback == null)
        {
            GD.PrintErr($"Got {nameof(WorkshopItemCreated)} even with no active callbacks");
            return;
        }

        bool success = true;
        string? error = null;

        if (translatedResult != EResult.k_EResultOK)
        {
            success = false;
            error = GetDescriptiveSteamError(translatedResult);
        }

        var convertedResult = new WorkshopResult(success, error, acceptTermsOfService, fileId);

        workshopCreateCallback.Invoke(convertedResult);
        workshopCreateCallback = null;
    }

    public void WorkshopItemDownloadedLocally(int result, ulong fileId, ulong relatedAppId)
    {
        /*if (appId != AppId)
            return;*/
    }

    public void WorkshopItemInstalledOrUpdatedLocally(ulong relatedAppId, ulong fileId)
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

        bool success = true;
        string? error = null;

        if (result != (int)EResult.k_EResultOK)
        {
            success = false;
            error = GetDescriptiveSteamError((EResult)result);
        }

        var convertedResult = new WorkshopResult(success, error, acceptTermsOfService, null);

        workshopUpdateCallback.Invoke(convertedResult);
        workshopUpdateCallback = null;
    }

    public void Dispose()
    {
        overlayToggledCallback?.Dispose();
        statsReceivedCallback?.Dispose();
        statsStoredCallback?.Dispose();
        shutdownCallback?.Dispose();
        createItemCallback?.Dispose();
        downloadItemCallback?.Dispose();
        installItemCallback?.Dispose();
        deleteItemCallback?.Dispose();
        updateItemCallback?.Dispose();
        apiCallCompletedCallback?.Dispose();
        lowPowerCallback?.Dispose();

        GD.Print("Shutting down Steam API");
        SteamAPI.Shutdown();
    }

    private void RefreshCurrentUserInfo()
    {
        IsOnline = SteamUser.BLoggedOn();

        if (IsOnline == false)
        {
            IsOwned = false;
            return;
        }

        SteamId = SteamUser.GetSteamID();
        IsOwned = SteamApps.BIsSubscribed();
    }

    private IEnumerable<PublishedFileId_t> GetSubscribedWorkshopItems()
    {
        var count = SteamUGC.GetNumSubscribedItems();

        var subscribed = new PublishedFileId_t[count];
        count = SteamUGC.GetSubscribedItems(subscribed, (uint)subscribed.Length);
        return subscribed.Take((int)count);
    }

    private WorkshopItemState GetWorkshopItemLocalState(PublishedFileId_t itemId)
    {
        var result = new WorkshopItemState();

        var state = (EItemState)SteamUGC.GetItemState(itemId);

        if (state == 0)
        {
            GD.PrintErr("Local state queried for workshop item that is not locally tracked: ", itemId);
            result.Untracked = true;
        }

        if ((state & EItemState.k_EItemStateSubscribed) != 0)
        {
            result.Subscribed = true;
        }

        // Not handled
        if ((state & EItemState.k_EItemStateLegacyItem) != 0)
        {
        }

        if ((state & EItemState.k_EItemStateInstalled) != 0)
        {
            result.Installed = true;
        }

        if ((state & EItemState.k_EItemStateNeedsUpdate) != 0)
        {
            result.NeedsUpdate = true;
        }

        if ((state & EItemState.k_EItemStateDownloading) != 0)
        {
            result.Downloading = true;
        }

        if ((state & EItemState.k_EItemStateDownloadPending) != 0)
        {
            result.DownloadPending = true;
        }

        return result;
    }

    private string? GetDescriptiveSteamError(EResult result)
    {
        // Note: the exact problem varies a bit based on the action being performed, but for faster implementation
        // these are not separated by operation type TODO: (yet)
        switch (result)
        {
            case EResult.k_EResultOK:
                return null;

            case EResult.k_EResultInsufficientPrivilege:
                return TranslationServer.Translate("STEAM_ERROR_INSUFFICIENT_PRIVILEGE");
            case EResult.k_EResultBanned:
                return TranslationServer.Translate("STEAM_ERROR_BANNED");
            case EResult.k_EResultTimeout:
                return TranslationServer.Translate("STEAM_ERROR_TIMEOUT");
            case EResult.k_EResultNotLoggedOn:
                return TranslationServer.Translate("STEAM_ERROR_NOT_LOGGED_IN");
            case EResult.k_EResultServiceUnavailable:
                return TranslationServer.Translate("STEAM_ERROR_UNAVAILABLE");
            case EResult.k_EResultInvalidParam:
                return TranslationServer.Translate("STEAM_ERROR_INVALID_PARAMETER");
            case EResult.k_EResultLimitExceeded:
                return TranslationServer.Translate("STEAM_ERROR_CLOUD_LIMIT_EXCEEDED");
            case EResult.k_EResultFileNotFound:
                return TranslationServer.Translate("STEAM_ERROR_FILE_NOT_FOUND");
            case EResult.k_EResultDuplicateRequest:
                return TranslationServer.Translate("STEAM_ERROR_ALREADY_UPLOADED");
            case EResult.k_EResultDuplicateName:
                return TranslationServer.Translate("STEAM_ERROR_DUPLICATE_NAME");
            case EResult.k_EResultServiceReadOnly:
                return TranslationServer.Translate("STEAM_ERROR_ACCOUNT_READ_ONLY");
            case EResult.k_EResultAccessDenied:
                return TranslationServer.Translate("STEAM_ERROR_ACCOUNT_DOES_NOT_OWN_PRODUCT");
            case EResult.k_EResultLockingFailed:
                return TranslationServer.Translate("STEAM_ERROR_LOCKING_FAILED");
            default:
                return TranslationServer.Translate("STEAM_ERROR_UNKNOWN");
        }
    }

    private void SetError(string error, string? extraDescription = null)
    {
        LoadError = error;
        ExtraErrorInfo = extraDescription;
    }
}
