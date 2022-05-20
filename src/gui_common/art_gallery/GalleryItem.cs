using System;
using Godot;
using Asset = Gallery.Asset;

public class GalleryItem : Button
{
    [Export]
    public NodePath TitleLabelPath = null!;

    [Export]
    public NodePath TextureRectPath = null!;

    [Export]
    public NodePath InspectButtonPath = null!;

    private Label titleLabel = null!;
    private TextureRect imagePreview = null!;

    private Asset asset = null!;

    public event EventHandler<GalleryItemSelectedCallbackData>? OnFullscreenView;

    public Asset Asset
    {
        get => asset;
        set
        {
            asset = value;
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
                OnFullscreenView?.Invoke(this, new GalleryItemSelectedCallbackData(asset));
            }
        }
    }

    private void UpdatePreview()
    {
        if (asset == null || titleLabel == null)
            return;

        titleLabel.Text = string.IsNullOrEmpty(asset.Title) ? TranslationServer.Translate("UNTITLED") : asset.Title;
        imagePreview.Texture = GD.Load(asset.ResourcePath) as Texture;
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

public class GalleryItemSelectedCallbackData : EventArgs
{
    public GalleryItemSelectedCallbackData(Asset asset)
    {
        Asset = asset;
    }

    public Asset Asset { get; set; }
}
