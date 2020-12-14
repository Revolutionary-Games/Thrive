using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;

/// <summary>
///   The main tooltip class for the selections on the microbe editor's selection menu.
///   Contains list of processes and modifiers info
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
    public NodePath ModifierListPath;

    [Export]
    public NodePath ProcessListPath;

    [Export]
    public PackedScene ModifierInfoScene;

    private Label nameLabel;

    // ReSharper disable once NotAccessedField.Local
    private Label mpLabel;

    private Label descriptionLabel;
    private VBoxContainer modifierInfoList;
    private VBoxContainer processList;

    private Tween tween;

    private string displayName;
    private string description;

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
    public float DisplayDelay { get; set; } = 0.3f;

    public bool ToolTipVisible
    {
        get => Visible;
        set => Visible = value;
    }

    public Node ToolTipNode => this;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>(NameLabelPath);
        mpLabel = GetNode<Label>(MpLabelPath);
        descriptionLabel = GetNode<Label>(DescriptionLabelPath);
        modifierInfoList = GetNode<VBoxContainer>(ModifierListPath);
        processList = GetNode<VBoxContainer>(ProcessListPath);

        tween = GetNode<Tween>("Tween");

        UpdateName();
        UpdateDescription();
        UpdateLists();
    }

    /// <summary>
    ///   Instances the UI element for a modifier info
    /// </summary>
    public void AddModifierInfo(string name, float value)
    {
        var modifierInfo = (ModifierInfoLabel)ModifierInfoScene.Instance();

        modifierInfo.ModifierName = name;
        modifierInfo.ModifierValue = value.ToString(CultureInfo.CurrentCulture);

        modifierInfoList.AddChild(modifierInfo);
        modifierInfos.Add(modifierInfo);
    }

    public ModifierInfoLabel GetModifierInfo(string name)
    {
        return modifierInfos.Find(found => found.Name == name);
    }

    /// <summary>
    ///   Creates UI elements for the processes info in a specific patch
    /// </summary>
    public void WriteOrganelleProcessList(List<ProcessSpeedInformation> processes)
    {
        // Remove previous process list
        if (processList.GetChildCount() > 0)
        {
            foreach (Node children in processList.GetChildren())
            {
                children.QueueFree();
            }
        }

        if (processes == null)
        {
            var noProcesslabel = new Label();
            noProcesslabel.Text = "No processes";
            processList.AddChild(noProcesslabel);
            return;
        }

        foreach (var process in processes)
        {
            var processContainer = new VBoxContainer();
            processContainer.MouseFilter = MouseFilterEnum.Ignore;
            processList.AddChild(processContainer);

            var processTitle = new Label();
            processTitle.AddColorOverride("font_color", new Color(1.0f, 0.84f, 0.0f));
            processTitle.Text = process.Process.Name;
            processContainer.AddChild(processTitle);

            var processBody = new HBoxContainer();
            processBody.MouseFilter = MouseFilterEnum.Ignore;

            bool usePlus;

            if (process.OtherInputs.Count == 0)
            {
                // Just environmental stuff
                usePlus = true;
            }
            else
            {
                // Something turns into something else, uses the arrow notation
                usePlus = false;

                // Show the inputs
                // TODO: add commas or maybe pluses for multiple inputs
                foreach (var key in process.OtherInputs.Keys)
                {
                    var inputCompound = process.OtherInputs[key];

                    var amountLabel = new Label();
                    amountLabel.Text = Math.Round(inputCompound.Amount, 3) + " ";
                    processBody.AddChild(amountLabel);
                    processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(inputCompound.Compound.InternalName));
                }

                // And the arrow
                var arrow = new TextureRect();
                arrow.Expand = true;
                arrow.RectMinSize = new Vector2(20, 20);
                arrow.Texture = GD.Load<Texture>("res://assets/textures/gui/bevel/WhiteArrow.png");
                processBody.AddChild(arrow);
            }

            // Outputs of the process. It's assumed that every process has outputs
            foreach (var key in process.Outputs.Keys)
            {
                var outputCompound = process.Outputs[key];

                var amountLabel = new Label();

                var stringBuilder = new StringBuilder(string.Empty, 150);

                // Changes process title and process# to red if process has 0 output
                if (outputCompound.Amount == 0)
                {
                    processTitle.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                    amountLabel.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                }

                if (usePlus)
                {
                    stringBuilder.Append(outputCompound.Amount >= 0 ? "+" : string.Empty);
                }

                stringBuilder.Append(Math.Round(outputCompound.Amount, 3) + " ");

                amountLabel.Text = stringBuilder.ToString();

                processBody.AddChild(amountLabel);
                processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(outputCompound.Compound.InternalName));
            }

            var perSecondLabel = new Label();
            perSecondLabel.Text = TranslationServer.Translate("PER_SECOND_SLASH");

            processBody.AddChild(perSecondLabel);

            // Environment conditions
            if (process.EnvironmentInputs.Count > 0)
            {
                var atSymbol = new Label();

                atSymbol.Text = "@";
                atSymbol.RectMinSize = new Vector2(30, 20);
                atSymbol.Align = Label.AlignEnum.Center;
                processBody.AddChild(atSymbol);

                var first = true;

                foreach (var key in process.EnvironmentInputs.Keys)
                {
                    if (!first)
                    {
                        var commaLabel = new Label();
                        commaLabel.Text = ", ";
                        processBody.AddChild(commaLabel);
                    }

                    first = false;

                    var environmentCompound = process.EnvironmentInputs[key];

                    // To percentage
                    var percentageLabel = new Label();

                    // TODO: sunlight needs some special handling (it used to say the lux amount)
                    percentageLabel.Text = Math.Round(environmentCompound.AvailableAmount * 100, 1) + "%";

                    processBody.AddChild(percentageLabel);
                    processBody.AddChild(
                        GUICommon.Instance.CreateCompoundIcon(environmentCompound.Compound.InternalName));
                }
            }

            processContainer.AddChild(processBody);
        }
    }

    public void OnDisplay()
    {
        ToolTipHelper.TooltipFadeIn(tween, this);
    }

    public void OnHide()
    {
        Hide();
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

    private void UpdateLists()
    {
        foreach (ModifierInfoLabel item in modifierInfoList.GetChildren())
        {
            modifierInfos.Add(item);
        }
    }
}
