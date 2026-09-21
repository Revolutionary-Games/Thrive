/// <summary>
///   Where a player's identity came from
/// </summary>
public enum IdentityKind
{
    /// <summary>
    ///   No account, only a name typed in for this session. The server decides whether to allow these.
    /// </summary>
    Guest = 0,

    /// <summary>
    ///   A Thrive account verified against the master server
    /// </summary>
    Thrive = 1,

    /// <summary>
    ///   A Steam account verified through a Steam session ticket
    /// </summary>
    Steam = 2,
}

/// <summary>
///   Who a peer is, as far as the server knows
/// </summary>
/// <remarks>
///   <para>
///     Keeping the kind separate from the ID is what allows Steam and non-Steam players in the same match, so
///     multiplayer does not become Steam-only.
///   </para>
/// </remarks>
public struct PlayerIdentity(IdentityKind kind, ulong id, string name)
{
    public IdentityKind Kind = kind;

    public ulong Id = id;

    public string Name = name;

    /// <summary>
    ///   True when this identity was verified rather than just claimed by the client.
    /// </summary>
    public bool Verified = false;
}
