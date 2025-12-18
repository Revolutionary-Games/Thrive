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

        this.QueueFreeChildren();

        if (Species == null)
            throw new InvalidOperationException("Species is not initialized");

        foreach (var cell in Species.CellLayout)
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

        float maxDistanceSquared = 0.0f;

        foreach (var cell in species.CellLayout)
        {
            maxDistanceSquared = MathF.Max(maxDistanceSquared, Hex.AxialToCartesian(cell.Position)
                .DistanceSquaredTo(Vector3.Zero));
        }

        radius = MathF.Sqrt(maxDistanceSquared) + Constants.DEFAULT_HEX_SIZE;

        radiusDirty = false;
    }
}
