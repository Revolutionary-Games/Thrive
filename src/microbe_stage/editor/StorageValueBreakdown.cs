namespace Thrive.microbe_stage.editor;

using System.Collections.Generic;

/// <summary>
/// Stores a summary of an organism's storage, including how much of it comes from the specialization bonus.
/// </summary>
public class StorageValueBreakdown
{
    public readonly Dictionary<Compound, ValueBreakdown> SpecificStorage = new();

    public ValueBreakdown NominalStorage;

    public void Add(Compound compound, float baseValue, float specializationBonus)
    {
        if (!SpecificStorage.TryGetValue(compound, out ValueBreakdown breakdown))
            breakdown = default(ValueBreakdown);

        var total = baseValue * specializationBonus;
        breakdown.Total += total;

        var specialization = total - baseValue;
        breakdown.Specialization += specialization;

        breakdown.Base += baseValue;

        SpecificStorage[compound] = breakdown;
    }
}
