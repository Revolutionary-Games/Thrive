using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

// See below why this is disabled
#if DEBUG && DISABLED
using System.Diagnostics;
#endif

/// <summary>
///   Applies organelle shader parameters to child nodes
/// </summary>
public partial class OrganelleMeshWithChildren : MeshInstance3D
{
#pragma warning disable CA2213
    /// <summary>
    ///   If set, overrides the default behaviour for finding the mesh children
    /// </summary>
    [Export]
    private Array<GeometryInstance3D>? meshChildren;
#pragma warning restore CA2213

    public void GetChildrenMaterials(List<ShaderMaterial> result, bool quiet = false)
    {
        bool found = false;

        if (meshChildren == null || meshChildren.Count < 1)
        {
            // This check cannot be enabled because apparently Godot editor really hates being able to set the value
            // to null so for now this check is not enabled as fixing it requires manual editing of a file and not
            // using the Godot editor
#if DEBUG && DISABLED
            if (meshChildren != null)
            {
                GD.PrintErr("Mesh children is set but it is empty, this wastes an object allocation unnecessarily");
                GD.PrintErr($"Problematic node with mesh children property: {this}");

                if (IsInsideTree())
                {
                    GD.PrintErr("Node is at: ", GetPath());
                }

                if (Debugger.IsAttached)
                    Debugger.Break();

                return;
            }
#endif

            foreach (var mesh in GetChildren().OfType<GeometryInstance3D>())
            {
                if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
                {
                    result.Add(shaderMaterial);
                    found = true;
                }
            }
        }
        else
        {
            foreach (var mesh in meshChildren)
            {
                if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
                {
                    result.Add(shaderMaterial);
                    found = true;
                }
            }
        }

        if (!found && !quiet)
            GD.PrintErr("Could not find any child geometry instances to get materials from");
    }
}
