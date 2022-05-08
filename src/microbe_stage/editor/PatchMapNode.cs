using System;
using Godot;

/// <summary>
///   A single patch in PatchMapDrawer
/// </summary>
public class PatchMapNode : MarginContainer
{
    [Export]
    public NodePath IconPath = null!;

    [Export]
    public NodePath HighlightPanelPath = null!;

    [Export]
    public NodePath MarkPanelPath = null!;

    private TextureRect? iconRect;
    private Panel? highlightPanel;
    private Panel? markPanel;

    private bool highlighted;
    private bool selected;
    private bool marked;
    private bool enabled = true;

    private Texture? patchIcon;

    private bool selectHighlightRingDirty = true;
    private bool iconDirty = true;
    private bool markRingDirty = true;
    private bool grayscaleDirty = true;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch? Patch { get; set; }

    public ShaderMaterial? MonochromeShader { get; set; }

    /// <summary>
    ///   Display the icon in monochrome and make it not selectable or highlightable.
    ///   Setting this to true also removes current selection.
    /// </summary>
    public bool Enabled
    {
        get => enabled;
        set
        {
            if (!value)
                Selected = false;
            enabled = value;
            selectHighlightRingDirty = true;
            grayscaleDirty = true;
        }
    }

    public Action<PatchMapNode>? SelectCallback { get; set; }

    public Texture? PatchIcon
    {
        get => patchIcon;
        set
        {
            if (patchIcon == value)
                return;

            patchIcon = value;
            iconDirty = true;
        }
    }

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;
            selectHighlightRingDirty = true;
        }
    }

    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            selectHighlightRingDirty = true;
        }
    }

    public bool Marked
    {
        get => marked;
        set
        {
            marked = value;
            markRingDirty = true;
        }
    }

    public override void _Ready()
    {
        if (Patch == null)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);
    }

    public override void _Process(float delta)
    {
        if (selectHighlightRingDirty)
            UpdateSelectHighlightRing();

        if (iconDirty)
            UpdateIcon();

        if (markRingDirty)
            UpdateMarkRing();

        if (grayscaleDirty)
            UpdateGrayscale();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Enabled)
            return;

        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
            })
        {
            OnSelect();
            GetTree().SetInputAsHandled();
        }
    }

    public void OnSelect()
    {
        Selected = true;

        SelectCallback?.Invoke(this);
    }

    public void OnMouseEnter()
    {
        Highlighted = true;
    }

    public void OnMouseExit()
    {
        Highlighted = false;
    }

    private void UpdateSelectHighlightRing()
    {
        if (highlightPanel == null)
            return;

        if (Enabled)
        {
            highlightPanel.Visible = Highlighted || Selected;
        }
        else
        {
            highlightPanel.Visible = false;
        }

        selectHighlightRingDirty = false;
    }

    private void UpdateMarkRing()
    {
        if (markPanel == null)
            return;

        markPanel.Visible = Marked;
        markRingDirty = false;
    }

    private void UpdateIcon()
    {
        if (PatchIcon == null || iconRect == null)
            return;

        iconRect.Texture = PatchIcon;
        iconDirty = false;
    }

    private void UpdateGrayscale()
    {
        if (iconRect != null)
            iconRect.Material = Enabled ? null : MonochromeShader;

        grayscaleDirty = false;
    }
}
