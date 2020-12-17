using System;
using System.Collections.Generic;
using System.Linq;

public class ColonyCompoundBag : CompoundBag
{
    private Microbe microbe;

    public ColonyCompoundBag(Microbe microbe) : base(microbe.GetAllColonyMembers().Sum(p => p.Compounds.Capacity))
    {
        this.microbe = microbe;
    }

    public override float Capacity =>
        IsInAColony ? microbe.GetAllColonyMembers().Sum(p => p.Compounds.Capacity) : base.Capacity;

    public override Dictionary<Compound, float> Compounds =>
        IsInAColony ?
            microbe
               .GetAllColonyMembers()
               .SelectMany(p => p.Compounds.Compounds)
               .GroupBy(p => p.Key)
               .ToDictionary(p => p.Key, p => p.Sum(x => x.Value)) :
            base.Compounds;

    private bool IsInAColony => microbe.Colony != null;

    /// <summary>
    ///   Distributes the amount above average compounds to the cells below average compounds.
    /// </summary>
    public void DistributeCompoundSurplus()
    {
        if (microbe.Colony == null)
            return;

        foreach (var compoundPair in Compounds)
        {
            var compound = compoundPair.Key;
            var average = compoundPair.Value / microbe.CountColonyMembers();

            var surplus = Math.Max(0, microbe.Compounds.GetCompoundAmount(compound) - average);
            foreach (var member in microbe.GetAllColonyMembers())
            {
                if (surplus <= 0.0001)
                    continue;

                var currValue = member.Compounds.GetCompoundAmount(compound);
                var toAdd = Math.Min(Math.Max(0, average - currValue), surplus);
                surplus -= toAdd;
                member.Compounds.AddCompound(compound, microbe.Compounds.TakeCompound(compound, toAdd));
            }
        }
    }

    public override float TakeCompound(Compound compound, float amount)
    {
        var took = base.TakeCompound(compound, amount);
        if (took < 0.0001f)
            return took;

        // Take equal amount from everyone
        var takePerCell = took / microbe.CountColonyMembers();

        foreach (var allColonyMember in microbe.GetAllColonyMembers())
            allColonyMember.Compounds.TakeCompound(compound, takePerCell);

        return took;
    }

    public override float AddCompound(Compound compound, float amount)
    {
        var addedCompound = base.AddCompound(compound, amount);

        // Add equal amount to everyone
        var amountPerCell = addedCompound / microbe.CountColonyMembers();

        foreach (var allColonyMember in microbe.GetAllColonyMembers())
            allColonyMember.Compounds.AddCompound(compound, amountPerCell);

        return addedCompound;
    }

    public override void ClearCompounds()
    {
        base.ClearCompounds();
        foreach (var allColonyMember in microbe.GetAllColonyMembers())
            allColonyMember.Compounds.ClearCompounds();
    }

    public override void ClampNegativeCompoundAmounts()
    {
        base.ClampNegativeCompoundAmounts();
        foreach (var allColonyMember in microbe.GetAllColonyMembers())
            allColonyMember.Compounds.ClampNegativeCompoundAmounts();
    }
}
