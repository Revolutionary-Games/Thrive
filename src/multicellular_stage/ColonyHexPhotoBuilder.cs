using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Generates images of colony hex layouts
/// </summary>
public partial class ColonyHexPhotoBuilder : Node3D, IScenePhotographable
{
    private readonly List<ShaderMaterial> usedMaterials = new();

    private float radius;
    private bool radiusDirty;
    private MulticellularSpecies? species;

    public string SceneToPhotographPath => "res://src/multicellular_stage/ColonyHexPhotoBuilder.tscn";

    public float Radius
    {
        get
        {
            if (radiusDirty)
                CalculateRadius();

            return radius;
        }
    }

    public MulticellularSpecies? Species
    {
        get => species;
        set
        {
            species = value;
            radiusDirty = true;
        }
    }

    public static ulong GetVisualHash(MulticellularSpecies species)
    {
        return Constants.VISUAL_HASH_HEX_LAYOUT ^ species.GetVisualHashCode();
    }

    public void ApplySceneParameters(Node3D instancedScene)
    {
        var builder = (ColonyHexPhotoBuilder)instancedScene;
        builder.Species = Species;
        builder.BuildHexStruct();
    }

    public Vector3 CalculatePhotographDistance(Node3D instancedScene)
    {
        return new Vector3(0, PhotoStudio.CameraDistanceFromRadiusOfObject(
            ((ColonyHexPhotoBuilder)instancedScene).Radius *
            Constants.PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER), 0);
    }

    public ulong GetVisualHashCode()
    {
        return GetVisualHash(species ?? throw new InvalidOperationException("No species set"));
    }

    private void BuildHexStruct()
    {
        var hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        var hexMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        var modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");

        this.QueueFreeChildren();

        if (Species == null)
            throw new InvalidOperationException("Species is not initialized");

        if (Species.EditorCellLayout == null)
        {
            GD.PrintErr("No cell layout is remembered, the hex preview can't be generated");
            return;
        }

        foreach (var cell in Species.EditorCellLayout)
        {
            var pos = Hex.AxialToCartesian(cell.Position);

            var hexNode = hexScene.Instantiate<MeshInstance3D>();
            AddChild(hexNode);
            hexNode.MaterialOverride = hexMaterial;
            hexNode.Position = pos;
        }
    }

    private void CalculateRadius()
    {
        if (species == null)
        {
            radius = 0;
            return;
        }

        float farthest = 0;

        foreach (var cell in species.Cells)
        {
            farthest = MathF.Max(farthest, Hex.AxialToCartesian(cell.Position).DistanceTo(Vector3.Zero));
        }

        radius = farthest + Constants.DEFAULT_HEX_SIZE;

        radiusDirty = false;
    }
}
