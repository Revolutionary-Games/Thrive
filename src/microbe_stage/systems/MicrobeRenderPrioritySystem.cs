namespace Systems;

using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Microbe specific render priority system that is aware of how to set render priority separately for organelles
///   and membrane. Requires <see cref="RenderPriorityOverride"/>
/// </summary>
/// <remarks>
///   <para>
///     This is marked as just reading the materials and organelle containers as this just modifies the materials
///     for them a bit.
///   </para>
/// </remarks>
[With(typeof(RenderPriorityOverride))]
[With(typeof(EntityMaterial))]
[With(typeof(OrganelleContainer))]
[ReadsComponent(typeof(EntityMaterial))]
[ReadsComponent(typeof(OrganelleContainer))]
[RunsAfter(typeof(MicrobeVisualsSystem))]
[RunsAfter(typeof(EntityMaterialFetchSystem))]
[RuntimeCost(0.5f)]
[RunsOnMainThread]
public sealed class MicrobeRenderPrioritySystem : AEntitySetSystem<float>
{
    private readonly List<ShaderMaterial> tempMaterialsList = new();

    public MicrobeRenderPrioritySystem(World world) : base(world, null)
    {
    }

    protected override void Update(float state, in Entity entity)
    {
        ref var renderOrder = ref entity.Get<RenderPriorityOverride>();

        if (renderOrder.RenderPriorityApplied)
            return;

        ref var materials = ref entity.Get<EntityMaterial>();

        // Wait until materials fetched
        if (materials.Materials == null)
            return;

        ref var container = ref entity.Get<OrganelleContainer>();

        if (container.Organelles == null)
        {
            GD.PrintErr("Microbe to have render priority applied doesn't have organelles list");
            renderOrder.RenderPriorityApplied = true;
            return;
        }

        try
        {
            // First material is always the membrane, which gets the base value. Organelles are rendered above it.
            var materialList = materials.Materials;

            if (materialList.Length > 0)
            {
                materialList[0].RenderPriority = renderOrder.RenderPriority;
            }
            else
            {
                GD.PrintErr("Expected microbe materials to have at least one entry for membrane");
            }

            // Rest of the materials list os not used directly as multipart organelles have multiple entries and
            // would cause the indexes to get out of sync

            foreach (var placedOrganelle in container.Organelles.Organelles)
            {
                // Cytoplasm doesn't have graphics so that is naturally skipped, and we can't give a good error
                // message
                if (placedOrganelle.OrganelleGraphics == null)
                    continue;

                // Render priority can be fully calculated from the organelle position and the base render priority
                int organelleRenderOrder =
                    renderOrder.RenderPriority + Hex.GetRenderPriority(placedOrganelle.Position);

                if (placedOrganelle.OrganelleGraphics is OrganelleMeshWithChildren organelleMeshWithChildren)
                {
                    organelleMeshWithChildren.GetChildrenMaterials(tempMaterialsList);

                    foreach (var extraMaterial in tempMaterialsList)
                    {
                        // Sub-graphics have one less render priority
                        extraMaterial.RenderPriority = organelleRenderOrder - 1;
                    }

                    tempMaterialsList.Clear();
                }

                var material =
                    placedOrganelle.OrganelleGraphics.GetMaterial(placedOrganelle.Definition
                        .DisplaySceneModelNodePath);
                material.RenderPriority = organelleRenderOrder;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to apply render priority for entity {entity}: ", e);
        }

        renderOrder.RenderPriorityApplied = true;
    }
}
