namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;

/// <summary>
///   Handles creating temporary organelles for endosymbiosis
/// </summary>
[ReadsComponent(typeof(SpeciesMember))]
[ReadsComponent(typeof(CellProperties))]
[RunsBefore(typeof(MicrobeReproductionSystem))]
[RunsBefore(typeof(MicrobePhysicsCreationAndSizeSystem))]
[RunsBefore(typeof(MicrobeVisualsSystem))]
[RuntimeCost(0.25f)]
public partial class EndosymbiontOrganelleSystem : BaseSystem<World, float>
{
    // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
    // private readonly ThreadLocal<List<Hex>> hexWorkData = new(() => new List<Hex>());
    // private readonly ThreadLocal<List<Hex>> hexWorkData2 = new(() => new List<Hex>());

    private readonly List<Hex> hexWorkData = new();
    private readonly List<Hex> hexWorkData2 = new();
    private readonly HashSet<Hex> hexWorkData3 = new();

    public EndosymbiontOrganelleSystem(World world) : base(world)
    {
    }

    [Query]
    [All<SpeciesMember, CompoundStorage, BioProcesses, Engulfer, Engulfable, CellProperties>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref TemporaryEndosymbiontInfo endosymbiontInfo, ref OrganelleContainer organelleContainer,
        in Entity entity)
    {
        if (endosymbiontInfo.Applied)
            return;

        // Skip if organelles are not initialized
        if (organelleContainer.Organelles is not { Count: > 0 })
            return;

        if (endosymbiontInfo.EndosymbiontSpeciesPresent != null)
        {
            endosymbiontInfo.CreatedOrganelleInstancesFor ??= new List<Species>();

            var species = entity.Get<SpeciesMember>().Species;

            foreach (var symbiontSpecies in endosymbiontInfo.EndosymbiontSpeciesPresent)
            {
                // Skip already processed species we have an endosymbiont organelle for
                if (endosymbiontInfo.CreatedOrganelleInstancesFor.Contains(symbiontSpecies))
                    continue;

                // When originally creating the symbiont info, it is not yet resolved which organelle type they
                // represent, so we need to find that now
                try
                {
                    var type = species.Endosymbiosis.GetOrganelleTypeForInProgressSymbiosis(symbiontSpecies);
                    CreateNewOrganelle(organelleContainer.Organelles!, type);

                    // These are fetched inside the loop with the assumption that most of the time the loop runs 0
                    // times and when not empty, mostly just once
                    organelleContainer.OnOrganellesChanged(ref entity.Get<CompoundStorage>(),
                        ref entity.Get<BioProcesses>(), ref entity.Get<Engulfer>(), ref entity.Get<Engulfable>(),
                        ref entity.Get<CellProperties>());

                    endosymbiontInfo.CreatedOrganelleInstancesFor.Add(symbiontSpecies);
                }
                catch (Exception e)
                {
                    GD.PrintErr("Error in creating endosymbiont temporary organelle: ", e);
                }
            }
        }

        endosymbiontInfo.Applied = true;
    }

    private void CreateNewOrganelle(OrganelleLayout<PlacedOrganelle> organelles, OrganelleDefinition definition)
    {
        var newOrganelle = new PlacedOrganelle(definition, new Hex(0, 0), 0, null)
        {
            IsEndosymbiont = true,
        };

        // Find the last placed organelle to efficiently find an empty position
        var searchStart = organelles.Organelles[^1].Position;

        var workData1 = hexWorkData;
        var workData2 = hexWorkData2;
        var workData3 = hexWorkData3;

        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
        lock (workData1)
        {
            lock (workData2)
            {
                // Work data 3 is not locked as it is only used when the other two are locked

                // Spiral search for space for the organelle. This will be pretty slow if huge non-player cells are
                // allowed to do this.
                organelles.FindAndPlaceAtValidPosition(newOrganelle, searchStart.Q, searchStart.R, workData1, workData2,
                    workData3);
            }
        }
    }
}
