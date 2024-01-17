using System.Collections.Generic;
using Godot;

/// <summary>
///   Applies organelle shader parameters to child nodes
/// </summary>
public class OrganelleMeshWithChildren : MeshInstance
{
    public void GetChildrenMaterials(List<ShaderMaterial> result)
    {
        foreach (GeometryInstance mesh in GetChildren())
        {
            if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
            {
                result.Add(shaderMaterial);
            }
        }
    }
}
