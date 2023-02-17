public interface ISteamSignalReceiver
{
    public void GenericSteamworksError(string failedSignal, string message);
    public void OverlayStatusChanged(bool active);
    public void CurrentUserStatsReceived(ulong game, int result, ulong user);
    public void UserStatsReceived(ulong game, int result, ulong user);
    public void UserStatsStored(ulong game, int result);
    public void LowPower(int batteryLeftMinutes);
    public void APICallComplete(ulong asyncCall, int callback, uint parameter);
    public void ShutdownRequested();

    public void WorkshopItemCreated(int result, ulong fileId, bool acceptTermsOfService);
    public void WorkshopItemDownloadedLocally(int result, ulong fileId, ulong appId);
    public void WorkshopItemInstalledOrUpdatedLocally(ulong appId, ulong fileId);

    // TODO: this doesn't seem to be actually a callback about this, so this should be removed probably (at least until
    // we allow deleting workshop creations from within Thrive)
    public void WorkshopItemDeletedRemotely(int result, ulong fileId);
    public void WorkshopItemInfoUpdateFinished(int result, bool acceptTermsOfService);
}
