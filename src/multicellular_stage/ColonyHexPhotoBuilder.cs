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
        ulong hash = Constants.VISUAL_HASH_HEX_LAYOUT;

        foreach (var cell in species.EditorCells)
        {
            unchecked
            {
                var isCavity = cell.Data?.IsCavity == true;
                ulong posHash = (ulong)cell.Position.GetHashCode();

                ulong cellHash = posHash * (isCavity ? 0x9E3779B97F4A7C15UL : 0xC6A4A7935BD1E995UL);
                cellHash ^= cellHash >> 32;
                cellHash *= isCavity ? 0xBF58476D1CE4E5B9UL : 0x94D049BB133111EBUL;

                hash += cellHash;
            }
        }

        return hash;
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
        var cavityMaterial = GD.Load<Material>("res://src/multicellular_stage/editor/CavityHex.tres");

        this.QueueFreeChildren();

        if (Species == null)
            throw new InvalidOperationException("Species is not initialized");

        foreach (var cell in Species.EditorCells)
        {
            var pos = Hex.AxialToCartesian(cell.Position);

            var hexNode = hexScene.Instantiate<MeshInstance3D>();
            AddChild(hexNode);
            hexNode.MaterialOverride = cell.Data?.IsCavity == true ? cavityMaterial : hexMaterial;
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

        foreach (var cell in species.EditorCells)
        {
            maxDistanceSquared = MathF.Max(maxDistanceSquared, Hex.AxialToCartesian(cell.Position)
                .DistanceSquaredTo(Vector3.Zero));
        }

        radius = MathF.Sqrt(maxDistanceSquared) + Constants.DEFAULT_HEX_SIZE;

        radiusDirty = false;
    }
}
