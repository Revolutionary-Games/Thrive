/// <summary>
///   Specifies object whose values can be transmitted across network under custom user-defined format.
/// </summary>
public interface INetworkSerializable
{
    /// <summary>
    ///   Packs informations/properties about the object as bytes to be sent across network.
    /// </summary>
    public void NetworkSerialize(BytesBuffer buffer);

    /// <summary>
    ///   Unpacks incoming informations/properties about the object.
    /// </summary>
    public void NetworkDeserialize(BytesBuffer buffer);
}
