namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Updates visual positions of entities for rendering by Godot
/// </summary>
[ReadsComponent(typeof(WorldPosition))]
[RuntimeCost(36)]
[RunsOnMainThread]
public partial class SpatialPositionSystem : BaseSystem<World, float>
{
    public SpatialPositionSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref SpatialInstance spatial, ref WorldPosition position)
    {
        if (spatial.GraphicalInstance == null)
            return;

        if (spatial.ApplyVisualScale)
        {
            spatial.GraphicalInstance.Transform =
                new Transform3D(new Basis(position.Rotation).Scaled(spatial.VisualScale), position.Position);
        }
        else
        {
            spatial.GraphicalInstance.Transform =
                new Transform3D(new Basis(position.Rotation), position.Position);
        }
    }
}
