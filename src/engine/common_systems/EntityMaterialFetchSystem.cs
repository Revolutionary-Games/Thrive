namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Fetches the materials for <see cref="EntityMaterial"/> that have auto fetch on
/// </summary>
[ReadsComponent(typeof(SpatialInstance))]
[ReadsComponent(typeof(SpatialInstance))]
[RunsAfter(typeof(PathBasedSceneLoader))]
[RunsAfter(typeof(PredefinedVisualLoaderSystem))]
[RuntimeCost(0.75f)]
[RunsOnMainThread]
public partial class EntityMaterialFetchSystem : BaseSystem<World, float>
{
    private readonly List<ShaderMaterial> tempMaterialFetchList = new();

    // TODO: determine if it is thread safe to fetch Godot materials
    public EntityMaterialFetchSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref EntityMaterial materialComponent, in Entity entity)
    {
        if (materialComponent.MaterialFetchPerformed || materialComponent.Materials != null)
            return;

        if (materialComponent.AutoRetrieveFromSpatial)
        {
            try
            {
                ref var spatial = ref entity.Get<SpatialInstance>();

                // Wait until spatial gets an instance
                if (spatial.GraphicalInstance == null)
                    return;

                var nodeToFetchFrom = spatial.GraphicalInstance;

                if (!materialComponent.AutoRetrieveAssumesNodeIsDirectlyAttached)
                {
                    nodeToFetchFrom = nodeToFetchFrom.GetChild<Node3D>(0);
                }

                if (string.IsNullOrEmpty(materialComponent.AutoRetrieveModelPath))
                {
                    nodeToFetchFrom.GetMaterial(tempMaterialFetchList);
                }
                else
                {
                    using var nodePath = new NodePath(materialComponent.AutoRetrieveModelPath);
                    nodeToFetchFrom.GetMaterial(tempMaterialFetchList, nodePath);
                }

                materialComponent.Materials = tempMaterialFetchList.ToArray();
                tempMaterialFetchList.Clear();

                if (materialComponent.Materials is not { Length: > 0 } || materialComponent.Materials[0] == null)
                {
                    throw new NullReferenceException(
                        "Expected material not set, this component doesn't wait for material to be set later");
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Entity with material auto retrieve from spatial cannot access " +
                    "spatial component's material: ", e);
            }
        }

        materialComponent.MaterialFetchPerformed = true;
    }
}
