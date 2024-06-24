using Godot;

/// <summary>
///   A "card" preview shown in the art gallery
/// </summary>
public partial class GalleryCard : Button
{
    [Export]
    public NodePath? TitleLabelPath;

    [Export]
    public NodePath TextureRectPath = null!;

#pragma warning disable CA2213
    [Export]
    public Texture2D MissingTexture = null!;
#pragma warning restore CA2213

    private readonly NodePath modulationReference = new("modulate");

#pragma warning disable CA2213
    private Label? titleLabel;
    private TextureRect? imagePreview;
    private Texture2D? thumbnail;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnFullscreenViewEventHandler(GalleryCard item);

    /// <summary>
    ///   If this is true, this item can be featured in slideshow.
    /// </summary>
    [Export]
    public bool CanBeShownInASlideshow { get; set; } = true;

    public Asset Asset { get; set; } = null!;

    public Texture2D Thumbnail
    {
        get => thumbnail ?? MissingTexture;
        set
        {
            thumbnail = value;
            UpdatePreview();
        }
    }

    public override void _Ready()
    {
        titleLabel = GetNode<Label>(TitleLabelPath);
        imagePreview = GetNode<TextureRect>(TextureRectPath);

        UpdatePreview();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
        {
            AcceptEvent();

            if (mouse.DoubleClick)
            {
                GUICommon.Instance.PlayButtonPressSound();
                EmitSignal(SignalName.OnFullscreenView, this);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TitleLabelPath != null)
            {
                TitleLabelPath.Dispose();
                TextureRectPath.Dispose();
            }

            modulationReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdatePreview()
    {
        if (titleLabel == null || imagePreview == null)
            return;

        titleLabel.Text = string.IsNullOrEmpty(Asset.Title) ? Asset.FileName : Asset.Title;
        imagePreview.Texture = Thumbnail;
    }

    private void OnMouseEnter()
    {
        var tween = CreateTween();
        tween.TweenProperty(imagePreview, modulationReference, Colors.Gray, 0.5);
    }

    private void OnMouseExit()
    {
        var tween = CreateTween();
        tween.TweenProperty(imagePreview, modulationReference, Colors.White, 0.5);
    }
}
