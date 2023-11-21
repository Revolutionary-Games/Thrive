using System;
using Godot;

public class CellHexesPhotoBuilder : Spatial, IScenePhotographable
{
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

    public void ApplySceneParameters(Spatial instancedScene)
    {
        var builder = (CellHexesPhotoBuilder)instancedScene;
        builder.Species = Species;
        builder.BuildHexStruct();
    }

    public float CalculatePhotographDistance(Spatial instancedScene)
    {
        return PhotoStudio.CameraDistanceFromRadiusOfObject(((CellHexesPhotoBuilder)instancedScene).Radius *
            Constants.PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER);
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

                var hexNode = (MeshInstance)hexScene.Instance();
                AddChild(hexNode);
                hexNode.MaterialOverride = hexMaterial;
                hexNode.Translation = pos;
            }
        }

        foreach (var organelle in organelleLayout)
        {
            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                var organelleModel = (SceneDisplayer)modelScene.Instance();
                AddChild(organelleModel);

                CellEditorComponent.UpdateOrganelleDisplayerTransform(organelleModel, organelle);

                CellEditorComponent.UpdateOrganellePlaceHolderScene(organelleModel,
                    organelle.Definition.DisplayScene, organelle.Definition, Hex.GetRenderPriority(organelle.Position));
            }
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
