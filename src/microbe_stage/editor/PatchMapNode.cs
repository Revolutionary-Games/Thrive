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

    public override void _Ready()
    {
        if (Patch == null)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);

        UpdateSelectHighlightRing();
        UpdateMarkRing();
        UpdateIcon();
    }

    public override void _GuiInput(InputEvent @event)
    {
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
