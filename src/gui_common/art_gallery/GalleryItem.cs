using Godot;

public class GalleryItem : Button
{
    [Export]
    public NodePath TitleLabelPath = null!;

    [Export]
    public NodePath TextureRectPath = null!;

    [Export]
    public Texture MissingTexture = null!;

    private Label? titleLabel;
    private TextureRect? imagePreview;
    private string? title;
    private Texture? image;

    [Signal]
    public delegate void OnFullscreenView(GalleryItem item);

    public string Title
    {
        get => title ?? TranslationServer.Translate("UNTITLED");
        set
        {
            title = value;
            UpdatePreview();
        }
    }

    public Texture Image
    {
        get => image ?? MissingTexture;
        set
        {
            image = value;
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

    private void UpdatePreview()
    {
        if (titleLabel == null || imagePreview == null)
            return;

        titleLabel.Text = Title;
        imagePreview.Texture = Image;
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
