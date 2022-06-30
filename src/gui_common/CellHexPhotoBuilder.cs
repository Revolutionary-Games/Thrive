using System;
using Godot;

public class CellHexPhotoBuilder : Spatial, IPhotographable
{
    private float radius;
    private bool radiusDirty;
    private MicrobeSpecies? species;

    public string SceneToPhotographPath => "res://src/gui_common/CellHexPhotoBuilder.tscn";

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
        var builder = (CellHexPhotoBuilder)instancedScene;
        builder.Species = Species;
        builder.BuildHexStruct();
    }

    public float CalculatePhotographDistance(Spatial instancedScene)
    {
        return PhotoStudio.CameraDistanceFromRadiusOfObject(((CellHexPhotoBuilder)instancedScene).Radius *
            Constants.PHOTO_STUDIO_CELL_RADIUS_MULTIPLIER);
    }

    private void BuildHexStruct()
    {
        var hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        var validMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        var modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");

        foreach (Node node in GetChildren())
            node.DetachAndQueueFree();

        if (Species == null)
            throw new InvalidOperationException("Species is ont initialized");

        var organelleLayout = Species.Organelles;
        foreach (var organelle in organelleLayout.Organelles)
        {
            var position = organelle.Position;
            var itemHexes = organelle.RotatedHexes;
            foreach (var hex in itemHexes)
            {
                var pos = Hex.AxialToCartesian(hex + position);

                var hexNode = (MeshInstance)hexScene.Instance();
                AddChild(hexNode);
                hexNode.MaterialOverride = validMaterial;
                hexNode.Translation = pos;
            }
        }

        foreach (var organelle in organelleLayout)
        {
            // Hexes are handled by UpdateAlreadyPlacedHexes

            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                var pos = Hex.AxialToCartesian(organelle.Position) +
                    organelle.Definition.CalculateModelOffset();

                var organelleModel = (SceneDisplayer)modelScene.Instance();
                AddChild(organelleModel);

                organelleModel.Transform = new Transform(
                    MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation), pos);

                organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                    Constants.DEFAULT_HEX_SIZE);

                UpdateOrganellePlaceHolderScene(organelleModel,
                    organelle.Definition.DisplayScene, organelle.Definition, Hex.GetRenderPriority(organelle.Position));
            }
        }
    }

    private void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel,
        string displayScene, OrganelleDefinition definition, int renderPriority)
    {
        organelleModel.Scene = displayScene;
        var material = organelleModel.GetMaterial(definition.DisplaySceneModelPath);
        if (material != null)
        {
            material.RenderPriority = renderPriority;
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
            var itemHexes = organelle.RotatedHexes;
            foreach (var hex in itemHexes)
            {
                var pos = Hex.AxialToCartesian(hex + position);
                farthest = Mathf.Max(farthest, pos.DistanceTo(Vector3.Zero));
            }
        }

        radius = farthest + Constants.DEFAULT_HEX_SIZE;

        radiusDirty = false;
    }
}
