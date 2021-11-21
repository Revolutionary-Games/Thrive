using System;
using Godot;

/// <summary>
///   A single patch in PatchMapDrawer
/// </summary>
public class PatchMapNode : MarginContainer
{
    [Export]
    public NodePath IconPath;

    [Export]
    public NodePath HighlightPanelPath;

    [Export]
    public NodePath MarkPanelPath;

    private TextureRect iconRect;
    private Panel highlightPanel;
    private Panel markPanel;

    private bool highlighted;
    private bool selected;
    private bool marked;

    private Texture patchIcon;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch Patch { get; set; }

    public ShaderMaterial MonochromeShader { get; set; }

    /// <summary>
    ///   Display the icon in monochrome and make it not selectable or highlightable.
    ///   Setting this to true also removes current selection.
    /// </summary>
    public bool Enabled
    {
        get => iconRect.Material == null;
        set
        {
            iconRect.Material = value ? null : MonochromeShader;
            if (!value)
                Selected = false;
            UpdateSelectHighlightRing();
        }
    }

    public Action<PatchMapNode> SelectCallback { get; set; }

    public Texture PatchIcon
    {
        get => patchIcon;
        set
        {
            if (patchIcon == value)
                return;

            patchIcon = value;
            UpdateIcon();
        }
    }

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;
            UpdateSelectHighlightRing();
        }
    }

    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            UpdateSelectHighlightRing();
        }
    }

    public bool Marked
    {
        get => marked;
        set
        {
            marked = value;
            UpdateMarkRing();
        }
    }

    public override void _Ready()
    {
        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);

        UpdateSelectHighlightRing();
        UpdateMarkRing();
        UpdateIcon();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Enabled)
            return;

        if (@event is InputEventMouseButton mouse)
        {
            if (mouse.Pressed)
            {
                OnSelect();
                AcceptEvent();
            }
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
    }

    private void UpdateMarkRing()
    {
        if (markPanel == null)
            return;

        markPanel.Visible = Marked;
    }

    private void UpdateIcon()
    {
        if (PatchIcon == null || iconRect == null)
            return;

        iconRect.Texture = PatchIcon;
    }
}
