using System;
using System.Collections.Generic;
using Godot;

public class AscensionComponent : SpaceStructureComponent
{
    public override void ProcessSpace(float delta, ISocietyStructureDataAccess dataAccess)
    {
    }

    public override void GetExtraAvailableActions(List<(InteractionType Type, string? DisabledAlternativeText)> result)
    {
        base.GetExtraAvailableActions(result);

        result.Add((InteractionType.ActivateAscension, null));
    }

    public override bool PerformExtraAction(InteractionType interactionType)
    {
        if (interactionType == InteractionType.ActivateAscension)
        {
            GD.Print("Ascension gate is activated");
            throw new NotImplementedException();

            return true;
        }

        return base.PerformExtraAction(interactionType);
    }
}

public class AscensionComponentFactory : ISpaceStructureComponentFactory
{
    public SpaceStructureComponent Create()
    {
        return new AscensionComponent();
    }

    public void Check(string name)
    {
    }
}
