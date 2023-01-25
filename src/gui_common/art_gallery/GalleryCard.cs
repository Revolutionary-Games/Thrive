using Godot;

public class GalleryCard : Button
{
    [Export]
    public NodePath? TitleLabelPath;

    [Export]
    public NodePath TextureRectPath = null!;

#pragma warning disable CA2213
    [Export]
    public Texture MissingTexture = null!;

    private Label? titleLabel;
    private TextureRect? imagePreview;
    private Texture? thumbnail;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnFullscreenView(GalleryCard item);

    /// <summary>
    ///   If this is true, this item can be featured in slideshow.
    /// </summary>
    [Export]
    public bool CanBeShownInASlideshow { get; set; } = true;

    public Asset Asset { get; set; } = null!;

    public Texture Thumbnail
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
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: (int)ButtonList.Left } mouse)
        {
            AcceptEvent();

            if (mouse.Doubleclick)
            {
                GUICommon.Instance.PlayButtonPressSound();
                EmitSignal(nameof(OnFullscreenView), this);
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
        GUICommon.Instance.Tween.InterpolateProperty(imagePreview, "modulate", null, Colors.Gray, 0.5f);
        GUICommon.Instance.Tween.Start();
    }

    private void OnMouseExit()
    {
        GUICommon.Instance.Tween.InterpolateProperty(imagePreview, "modulate", null, Colors.White, 0.5f);
        GUICommon.Instance.Tween.Start();
    }
}
