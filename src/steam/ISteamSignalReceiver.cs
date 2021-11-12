public interface ISteamSignalReceiver
{
    void GenericSteamworksError(string failedSignal, string message);
    void OverlayStatusChanged(bool active);
}
