using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   PhotoStudio related class for displaying cell hexes for photographing
/// </summary>
public partial class CellHexesPhotoBuilder : Node3D, IScenePhotographable
{
    private readonly List<ShaderMaterial> usedMaterials = new();

    private float radius;
    private bool radiusDirty;
    private MicrobeSpecies? species;

    public string SceneToPhotographPath => "res://src/microbe_stage/CellHexesPhotoBuilder.tscn";

    public float Radius
    {
        get
        {
            if (radiusDirty)
                CalculateRadius();

            return radius;
        }
    }

    public MicrobeSpecies? Species
    {
        get => species;
        set
        {
            species = value;
            radiusDirty = true;
        }
    }

    public void ApplySceneParameters(Node3D instancedScene)
    {
        var builder = (CellHexesPhotoBuilder)instancedScene;
        builder.Species = Species;
        builder.BuildHexStruct();
    }

    public Vector3 CalculatePhotographDistance(Node3D instancedScene)
    {
        return new Vector3(0, PhotoStudio.CameraDistanceFromRadiusOfObject(
            ((CellHexesPhotoBuilder)instancedScene).Radius *
            Constants.PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER), 0);
    }

    private void BuildHexStruct()
    {
        var hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        var hexMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        var modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");

        this.QueueFreeChildren();

        if (Species == null)
            throw new InvalidOperationException("Species is not initialized");

        // TODO: The code below is partly duplicate to CellEditorComponent and HexEditorComponentBase.
        // If that code is changed this needs changes too.
        var organelleLayout = Species.Organelles.Organelles;
        foreach (var organelle in organelleLayout)
        {
            var position = organelle.Position;
            foreach (var hex in organelle.RotatedHexes)
            {
                var pos = Hex.AxialToCartesian(hex + position);

                var hexNode = hexScene.Instantiate<MeshInstance3D>();
                AddChild(hexNode);
                hexNode.MaterialOverride = hexMaterial;
                hexNode.Position = pos;
            }
        }

        foreach (var organelle in organelleLayout)
        {
            // Model of the organelle
            if (!organelle.Definition.TryGetGraphicsScene(organelle.Upgrades, out var sceneWithModelInfo))
                continue;

            var organelleModel = modelScene.Instantiate<SceneDisplayer>();
            AddChild(organelleModel);

            CellEditorComponent.UpdateOrganelleDisplayerTransform(organelleModel, organelle);

            CellEditorComponent.UpdateOrganellePlaceHolderScene(organelleModel,
                sceneWithModelInfo, Hex.GetRenderPriority(organelle.Position), usedMaterials);
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

        foreach (var organelle in species.Organelles)
        {
            var position = organelle.Position;
            foreach (var hex in organelle.RotatedHexes)
            {
                var pos = Hex.AxialToCartesian(hex + position);
                farthest = Mathf.Max(farthest, pos.DistanceTo(Vector3.Zero));
            }
        }

        radius = farthest + Constants.DEFAULT_HEX_SIZE;

        radiusDirty = false;
    }
}
