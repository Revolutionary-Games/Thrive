using Godot;

public partial class GalleryCardModel : GalleryCard
{
    private ImageTask? imageTask;

#pragma warning disable CA2213
    private Texture2D imageLoadingIcon = null!;
#pragma warning restore CA2213

    private bool finishedLoadingImage;

    public override void _Ready()
    {
        base._Ready();

        imageLoadingIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/IconGenerating.png");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (finishedLoadingImage)
            return;

        if (imageTask != null)
        {
            if (imageTask.Finished)
            {
                Thumbnail = imageTask.FinalImage;
                finishedLoadingImage = true;
            }

            return;
        }

        imageTask = new ImageTask(new ModelPreview(Asset.ResourcePath, Asset.MeshNodePath!));

        PhotoStudio.Instance.SubmitTask(imageTask);

        Thumbnail = imageLoadingIcon;
    }

    public class ModelPreview : IScenePhotographable
    {
        private string resourcePath;
        private NodePath? meshNodePath;

        public ModelPreview(string resourcePath, NodePath? meshNodePath)
        {
            this.resourcePath = resourcePath;
            this.meshNodePath = meshNodePath;
        }

        public string SceneToPhotographPath => resourcePath;

        public void ApplySceneParameters(Node3D instancedScene)
        {
        }

        public Vector3 CalculatePhotographDistance(Node3D instancedScene)
        {
            MeshInstance3D instancedMesh;

            if (meshNodePath != null)
            {
                instancedMesh = instancedScene.GetNode<MeshInstance3D>(meshNodePath);
            }
            else
            {
                instancedMesh = (MeshInstance3D)instancedScene;
            }

            return new Vector3(0, instancedMesh.GetTransformedAabb().Size.Length(), 0);
        }
    }
}
