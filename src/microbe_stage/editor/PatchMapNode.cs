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

    private bool highlighted = false;
    private bool selected = false;
    private bool marked = false;

    private Texture patchIcon;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch Patch { get; set; }

    public Action<PatchMapNode> SelectCallback { get; set; }

    public Texture PatchIcon
    {
        get
        {
            return patchIcon;
        }
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
        get
        {
            return highlighted;
        }
        set
        {
            highlighted = value;
            UpdateSelectHighlightRing();
        }
    }

    public bool Selected
    {
        get
        {
            return selected;
        }
        set
        {
            selected = value;
            UpdateSelectHighlightRing();
        }
    }

    public bool Marked
    {
        get
        {
            return marked;
        }
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

        if (SelectCallback != null)
            SelectCallback(this);
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

        highlightPanel.Visible = Highlighted || Selected;
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
