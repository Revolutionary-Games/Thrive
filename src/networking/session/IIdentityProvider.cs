using System;

/// <summary>
///   Produces and verifies player identities
/// </summary>
public interface IIdentityProvider
{
    public IdentityKind Kind { get; }

    /// <summary>
    ///   Produces the ticket this client presents when joining. Empty for a guest.
    /// </summary>
    public byte[] CreateJoinTicket();

    /// <summary>
    ///   Checks a ticket a client sent
    /// </summary>
    /// <param name="ticket">The ticket data</param>
    /// <param name="claimedName">Name the client wants to use, which must not be trusted before verification</param>
    /// <param name="onVerified">
    ///   Called with the resulting identity, or with a failure reason when the ticket is not acceptable
    /// </param>
    public void VerifyJoinTicket(ReadOnlySpan<byte> ticket, string claimedName,
        Action<PlayerIdentity?, string?> onVerified);
}

/// <summary>
///   Accepts everyone under the name they ask for. Only for local testing and password protected private
///   servers, as nothing here is verified.
/// </summary>
public class GuestIdentityProvider : IIdentityProvider
{
    private ulong nextGuestId = 1;

    public IdentityKind Kind => IdentityKind.Guest;

    public byte[] CreateJoinTicket()
    {
        return [];
    }

    public void VerifyJoinTicket(ReadOnlySpan<byte> ticket, string claimedName,
        Action<PlayerIdentity?, string?> onVerified)
    {
        if (string.IsNullOrWhiteSpace(claimedName))
        {
            onVerified.Invoke(null, "Empty player name");
            return;
        }

        onVerified.Invoke(new PlayerIdentity(IdentityKind.Guest, nextGuestId++, claimedName), null);
    }
}
