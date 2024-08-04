using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

/// <summary>
///   Applies organelle shader parameters to child nodes
/// </summary>
public partial class OrganelleMeshWithChildren : MeshInstance3D
{
#pragma warning disable CA2213
    [Export]
    private Array<GeometryInstance3D>? meshChildren = null!;
#pragma warning restore CA2213

    public void GetChildrenMaterials(List<ShaderMaterial> result, bool quiet = false)
    {
        bool found = false;

        if (meshChildren == null)
        {
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
