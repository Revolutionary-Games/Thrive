/// <summary>
///   Delivery guarantee requested for a single outgoing network message
/// </summary>
public enum NetworkDelivery
{
    /// <summary>
    ///   May be lost, duplicated or arrive out of order.
    /// </summary>
    Unreliable = 0,

    /// <summary>
    ///   May be lost, but old messages arriving after newer ones are dropped by the transport.
    /// </summary>
    UnreliableSequenced = 1,

    /// <summary>
    ///   Guaranteed to arrive, and in the order sent.
    /// </summary>
    ReliableOrdered = 2,
}
