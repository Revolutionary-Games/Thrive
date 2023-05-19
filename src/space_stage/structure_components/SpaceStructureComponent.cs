using System.Collections.Generic;
using Godot;

/// <summary>
///   Space variant of base class for all space structure components defined for structure types
///   <see cref="SpaceStructureDefinition"/>
/// </summary>
public abstract class SpaceStructureComponent
{
    public abstract void ProcessSpace(float delta, ISocietyStructureDataAccess dataAccess);

    public virtual void GetExtraAvailableActions(List<(InteractionType Type, string? DisabledAlternativeText)> result)
    {
    }

    public virtual bool PerformExtraAction(InteractionType interactionType)
    {
        GD.PrintErr("Extra action perform not overridden can't perform: ", interactionType);
        return false;
    }
}
