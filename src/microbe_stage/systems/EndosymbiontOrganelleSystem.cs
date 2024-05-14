namespace Systems;

using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;

/// <summary>
///   Handles creating temporary organelles for endosymbiosis
/// </summary>
[With(typeof(TemporaryEndosymbiontInfo))]
[With(typeof(OrganelleContainer))]
[With(typeof(SpeciesMember))]
[With(typeof(CompoundStorage))]
[With(typeof(BioProcesses))]
[With(typeof(Engulfer))]
[With(typeof(Engulfable))]
[With(typeof(CellProperties))]
[ReadsComponent(typeof(SpeciesMember))]
[ReadsComponent(typeof(CellProperties))]
[RunsBefore(typeof(MicrobeReproductionSystem))]
[RunsBefore(typeof(MicrobePhysicsCreationAndSizeSystem))]
[RunsBefore(typeof(MicrobeVisualsSystem))]
public class EndosymbiontOrganelleSystem : AEntitySetSystem<float>
{
    // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
    // private readonly ThreadLocal<List<Hex>> hexWorkData = new(() => new List<Hex>());
    // private readonly ThreadLocal<List<Hex>> hexWorkData2 = new(() => new List<Hex>());

    private readonly List<Hex> hexWorkData = new();
    private readonly List<Hex> hexWorkData2 = new();

    public EndosymbiontOrganelleSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner,
        Constants.SYSTEM_NORMAL_ENTITIES_PER_THREAD)
    {
    }

    protected override void Update(float state, in Entity entity)
    {
        ref var endosymbiontInfo = ref entity.Get<TemporaryEndosymbiontInfo>();

        if (endosymbiontInfo.Applied)
            return;

        ref var organelleContainer = ref entity.Get<OrganelleContainer>();

        // Skip if organelles are not initialized
        if (organelleContainer.Organelles is not { Count: > 0 })
            return;

        var species = entity.Get<SpeciesMember>().Species;

        if (endosymbiontInfo.EndosymbiontSpeciesPresent != null)
        {
            endosymbiontInfo.CreatedOrganelleInstancesFor ??= new List<Species>();

            foreach (var symbiontSpecies in endosymbiontInfo.EndosymbiontSpeciesPresent)
            {
                // Skip already processed species we have an endosymbiont organelle for
                if (endosymbiontInfo.CreatedOrganelleInstancesFor.Contains(symbiontSpecies))
                    continue;

                // When originally creating the symbiont info it is not yet resolved which organelle type they
                // represent, so we need to find that now
                try
                {
                    var type = species.Endosymbiosis.GetOrganelleTypeForInProgressSymbiosis(symbiontSpecies);
                    CreateNewOrganelle(organelleContainer.Organelles!, type);

                    // These are fetched inside the loop with the assumption that most of the time the loop runs 0
                    // times and when not empty mostly just once
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
        var newOrganelle = new PlacedOrganelle(definition, new Hex(0, 0), 0, null);

        var workData1 = hexWorkData;
        var workData2 = hexWorkData2;

        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
        lock (workData1)
        {
            lock (workData2)
            {
                // Spiral search for space for the organelle. This will be pretty slow if huge non-player cells are
                // allowed to do this.
                // TODO: https://github.com/Revolutionary-Games/Thrive/issues/3273
                organelles.FindValidPositionForNewOrganelle(newOrganelle, 0, 0, workData1, workData2);
            }
        }
    }
}
