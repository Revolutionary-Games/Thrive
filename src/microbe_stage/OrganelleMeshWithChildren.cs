using System.Collections.Generic;
using Godot;

/// <summary>
///   Applies organelle shader parameters to child nodes
/// </summary>
public partial class OrganelleMeshWithChildren : MeshInstance3D
{
    public void GetChildrenMaterials(List<ShaderMaterial> result)
    {
        foreach (GeometryInstance3D mesh in GetChildren())
        {
            if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
            {
                result.Add(shaderMaterial);
            }
        }
    }
}
