using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   The main tooltip class for the selections on the microbe editor's selection menu.
///   Contains list of processes and modifiers info.
/// </summary>
public partial class SelectionMenuToolTip : ControlWithInput, ICustomToolTip
{
    /// <summary>
    ///   Hold reference of modifier info elements for easier access to change their values later
    /// </summary>
    private readonly List<ModifierInfoLabel> modifierInfos = new();

#pragma warning disable CA2213
    [Export]
    private VBoxContainer modifierInfoList = null!;

    private PackedScene modifierInfoScene = null!;
    private LabelSettings noProcessesFont = null!;
    private LabelSettings processTitleFont = null!;

    // TODO: these can probably be changed to be non-nullable with the Godot 4 upgrade now allowing directly setting
    // these
    [Export]
    private Label? nameLabel;

    [Export]
    private Label? mpLabel;

    [Export]
    private Label? requiresNucleusLabel;

    [Export]
    private ModifierInfoLabel? osmoregulationModifier;

    [Export]
    private CustomRichTextLabel? descriptionLabel;

    [Export]
    private CustomRichTextLabel? processesDescriptionLabel;

    [Export]
    private ProcessList processList = null!;

    [Export]
    private VBoxContainer? moreInfo;
#pragma warning restore CA2213

    private string? displayName;
    private string? description;
    private string processesDescription = string.Empty;
    private int mpCost;
    private float osmoregulationCost;
    private bool showOsmoregulation = true;
    private bool requiresNucleus;
    private string? thriveopediaPageName;

    [Export]
    public string DisplayName
    {
        get => displayName ?? "SelectionMenuToolTip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    /// <summary>
    ///   Description of processes an organelle does if any.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: description string should only be set here and not directly on the rich text label node
    ///     as it will be overridden otherwise.
    ///   </para>
    /// </remarks>
    [Export]
    public string ProcessesDescription
    {
        get => processesDescription;
        set
        {
            processesDescription = value;
            UpdateProcessesDescription();
        }
    }

    /// <summary>
    ///   General description of the selectable.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: description string should only be set here and not directly on the rich text label node
    ///     as it will be overridden otherwise.
    ///   </para>
    /// </remarks>
    [Export]
    public string? Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    [Export]
    public int MutationPointCost
    {
        get => mpCost;
        set
        {
            mpCost = value;
            UpdateMpCost();
        }
    }

    [Export]
    public float OsmoregulationCost
    {
        get => osmoregulationCost;
        set
        {
            osmoregulationCost = value;
            UpdateOsmoregulationCost();
        }
    }

    /// <summary>
    ///   If set to false hides the osmoregulation cost section of this tooltip.
    /// </summary>
    [Export]
    public bool ShowOsmoregulation
    {
        get => showOsmoregulation;
        set
        {
            if (showOsmoregulation == value)
                return;

            showOsmoregulation = value;
            UpdateOsmoregulationCost();
        }
    }

    [Export]
    public bool RequiresNucleus
    {
        get => requiresNucleus;
        set
        {
            requiresNucleus = value;
            UpdateRequiresNucleus();
        }
    }

    [Export]
    public string? ThriveopediaPageName
    {
        get => thriveopediaPageName;
        set
        {
            thriveopediaPageName = value;
            UpdateMoreInfo();
        }
    }

    [Export]
    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        modifierInfoScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/tooltips/ModifierInfoLabel.tscn");
        noProcessesFont = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Bold-Smaller.tres");
        processTitleFont = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Bold-Smaller-Gold.tres");

        UpdateName();
        UpdateDescription();
        UpdateProcessesDescription();
        UpdateMpCost();
        UpdateRequiresNucleus();
        UpdateLists();
        UpdateMoreInfo();

        // Apply initial hidden state to osmoregulation if it was applied before this was put into the scene tree
        // TODO: make also other properties work before this is added to the scene tree to not cause surprise problems
        if (!ShowOsmoregulation)
            UpdateOsmoregulationCost();
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

    /// <summary>
    ///   Instances the UI element for a modifier info
    /// </summary>
    public void AddModifierInfo(string name, string value, float valueForColourApplying = 0,
        string? iconPath = null, StringName? nodeName = null)
    {
        var modifierInfo = modifierInfoScene.Instantiate<ModifierInfoLabel>();

        modifierInfo.DisplayName = name;
        if (nodeName != null)
            modifierInfo.Name = nodeName;

        modifierInfo.ModifierValue = value;

        modifierInfo.AdjustValueColor(valueForColourApplying);
        modifierInfo.ModifierIcon = string.IsNullOrEmpty(iconPath) ? null : GD.Load<Texture2D>(iconPath);

        modifierInfoList.AddChild(modifierInfo);
        modifierInfos.Add(modifierInfo);

        // Make sure the default osmoregulation cost info is always last as it is usually the least important one (as
        // it is the same for all organelles)
        var count = modifierInfoList.GetChildCount();
        if (count > 1)
        {
            modifierInfoList.MoveChild(modifierInfoList.GetChild(count - 2), count - 1);
        }
    }

    /// <summary>
    ///   Gets a modifier based on its name.
    /// </summary>
    /// <param name="nodeName">Name of the modifier node</param>
    /// <returns>The found modifier or null</returns>
    public ModifierInfoLabel? GetModifierInfo(string nodeName)
    {
        foreach (var modifierInfo in modifierInfos)
        {
            if (modifierInfo.Name == nodeName)
                return modifierInfo;
        }

        return null;
    }

    /// <summary>
    ///   Creates UI elements for the processes info in a specific patch. Note that this doesn't refresh so this must
    ///   be always called again when the process speed information has changed.
    /// </summary>
    public void WriteOrganelleProcessList(List<ProcessSpeedInformation>? processes)
    {
        processList.ProcessesTitleColour = processTitleFont;
        processList.UpdateEquationAutomatically = false;

        if (processes == null || processes.Count <= 0)
        {
            processList.QueueFreeChildren();

            var noProcessLabel = new Label
            {
                LabelSettings = noProcessesFont,
                Text = "NO_ORGANELLE_PROCESSES",
            };

            processList.AddChild(noProcessLabel);
            return;
        }

        processList.ShowSpinners = false;
        processList.ShowToggles = false;
        processList.MarkRedOnLimitingCompounds = true;
        processList.ProcessesToShow = processes;
    }

    /// <summary>
    ///   Sets the value of all the membrane type modifiers on this tooltip relative
    ///   to the referenceMembrane. This currently only reads from the pre-added modifier
    ///   UI elements on this tooltip and doesn't actually create them on runtime.
    /// </summary>
    public void WriteMembraneModifierList(MembraneType referenceMembrane, MembraneType membraneType)
    {
        foreach (var modifier in modifierInfos)
        {
            float deltaValue;

            switch (modifier.Name)
            {
                case "baseMobility":
                    deltaValue = membraneType.MovementFactor - referenceMembrane.MovementFactor;
                    break;
                case "osmoregulationCost":
                    deltaValue = membraneType.OsmoregulationFactor - referenceMembrane.OsmoregulationFactor;
                    break;
                case "resourceAbsorptionSpeed":
                    deltaValue = membraneType.ResourceAbsorptionFactor - referenceMembrane.ResourceAbsorptionFactor;
                    break;
                case "health":
                    deltaValue = membraneType.Hitpoints - referenceMembrane.Hitpoints;
                    break;
                case "physicalResistance":
                    deltaValue = membraneType.PhysicalResistance - referenceMembrane.PhysicalResistance;
                    break;
                case "toxinResistance":
                    deltaValue = membraneType.ToxinResistance - referenceMembrane.ToxinResistance;
                    break;
                case "canEngulf":
                case "engulfInvulnerable":
                    deltaValue = 1;
                    break;
                default:
                    throw new Exception("Unhandled modifier type: " + modifier.Name);
            }

            // All stats with +0 value are made hidden on the tooltip so it'll be easier
            // to digest and compare modifier changes
            modifier.Visible = deltaValue != 0;

            // Apply the value to the text labels as percentage (except for Health)
            if (modifier.Name == "health")
            {
                modifier.ModifierValue =
                    StringUtils.FormatPositiveWithLeadingPlus(deltaValue.ToString("F0", CultureInfo.CurrentCulture),
                        deltaValue);
            }
            else
            {
                modifier.ModifierValue = StringUtils.FormatPositiveWithLeadingPlus(Localization
                    .Translate("PERCENTAGE_VALUE")
                    .FormatSafe((deltaValue * 100).ToString("F0", CultureInfo.CurrentCulture)), deltaValue);
            }

            if (modifier.Name == "osmoregulationCost")
            {
                modifier.AdjustValueColor(deltaValue, true);
            }
            else
            {
                modifier.AdjustValueColor(deltaValue);
            }
        }
    }

    [RunOnKeyDown("help", Priority = int.MaxValue)]
    public bool OpenMoreInfo()
    {
        if (!Visible || thriveopediaPageName == null)
            return false;

        ThriveopediaManager.OpenPage(thriveopediaPageName);
        return true;
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

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        if (Description == null)
        {
            descriptionLabel.ExtendedBbcode = null;
            return;
        }

        descriptionLabel.ExtendedBbcode = Localization.Translate(Description);
    }

    private void UpdateProcessesDescription()
    {
        if (processesDescriptionLabel == null)
            return;

        processesDescriptionLabel.ExtendedBbcode = Localization.Translate(ProcessesDescription);
        processesDescriptionLabel.Visible = !string.IsNullOrEmpty(ProcessesDescription);
    }

    private void UpdateMpCost()
    {
        if (mpLabel == null)
            return;

        string cost;

        if (mpCost < 0)
        {
            // Negative MP cost means it actually gives MP, to convey that to the player we need to explicitly
            // prefix the cost with a positive sign
            cost = "+" + MathF.Abs(mpCost).ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            cost = mpCost.ToString(CultureInfo.CurrentCulture);
        }

        mpLabel.Text = cost;
    }

    private void UpdateOsmoregulationCost()
    {
        if (osmoregulationModifier == null)
            return;

        if (ShowOsmoregulation)
        {
            osmoregulationModifier.Visible = true;
            osmoregulationModifier.ModifierValue =
                $"+{osmoregulationCost.ToString("0.###", CultureInfo.CurrentCulture)}";
        }
        else
        {
            osmoregulationModifier.Visible = false;
        }
    }

    private void UpdateRequiresNucleus()
    {
        if (requiresNucleusLabel == null)
            return;

        requiresNucleusLabel.Visible = requiresNucleus;
    }

    private void UpdateLists()
    {
        foreach (var item in modifierInfoList.GetChildren().OfType<ModifierInfoLabel>())
        {
            modifierInfos.Add(item);
        }
    }

    private void UpdateMoreInfo()
    {
        if (moreInfo == null)
            return;

        moreInfo.Visible = thriveopediaPageName != null;
    }

    private void OnTranslationsChanged()
    {
        UpdateDescription();
        UpdateProcessesDescription();
    }

    private void DummyKeepTranslations()
    {
        Localization.Translate("NO_ORGANELLE_PROCESSES");
    }
}
