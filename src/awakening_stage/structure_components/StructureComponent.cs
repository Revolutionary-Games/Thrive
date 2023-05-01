using System.Collections.Generic;

/// <summary>
///   Base class for all structure components allowed in <see cref="StructureDefinition"/> and
///   <see cref="PlacedStructure"/>
/// </summary>
public abstract class StructureComponent
{
    protected readonly PlacedStructure owningStructure;

    public StructureComponent(PlacedStructure owningStructure)
    {
        this.owningStructure = owningStructure;
    }

    public virtual void GetExtraAvailableActions(List<(InteractionType Type, string? DisabledAlternativeText)> result)
    {
    }

    public virtual bool PerformExtraAction(InteractionType interactionType)
    {
        return false;
    }

    public virtual void ProcessSociety(float delta, ISocietyStructureDataAccess dataAccess)
    {
    }
}
