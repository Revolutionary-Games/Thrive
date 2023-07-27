using System.Collections.Generic;

public class NetworkInputBatch : INetworkSerializable
{
    public uint StartTick { get; set; }

    public List<NetworkInputVars> Inputs { get; set; } = new();

    public void NetworkSerialize(BytesBuffer buffer)
    {
        buffer.Write(StartTick);

        foreach (var input in Inputs)
            input.NetworkSerialize(buffer);
    }

    public void NetworkDeserialize(BytesBuffer buffer)
    {
        StartTick = buffer.ReadUInt32();

        while (buffer.Position < buffer.Length)
        {
            var input = default(NetworkInputVars);
            input.NetworkDeserialize(buffer);
            Inputs.Add(input);
        }
    }
}
