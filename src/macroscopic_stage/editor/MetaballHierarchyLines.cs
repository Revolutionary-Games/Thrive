using System.Collections.Generic;
using Godot;

/// <summary>
///   Creates a visual skeleton based on a metaball layout
/// </summary>
public partial class MetaballHierarchyLines : MultiMeshInstance3D
{
    public void UpdateLines(IReadOnlyCollection<MacroscopicMetaball> layout)
    {
        var mesh = Multimesh;

        int instances = layout.Count - 1;
        mesh.InstanceCount = instances;

        if (instances < 1)
        {
            return;
        }

        int i = 0;

        foreach (var metaball in layout)
        {
            if (metaball.Parent == null)
                continue;

            var basis = Basis.LookingAt(metaball.Parent.Position - metaball.Position)
                .ScaledLocal(new Vector3(1.0f, 1.0f, metaball.Position.DistanceTo(metaball.Parent.Position)));

            mesh.SetInstanceTransform(i,
                new Transform3D(basis, (metaball.Position + metaball.Parent.Position) * 0.5f));

            ++i;
        }
    }
}
