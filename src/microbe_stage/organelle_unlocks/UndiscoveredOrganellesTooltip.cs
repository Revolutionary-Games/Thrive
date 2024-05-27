﻿using Godot;

/// <summary>
///   The tooltip for undiscovered organelles
/// </summary>
public partial class UndiscoveredOrganellesTooltip : Control, ICustomToolTip
{
    [Export]
    public NodePath? NameLabelPath;

    [Export]
    public NodePath UnlockTextPath = null!;

#pragma warning disable CA2213
    private Label? nameLabel;
    private CustomRichTextLabel? unlockTextLabel;
#pragma warning restore CA2213

    private string? displayName;
    private LocalizedStringBuilder? unlockText;

    [Export]
    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public string DisplayName
    {
        get => displayName ?? "UndiscoveredOrganelleTooltip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    public string? Description { get; set; }

    public LocalizedStringBuilder? UnlockText
    {
        get => unlockText;
        set
        {
            unlockText = value;
            UpdateUnlockText();
        }
    }

    public override void _Ready()
    {
        unlockTextLabel = GetNode<CustomRichTextLabel>(UnlockTextPath);
        nameLabel = GetNode<Label>(NameLabelPath);

        UpdateName();
        UpdateUnlockText();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NameLabelPath != null)
            {
                NameLabelPath.Dispose();
                UnlockTextPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = nameLabel.Text;
        }
        else
        {
            nameLabel.Text = displayName;
        }
    }

    private void UpdateUnlockText()
    {
        if (unlockTextLabel == null)
            return;

        unlockTextLabel.Visible = unlockText != null;
        if (unlockText != null)
            unlockTextLabel.ExtendedBbcode = unlockText.ToString();
    }

    private void OnTranslationsChanged()
    {
        UpdateName();
        UpdateUnlockText();
    }
}
