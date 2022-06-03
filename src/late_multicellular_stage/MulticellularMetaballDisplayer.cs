using System;
using Godot;

/// <summary>
///   Displays a layout of metaballs using multimeshing for more efficient rendering
/// </summary>
public class MulticellularMetaballDisplayer : MultiMeshInstance, IMetaballDisplayer<MulticellularMetaball>
{
    private static readonly MultiMesh MetaballMesh = new MultiMesh()
    {
        Mesh = new SphereMesh()
        {
            Height = 1,
            Radius = 0.5f,
            Material = new SpatialMaterial(),
        },
    };

    public override void _Ready()
    {
        base._Ready();

        Multimesh = MetaballMesh;

        throw new NotImplementedException();
    }
}
