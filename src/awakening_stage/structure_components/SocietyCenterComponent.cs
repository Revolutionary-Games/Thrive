using System.Collections.Generic;
using Godot;

public class SocietyCenterComponent : StructureComponent
{
    public SocietyCenterComponent(PlacedStructure owningStructure) : base(owningStructure)
    {
    }

    public override void GetExtraAvailableActions(List<(InteractionType Type, string? DisabledAlternativeText)> result)
    {
        result.Add((InteractionType.FoundSettlement, null));
    }

    public override bool PerformExtraAction(InteractionType interactionType)
    {
        if (interactionType != InteractionType.FoundSettlement)
            return false;

        // TODO: a cleaner way to do this
        var stage = owningStructure.FirstAncestorOfType<MulticellularStage>();

        if (stage == null)
        {
            GD.PrintErr("Could not find parent stage of multicellular type for found settlement action");
            return true;
        }

        stage.OnSocietyFounded(owningStructure);
        return true;
    }
}

public class SocietyCenterComponentFactory : IStructureComponentFactory
{
    public StructureComponent Create(PlacedStructure owningStructure)
    {
        return new SocietyCenterComponent(owningStructure);
    }

    public void Check(string name)
    {
    }
}
