using System;
using Godot;

/// <summary>
///   A single patch in PatchMapDrawer
/// </summary>
public class PatchMapNode : MarginContainer
{
    [Export]
    public NodePath IconPath = null!;

    // Selected patch node
    [Export]
    public NodePath HighlightPanelPath = null!;

    // Player patch node
    [Export]
    public NodePath MarkPanelPath = null!;

    // for patches adjacent to the selected one
    [Export]
    public NodePath AdjacentPanelPath = null!;

    private TextureRect? iconRect;
    private Panel? highlightPanel;
    private Panel? markPanel;
    private Panel? adjacentHighlightPanel;

    // mouse hover
    private bool highlighted;

    // currently selected node
    private bool selected;

    // current player node
    private bool marked;

    // node adjacent to the selected node
    private bool selectionAdjacent;
    private Texture? patchIcon;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch? Patch { get; set; }

    public Action<PatchMapNode>? SelectCallback { get; set; }

    public Texture? PatchIcon
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
    public bool SelectionAdjacent
    {
        get => selectionAdjacent;
        set
        {
            selectionAdjacent = value;
            UpdateSelectHighlightRing(); 
        }
    }

    public override void _Ready()
    {
        if (Patch == null)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);
        adjacentHighlightPanel = GetNode<Panel>(AdjacentPanelPath);

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
                ((PatchMapDrawer)GetParent()).MarkDirty();
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
        if (highlightPanel == null || adjacentHighlightPanel == null)
            return;
        
        highlightPanel.Visible = Highlighted || Selected;
        adjacentHighlightPanel.Visible = SelectionAdjacent;
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
