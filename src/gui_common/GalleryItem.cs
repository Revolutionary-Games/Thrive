using System;
using Godot;
using Asset = Gallery.Asset;

public class GalleryItem : Button
{
    [Export]
    public NodePath TitleLabelPath;

    [Export]
    public NodePath TextureRectPath;

    [Export]
    public NodePath InspectButtonPath;

    private Label titleLabel;
    private TextureRect imagePreview;
    private TextureButton inspectButton;

    private Asset asset;

    public event EventHandler<GalleryItemSelectedCallbackData> OnFullscreenView;

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
        inspectButton = GetNode<TextureButton>(InspectButtonPath);

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (asset == null || titleLabel == null || inspectButton == null)
            return;

        titleLabel.Text = string.IsNullOrEmpty(asset.Title) ? TranslationServer.Translate("UNTITLED") : asset.Title;
        imagePreview.Texture = GD.Load(asset.ResourcePath) as Texture;
    }

    private void OnMouseEnter()
    {
        inspectButton.Show();

        GUICommon.Instance.Tween.InterpolateProperty(imagePreview, "modulate", null, Colors.Gray, 0.5f);
        GUICommon.Instance.Tween.Start();
    }

    private void OnMouseExit()
    {
        inspectButton.Hide();

        GUICommon.Instance.Tween.InterpolateProperty(imagePreview, "modulate", null, Colors.White, 0.5f);
        GUICommon.Instance.Tween.Start();
    }

    private void OnInspectPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        OnFullscreenView.Invoke(this, new GalleryItemSelectedCallbackData(asset));
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
