public interface ISteamSignalReceiver
{
    void GenericSteamworksError(string failedSignal, string message);
    void OverlayStatusChanged(bool active);
    void CurrentUserStatsReceived(int game, int result, int user);
    void UserStatsReceived(int game, int result, int user);
    void UserStatsStored(int game, int result);
    void LowPower(int power);
    void APICallComplete(int asyncCall, int callback, int parameter);
    void ShutdownRequested();
}
