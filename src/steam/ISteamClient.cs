using Godot;

/// <summary>
///   Interface specifying the Steam operations we use. Implemented in the Steam/Steam.csproj library
/// </summary>
public interface ISteamClient : ISteamSignalReceiver
{
    /// <summary>
    ///   True if steam client has been initialized
    /// </summary>
    bool IsLoaded { get; }

    void Init();

    /// <summary>
    ///   Sets up receiver to receive callbacks
    /// </summary>
    /// <param name="receiver">The receiver. Must implement <see cref="ISteamSignalReceiver"/></param>
    /// <remarks>
    ///   <para>
    ///     Note that for many of the received signals they need to be forwarded to this object for further processing.
    ///   </para>
    /// </remarks>
    void ConnectSignals(Object receiver);

    void Process(float delta);
}
