public interface ISteamSignalReceiver
{
    void GenericSteamworksError(string failedSignal, string message);
    void OverlayStatusChanged(bool active);
    void CurrentUserStatsReceived(ulong game, int result, ulong user);
    void UserStatsReceived(ulong game, int result, ulong user);
    void UserStatsStored(ulong game, int result);
    void LowPower(int batteryLeftMinutes);
    void APICallComplete(ulong asyncCall, int callback, uint parameter);
    void ShutdownRequested();

    void WorkshopItemCreated(int result, ulong fileId, bool acceptTermsOfService);
    void WorkshopItemDownloadedLocally(int result, ulong fileId, ulong appId);
    void WorkshopItemInstalledOrUpdatedLocally(ulong appId, ulong fileId);

    // TODO: this doesn't seem to be actually a callback about this, so this should be removed probably (at least until
    // we allow deleting workshop creations from within Thrive)
    void WorkshopItemDeletedRemotely(int result, ulong fileId);
    void WorkshopItemInfoUpdateFinished(int result, bool acceptTermsOfService);
}
