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

    private TextureRect? iconRect;
    private Panel? highlightPanel;
    private Panel? markPanel;

    private Color selectionAdjacentColor = new Color(1f, 1f, 1f, 1f);
    private Color playerAdjacentColor = new Color(1f, 1f, 1f, 1f); 
    private Color selectColor = new Color(0.05f, 0.03f, 0.95f, 1f);
    private Color markedColor = new Color(0.02f, 0.8f, 0.95f, 1f);
    // mouse hover
    private bool highlighted;

    // currently selected node
    private bool selected;

    // current player node
    private bool marked;

    // node adjacent to the selected node
    private bool selectionAdjacent;

    // node adjacent to the player node
    private bool playerAdjacent;
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
    public bool PlayerAdjacent
    {
        get => playerAdjacent;
        set
        {
            playerAdjacent = value;
            UpdateMarkRing();
        }
    }

    public override void _Ready()
    {
        if (Patch == null)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);
        var style = markPanel.HasStyleboxOverride("panel");
        var sty = (StyleBoxFlat)markPanel.GetStylebox("panel","");

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

        highlightPanel.Visible = Highlighted || Selected || SelectionAdjacent;

        var styleBox = (StyleBoxFlat)highlightPanel.GetStylebox("panel","");
        if (SelectionAdjacent)
        {
            styleBox.BgColor = selectionAdjacentColor;
        }
        else
        {
            styleBox.BgColor = selectColor;
        }
        highlightPanel.AddStyleboxOverride("panel", styleBox);
    }

    private void UpdateMarkRing()
    {
        if (markPanel == null)
            return;

        markPanel.Visible = Marked || PlayerAdjacent;

        var styleBox = (StyleBoxFlat)markPanel.GetStylebox("panel","");
        if (PlayerAdjacent)
        {
            styleBox.BgColor = playerAdjacentColor;
        }
        else
        {
            styleBox.BgColor = markedColor;
        }
        markPanel.AddStyleboxOverride("panel", styleBox);
    }



    private void UpdateIcon()
    {
        if (PatchIcon == null || iconRect == null)
            return;

        iconRect.Texture = PatchIcon;
    }
}
