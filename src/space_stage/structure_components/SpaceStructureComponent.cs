using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Space variant of base class for all space structure components defined for structure types
///   <see cref="SpaceStructureDefinition"/>
/// </summary>
public abstract class SpaceStructureComponent
{
    [JsonProperty]
    protected readonly PlacedSpaceStructure owningStructure;

    protected SpaceStructureComponent(PlacedSpaceStructure owningStructure)
    {
        this.owningStructure = owningStructure;
    }

    public abstract void ProcessSpace(float delta, ISocietyStructureDataAccess dataAccess);

    public virtual void GetExtraAvailableActions(List<(InteractionType Type, string? DisabledAlternativeText)> result)
    {
    }

    /// <summary>
    ///   Perform an extra action that this component has returned from <see cref="GetExtraAvailableActions"/>
    /// </summary>
    /// <returns>True when performed</returns>
    /// <remarks>
    ///   <para>
    ///     The overriding method should not call this base method as this base method doesn't do anything and just
    ///     gives an error message
    ///   </para>
    /// </remarks>
    public virtual bool PerformExtraAction(InteractionType interactionType)
    {
        GD.PrintErr("Extra action perform not overridden can't perform: ", interactionType);
        return false;
    }
}
