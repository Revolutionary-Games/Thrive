using Godot;

/// <summary>
///   Applies the tint to the defined children
/// </summary>
public class OrganelleMeshWithChildren : MeshInstance
{
	public void SetTintOfChildren(Color value)
	{
		foreach (GeometryInstance mesh in GetChildren())
		{
			if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
			{
				shaderMaterial.SetShaderParam("tint", value);
			}
		}
	}

	public void SetDissolveEffectOfChildren(float value)
	{
		foreach (GeometryInstance mesh in GetChildren())
		{
			if (mesh.MaterialOverride is ShaderMaterial shaderMaterial)
			{
				shaderMaterial.SetShaderParam("dissolveValue", value);
			}
		}
	}
}
