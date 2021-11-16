using Godot;

/// <summary>
///   Interface specifying the Steam operations we use. Implemented in the SteamClient.cs file
/// </summary>
public interface ISteamClient : ISteamSignalReceiver
{
    /// <summary>
    ///   True if steam client has been initialized
    /// </summary>
    bool IsLoaded { get; }

    string DisplayName { get; }

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
}
