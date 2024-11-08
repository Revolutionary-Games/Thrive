﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   A single patch in PatchMapDrawer
/// </summary>
public partial class PatchMapNode : MarginContainer
{
    [Export]
    public NodePath? IconPath;

    /// <summary>
    ///   Selected patch graphics
    /// </summary>
    [Export]
    public NodePath HighlightPanelPath = null!;

    /// <summary>
    ///   Player patch graphics
    /// </summary>
    [Export]
    public NodePath MarkPanelPath = null!;

    /// <summary>
    ///   For patches adjacent to the selected one
    /// </summary>
    [Export]
    public NodePath AdjacentPanelPath = null!;

    [Export]
    public NodePath UnknownLabelPath = null!;

    [Export]
    public string UnknownTextureFilePath = null!;

    // TODO: Move this to Constants.cs
    private const float HalfBlinkInterval = 0.5f;

#pragma warning disable CA2213
    [Export]
    private TextureRect eruptionEventIndicator = null!;

    private TextureRect? iconRect;
    private Panel? highlightPanel;
    private Panel? markPanel;
    private Panel? adjacentHighlightPanel;
    private Label? unknownLabel;

    private Texture2D? patchIcon;
#pragma warning restore CA2213

    /// <summary>
    ///   True if mouse is hovering on this node
    /// </summary>
    private bool highlighted;

    /// <summary>
    ///   True if the current node is selected
    /// </summary>
    private bool selected;

    /// <summary>
    ///   True if player is in the current node
    /// </summary>
    private bool marked;

    /// <summary>
    ///   True if player can move to this patch
    /// </summary>
    private bool enabled = true;

    /// <summary>
    ///   True if the patch is adjacent to the selected patch
    /// </summary>
    private bool adjacentToSelectedPatch;

    private double currentBlinkTime;

    private Patch? patch;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch Patch
    {
        get => patch ?? throw new InvalidOperationException("Patch not set yet");
        set => patch = value;
    }

    public MapElementVisibility Visibility
    {
        get => Patch.Visibility;
        set => Patch.ApplyVisibility(value);
    }

    /// <summary>
    ///   True when this has been selected and the <see cref="PatchMapDrawer"/> has not reacted yet.
    /// </summary>
    public bool SelectionDirty { get; private set; }

    public ShaderMaterial? MonochromeMaterial { get; set; }

    public Action<PatchMapNode>? SelectCallback { get; set; }

    /// <summary>
    ///   Display the icon in color and make it highlightable/selectable.
    ///   Setting this to false removes current selection.
    /// </summary>
    public bool Enabled
    {
        get => enabled;
        set
        {
            Selected = Selected && value;

            enabled = value;

            UpdateSelectHighlightRing();
            UpdateGreyscale();
        }
    }

    public Texture2D? PatchIcon
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
            if (selected == value)
                return;

            selected = value;

            // Make sure any highlighted lines / other patches also react correctly to this becoming deselected
            if (!selected)
                SelectionDirty = true;

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

    public bool AdjacentToSelectedPatch
    {
        get => adjacentToSelectedPatch;
        set
        {
            adjacentToSelectedPatch = value;
            UpdateSelectHighlightRing();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (patch == null)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);
        adjacentHighlightPanel = GetNode<Panel>(AdjacentPanelPath);
        unknownLabel = GetNode<Label>(UnknownLabelPath);

        UpdateSelectHighlightRing();
        UpdateMarkRing();
        UpdateIcon();
        UpdateGreyscale();

        UpdateVisibility();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        currentBlinkTime += delta;
        if (currentBlinkTime > HalfBlinkInterval)
        {
            currentBlinkTime = 0;

            if (Marked)
                markPanel!.Visible = !markPanel.Visible;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Enabled)
            return;

        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right,
            })
        {
            SelectionDirty = true;
            OnSelect();
            GetViewport().SetInputAsHandled();
        }
    }

    public void UpdateSelectionState()
    {
        SelectionDirty = false;
        UpdateSelectHighlightRing();
    }

    public void UpdateVisibility()
    {
        if (unknownLabel == null)
            throw new InvalidOperationException("Not initialized yet");

        switch (Visibility)
        {
            case MapElementVisibility.Hidden:
                Hide();
                return;

            case MapElementVisibility.Unknown:
            {
                Show();
                unknownLabel.Show();

                // TODO: would it help anything to persistently load the unknown texture (instead of each time here)?
                PatchIcon = GD.Load<Texture2D>(UnknownTextureFilePath);
                return;
            }

            case MapElementVisibility.Shown:
            {
                Show();
                unknownLabel.Hide();
                PatchIcon = patch!.BiomeTemplate.LoadedIcon;
                return;
            }
        }
    }

    public void ShowEventVisuals(IReadOnlyList<WorldEffectVisuals> list)
    {
        eruptionEventIndicator.Visible = false;

        var count = list.Count;

        for (int i = 0; i < count; ++i)
        {
            switch (list[i])
            {
                case WorldEffectVisuals.None:
                    break;
                case WorldEffectVisuals.UnderwaterVentEruption:
                    eruptionEventIndicator.Visible = true;
                    break;
                default:
                    GD.PrintErr($"Unknown event to display on patch map node: {list[i]}");
                    break;
            }
        }
    }

    public void OnSelect()
    {
        Selected = true;

        if (Selected)
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (IconPath != null)
            {
                IconPath.Dispose();
                HighlightPanelPath.Dispose();
                MarkPanelPath.Dispose();
                AdjacentPanelPath.Dispose();
                UnknownLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateSelectHighlightRing()
    {
        if (highlightPanel == null || adjacentHighlightPanel == null)
            return;

        if (Enabled)
        {
            highlightPanel.Visible = Highlighted || Selected;
            adjacentHighlightPanel.Visible = AdjacentToSelectedPatch;
        }
        else
        {
            highlightPanel.Visible = false;
            adjacentHighlightPanel.Visible = false;
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

    private void UpdateGreyscale()
    {
        if (iconRect == null)
            return;

        iconRect.Material = Enabled ? null : MonochromeMaterial;
    }
}
