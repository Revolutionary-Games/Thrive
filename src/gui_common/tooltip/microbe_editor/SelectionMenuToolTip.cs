using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
///   The main tooltip class for the selections on the microbe editor's selection menu.
///   Contains list of processes and modifiers info.
/// </summary>
public class SelectionMenuToolTip : Control, ICustomToolTip
{
    [Export]
    public NodePath NameLabelPath;

    [Export]
    public NodePath MpLabelPath;

    [Export]
    public NodePath DescriptionLabelPath;

    [Export]
    public NodePath ProcessesDescriptionLabelPath;

    [Export]
    public NodePath ModifierListPath;

    [Export]
    public NodePath ProcessListPath;

    private PackedScene modifierInfoScene;
    private Font latoBoldFont;

    private Label nameLabel;
    private Label mpLabel;

    private Label descriptionLabel;
    private RichTextLabel processesDescriptionLabel;
    private VBoxContainer modifierInfoList;
    private ProcessList processList;

    private string displayName;
    private string description;
    private string processesDescription;
    private int mpCost;

    /// <summary>
    ///   Hold reference of modifier info elements for easier access to change their values later
    /// </summary>
    private List<ModifierInfoLabel> modifierInfos = new List<ModifierInfoLabel>();

    public Vector2 Position
    {
        get => RectPosition;
        set => RectPosition = value;
    }

    public Vector2 Size
    {
        get => RectSize;
        set => RectSize = value;
    }

    [Export]
    public string DisplayName
    {
        get => displayName;
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
    ///     This supports custom format (for example: "Turns [glucose] into [atp]") where strings inside
    ///     the square brackets will be parsed and replaced with a predefined template. This is done to
    ///     make translating feasible.
    ///     NOTE: description string should only be set here and not directly on the rich text label node
    ///     as it will be overidden otherwise.
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

    [Export]
    public string Description
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
    public float DisplayDelay { get; set; } = 0.0f;

    public bool ToolTipVisible
    {
        get => Visible;
        set => Visible = value;
    }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.FollowMousePosition;

    public bool HideOnMousePress { get; set; } = false;

    public Node ToolTipNode => this;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>(NameLabelPath);
        mpLabel = GetNode<Label>(MpLabelPath);
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
        processesDescriptionLabel = GetNode<RichTextLabel>(ProcessesDescriptionLabelPath);
        modifierInfoList = GetNode<VBoxContainer>(ModifierListPath);
        processList = GetNode<ProcessList>(ProcessListPath);

        modifierInfoScene = GD.Load<PackedScene>("res://src/gui_common/tooltip/microbe_editor/ModifierInfoLabel.tscn");
        latoBoldFont = GD.Load<Font>("res://src/gui_common/fonts/Lato-Bold-Smaller.tres");

        UpdateName();
        UpdateDescription();
        UpdateProcessesDescription();
        UpdateMpCost();
        UpdateLists();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateProcessesDescription();
        }

        if (what == NotificationResized)
        {
            // A workaround to get RichTextLabel's height properly updated on size change
            if (processesDescriptionLabel != null)
                processesDescriptionLabel.BbcodeText = processesDescriptionLabel.BbcodeText;
        }
    }

    /// <summary>
    ///   Instances the UI element for a modifier info
    /// </summary>
    public void AddModifierInfo(string name, float value)
    {
        var modifierInfo = (ModifierInfoLabel)modifierInfoScene.Instance();

        modifierInfo.DisplayName = name;
        modifierInfo.ModifierValue = value.ToString(CultureInfo.CurrentCulture);

        modifierInfoList.AddChild(modifierInfo);
        modifierInfos.Add(modifierInfo);
    }

    public ModifierInfoLabel GetModifierInfo(string nodeName)
    {
        return modifierInfos.Find(found => found.Name == nodeName);
    }

    /// <summary>
    ///   Creates UI elements for the processes info in a specific patch
    /// </summary>
    public void WriteOrganelleProcessList(List<ProcessSpeedInformation> processes)
    {
        if (processes == null || processes.Count <= 0)
        {
            processList.QueueFreeChildren();

            var noProcesslabel = new Label();
            noProcesslabel.AddFontOverride("font", latoBoldFont);
            noProcesslabel.Text = TranslationServer.Translate("NO_ORGANELLE_PROCESSES");
            processList.AddChild(noProcesslabel);
            return;
        }

        processList.ShowSpinners = false;
        processList.ProcessesTitleColour = new Color(1.0f, 0.83f, 0.0f);
        processList.MarkRedOnLimitingCompounds = true;
        processList.ProcessesToShow = processes.Cast<IProcessDisplayInfo>().ToList();
    }

    /// <summary>
    ///   Sets the value of all the membrane type modifiers on this tooltip relative
    ///   to the referenceMembrane. This currently only reads from the preadded modifier
    ///   UI elements on this tooltip and doesn't actually create them on runtime.
    /// </summary>
    public void WriteMembraneModifierList(MembraneType referenceMembrane, MembraneType membraneType)
    {
        foreach (var modifier in modifierInfos)
        {
            var deltaValue = 0.0f;

            switch (modifier.Name)
            {
                case "mobility":
                    deltaValue = membraneType.MovementFactor - referenceMembrane.MovementFactor;
                    break;
                case "osmoregulation_cost":
                    deltaValue = membraneType.OsmoregulationFactor - referenceMembrane.OsmoregulationFactor;
                    break;
                case "resource_absorption_speed":
                    deltaValue = membraneType.ResourceAbsorptionFactor - referenceMembrane.ResourceAbsorptionFactor;
                    break;
                case "health":
                    deltaValue = membraneType.Hitpoints - referenceMembrane.Hitpoints;
                    break;
                case "physical_resistance":
                    deltaValue = membraneType.PhysicalResistance - referenceMembrane.PhysicalResistance;
                    break;
                case "toxin_resistance":
                    deltaValue = membraneType.ToxinResistance - referenceMembrane.ToxinResistance;
                    break;
            }

            // All stats with +0 value that are not part of the selected membrane is made hidden
            // on the tooltip so it'll be easier to digest and compare modifier changes
            if (Name != referenceMembrane.InternalName && modifier.ShowValue)
                modifier.Visible = deltaValue != 0;

            // Apply the value to the text labels as percentage (except for Health)
            if (modifier.Name == "health")
            {
                modifier.ModifierValue = (deltaValue >= 0 ? "+" : string.Empty)
                    + deltaValue.ToString("F0", CultureInfo.CurrentCulture);
            }
            else
            {
                modifier.ModifierValue = ((deltaValue >= 0) ? "+" : string.Empty)
                    + (deltaValue * 100).ToString("F0", CultureInfo.CurrentCulture) + "%";
            }

            if (modifier.Name == "osmoregulation_cost")
            {
                modifier.AdjustValueColor(deltaValue, true);
            }
            else
            {
                modifier.AdjustValueColor(deltaValue);
            }
        }
    }

    public void OnDisplay()
    {
        Show();
    }

    public void OnHide()
    {
        Hide();
    }

    /// <summary>
    ///   Searches the processes description string for "keys" of compound names and input actions
    ///   and turns them into a BBCode string with matching values.
    /// </summary>
    private string ParseProcessesDescription()
    {
        if (string.IsNullOrEmpty(ProcessesDescription))
            return string.Empty;

        var inputEvents = Settings.GetCurrentlyAppliedControls();

        var result = Regex.Replace(TranslationServer.Translate(ProcessesDescription), @"\[(.*?)\]", found =>
        {
            var parsed = string.Empty;

            var value = found.Groups[1].Value;

            // Parse compound names
            if (SimulationParameters.Instance.DoesCompoundExist(value))
            {
                var compound = SimulationParameters.Instance.GetCompound(value);

                parsed = $"[b]{compound.Name}[/b] [font=res://src/gui_common/fonts/" +
                    $"BBCode-Image-VerticalCenterAlign-3.tres] [img=20]{compound.IconPath}[/img][/font]";
            }

            // Parse input actions
            if (InputMap.HasAction(value))
            {
                var events = inputEvents.Data[value];

                for (int i = 0; i < events.Count; i++)
                {
                    parsed += $"[b][{KeyNames.Translate(events[i].Code)}][/b]";

                    if (events.Count > 1 && i < events.Count - 1)
                        parsed += ", ";
                }
            }

            return parsed;
        });

        return result;
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

        if (string.IsNullOrEmpty(Description))
        {
            description = descriptionLabel.Text;
        }
        else
        {
            descriptionLabel.Text = description;
        }
    }

    private void UpdateProcessesDescription()
    {
        if (processesDescriptionLabel == null)
            return;

        // Need to delay this so we can get the correct input controls from settings.
        Invoke.Instance.Queue(() => processesDescriptionLabel.BbcodeText = ParseProcessesDescription());
    }

    private void UpdateMpCost()
    {
        if (mpLabel == null)
            return;

        mpLabel.Text = MutationPointCost.ToString(CultureInfo.CurrentCulture);
    }

    private void UpdateLists()
    {
        foreach (ModifierInfoLabel item in modifierInfoList.GetChildren())
        {
            modifierInfos.Add(item);
        }
    }
}
