using System;
using System.Collections.Generic;

/// <summary>
///   Allows placing individual hexes with data in a layout
/// </summary>
/// <typeparam name="TData">The type of data to hold in hexes</typeparam>
[UseThriveSerializer]
public class IndividualHexLayout<TData> : HexLayout<HexWithData<TData>>
    where TData : IActionHex
{
    public IndividualHexLayout(Action<HexWithData<TData>> onAdded, Action<HexWithData<TData>>? onRemoved = null) : base(
        onAdded, onRemoved)
    {
    }

    public IndividualHexLayout()
    {
    }

    protected override IEnumerable<Hex> GetHexComponentPositions(HexWithData<TData> hex)
    {
        // The single hex is always at 0,0 as it's at the exact position the hex's overall position is
        yield return new Hex(0, 0);
    }
}
