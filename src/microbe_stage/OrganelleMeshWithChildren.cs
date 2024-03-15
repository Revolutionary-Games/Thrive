using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Applies organelle shader parameters to child nodes
/// </summary>
public partial class OrganelleMeshWithChildren : MeshInstance3D
{
    public void GetChildrenMaterials(List<ShaderMaterial> result, bool quiet = false)
    {
        bool found = false;

        foreach (var mesh in GetChildren().OfType<GeometryInstance3D>())
        {
            if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
            {
                result.Add(shaderMaterial);
                found = true;
            }
        }

        if (!found && !quiet)
            GD.PrintErr("Could not find any child geometry instances to get materials from");
    }
}
