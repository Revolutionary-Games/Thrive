using System;

/// <summary>
///   Multiplayer equivalent of <see cref="IStageBase"/>.
/// </summary>
public interface IMultiplayerStage : IStageBase
{
    /// <summary>
    ///   Should be emitted when the multiplayer stage is done setting up.
    /// </summary>
    public event EventHandler? GameReady;

    public MultiplayerGameWorld MultiplayerWorld { get; }

    /// <summary>
    ///   The number of frames ticked since the start of this stage's network tick.
    /// </summary>
    public uint TickCount { get; }

    public uint LastReceivedServerTick { get; }

    public uint LastAckedInputTick { get; }
}
