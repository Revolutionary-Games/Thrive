/// <summary>
///   One tick of a player's input, in whatever form a game mode needs
/// </summary>
/// <remarks>
///   <para>
///     Input is sent unreliably with a few recent ticks repeated, so a dropped packet does not stall.
///   </para>
/// </remarks>
public interface IInputPayload
{
    /// <summary>
    ///   Tick this input was produced on
    /// </summary>
    public uint Tick { get; set; }

    public void Write(NetworkWriter writer);

    public void Read(NetworkReader reader);

    public void CopyFrom(IInputPayload other);
}
