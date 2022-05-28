using Godot;

public class GalleryCardModel : GalleryCard
{
    private ImageTask? imageTask;

    private Texture imageLoadingIcon = null!;

    public override void _Ready()
    {
        base._Ready();

        imageLoadingIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/IconGenerating.png");
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (Asset == null)
            return;

        if (imageTask != null)
        {
            if (imageTask.Finished)
                Thumbnail = imageTask.FinalImage;

            return;
        }

        imageTask = new ImageTask(new ModelPreview(Asset.ResourcePath, Asset.MeshNodePath!));

        PhotoStudio.Instance.SubmitTask(imageTask);

        Thumbnail = imageLoadingIcon;
    }

    public class ModelPreview : IPhotographable
    {
        private string resourcePath = null!;
        private string meshNodePath = null!;

        public ModelPreview(string resourcePath, string meshNodePath)
        {
            this.resourcePath = resourcePath;
            this.meshNodePath = meshNodePath;
        }

        public MeshInstance InstancedMesh { get; private set; } = null!;

        public string SceneToPhotographPath => resourcePath;

        public void ApplySceneParameters(Spatial instancedScene)
        {
            InstancedMesh = instancedScene.GetNode<MeshInstance>(meshNodePath);
        }

        public float CalculatePhotographDistance(Spatial instancedScene)
        {
            return InstancedMesh.GetTransformedAabb().Size.Length();
        }
    }
}
