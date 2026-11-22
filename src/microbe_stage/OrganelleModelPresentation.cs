using System.Collections.Generic;
using Godot;

/// <summary>
///   Applies the shared model presentation rules for placed organelles, hover models and cell photos.
/// </summary>
/// <remarks>
///   <para>
///     Call on the Godot main thread. Callers own the displayers, their visibility and temporary material lists.
///   </para>
/// </remarks>
public static class OrganelleModelPresentation
{
    public static void ApplyTransform(SceneDisplayer organelleModel, OrganelleTemplate organelle)
    {
        ApplyTransform(organelleModel, organelle.Definition, organelle.Position, organelle.Orientation,
            organelle.Upgrades);
    }

    public static void ApplyTransform(SceneDisplayer organelleModel, OrganelleDefinition definition, Hex position,
        int orientation, IReadOnlyOrganelleUpgrades? upgrades)
    {
        organelleModel.Transform = new Transform3D(new Basis(MathUtils.CreateRotationForOrganelle(orientation)),
            Hex.AxialToCartesian(position) + definition.ModelOffset);

        organelleModel.Scale = definition.GetUpgradesSizeModification(upgrades);
    }

    /// <summary>
    ///   Updates the organelle model displayer to have the specified scene in it and applies material priorities.
    /// </summary>
    public static void ApplySceneAndMaterialPriority(SceneDisplayer organelleModel,
        LoadedSceneWithModelInfo displayScene, int renderPriority, List<ShaderMaterial> temporaryDataHolder)
    {
        organelleModel.Scene = displayScene.LoadedScene;

        temporaryDataHolder.Clear();
        if (!organelleModel.GetMaterial(temporaryDataHolder, displayScene.ModelPath))
        {
            GD.PrintErr("Failed to get material for editor / display cell to update render priority");
            return;
        }

        // To follow MicrobeRenderPrioritySystem, this sets other than the first material to be -1 in priority
        bool first = true;

        foreach (var shaderMaterial in temporaryDataHolder)
        {
            if (first)
            {
                shaderMaterial.RenderPriority = renderPriority;
                first = false;
            }
            else
            {
                shaderMaterial.RenderPriority = renderPriority - 1;
            }
        }
    }
}
