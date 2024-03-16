﻿using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Shows a single chemical equation in a control
/// </summary>
public partial class ChemicalEquation : VBoxContainer
{
    [Export]
    public NodePath? TitlePath;

    [Export]
    public NodePath SpinnerPath = null!;

    [Export]
    public NodePath FirstLineContainerPath = null!;

#pragma warning disable CA2213
    private Label? title;
    private TextureRect? spinner;
    private HBoxContainer firstLineContainer = null!;

    private Texture2D equationArrowTexture = null!;

    // Dynamically generated controls
    private CompoundListBox? leftSide;
    private TextureRect? equationArrow;
    private CompoundListBox? rightSide;
    private Label? perSecondLabel;
    private Label? environmentSeparator;
    private CompoundListBox? environmentSection;
#pragma warning restore CA2213

    private IProcessDisplayInfo? equationFromProcess;
    private bool showSpinner;
    private Color defaultTitleColour = Colors.White;

    /// <summary>
    ///   For some reason resizing the process panel causes the spinner to reset to the initial rotation, so we use
    ///   this intermediate value to not have that happen.
    /// </summary>
    private float currentSpinnerRotation;

    /// <summary>
    ///   True when the process has no inputs (or only environmental inputs).
    ///   If true, a plus sign will be used before the output amounts.
    /// </summary>
    private bool hasNoInputs;

    public IProcessDisplayInfo? EquationFromProcess
    {
        get => equationFromProcess;
        set
        {
            if (equationFromProcess == null && value == null)
                return;

            if (value != null && equationFromProcess?.Equals(value) == true)
                return;

            equationFromProcess = value;
            UpdateEquation();
        }
    }

    public bool ShowSpinner
    {
        get => showSpinner;
        set
        {
            showSpinner = value;
            UpdateHeader();
        }
    }

    /// <summary>
    ///   If true then "/ second" is shown after the process inputs and outputs
    /// </summary>
    public bool ShowPerSecondLabel { get; set; } = true;

    /// <summary>
    ///   If true this will automatically check the set process for changes
    /// </summary>
    public bool AutoRefreshProcess { get; set; } = true;

    public float SpinnerBaseSpeed { get; set; } = Constants.DEFAULT_PROCESS_SPINNER_SPEED;

    public Color DefaultTitleColour
    {
        get => defaultTitleColour;
        set
        {
            if (defaultTitleColour == value)
                return;

            defaultTitleColour = value;
            if (title != null)
                UpdateHeader();
        }
    }

    /// <summary>
    ///   If true the title color will be changed to red if EquationFromProcess has any limiting compounds.
    /// </summary>
    public bool MarkRedOnLimitingCompounds { get; set; }

    public override void _Ready()
    {
        title = GetNode<Label>(TitlePath);
        spinner = GetNode<TextureRect>(SpinnerPath);
        firstLineContainer = GetNode<HBoxContainer>(FirstLineContainerPath);

        equationArrowTexture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/WhiteArrow.png");

        UpdateEquation();
    }

    public override void _Process(double delta)
    {
        if (ShowSpinner && EquationFromProcess != null)
        {
            currentSpinnerRotation += (float)delta * EquationFromProcess.CurrentSpeed * SpinnerBaseSpeed;

            // TODO: should we at some point subtract like 100000*360 from the spinner rotation to avoid float range
            // exceeding?

            spinner!.RotationDegrees = (int)currentSpinnerRotation % 360;
        }

        if (AutoRefreshProcess)
            UpdateEquation();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            if (perSecondLabel != null)
                perSecondLabel.Text = Localization.Translate("PER_SECOND_SLASH");

            if (environmentSeparator != null)
                environmentSeparator.Text = GetEnvironmentLabelText();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TitlePath != null)
            {
                TitlePath.Dispose();
                SpinnerPath.Dispose();
                FirstLineContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateEquation()
    {
        if (title == null)
            return;

        if (EquationFromProcess == null)
        {
            Visible = false;
            firstLineContainer.FreeChildren();
            leftSide = null;
            equationArrow = null;
            rightSide = null;
            perSecondLabel = null;
            environmentSeparator = null;
            environmentSection = null;
            return;
        }

        Visible = true;

        // Title and spinner
        UpdateHeader();

        var normalInputs = EquationFromProcess.Inputs.ToList();
        var environmentalInputs = EquationFromProcess.EnvironmentalInputs.ToList();

        // TODO: add detection when this should be intelligently split onto multiple lines

        // Inputs of the process
        UpdateLeftSide(normalInputs);

        // Outputs of the process
        UpdateRightSide();

        if (perSecondLabel == null && ShowPerSecondLabel)
        {
            perSecondLabel = new Label { Text = Localization.Translate("PER_SECOND_SLASH") };
            firstLineContainer.AddChild(perSecondLabel);
        }

        // Environment conditions
        UpdateEnvironmentPart(environmentalInputs);
    }

    private void UpdateHeader()
    {
        if (spinner != null)
            spinner.Visible = ShowSpinner;

        if (title == null || EquationFromProcess == null)
            return;

        title.Text = EquationFromProcess.Name;

        if (MarkRedOnLimitingCompounds && EquationFromProcess.LimitingCompounds is { Count: > 0 })
        {
            title.AddThemeColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
        }
        else
        {
            title.AddThemeColorOverride("font_color", DefaultTitleColour);
        }
    }

    private void UpdateLeftSide(List<KeyValuePair<Compound, float>> normalInputs)
    {
        if (normalInputs.Count == 0)
        {
            // Just environmental stuff
            hasNoInputs = true;

            if (equationArrow != null)
                equationArrow.Visible = false;

            if (leftSide != null)
                leftSide.Visible = false;
        }
        else
        {
            // Something turns into something else, uses the arrow notation
            hasNoInputs = false;

            // Show the inputs
            if (leftSide == null)
            {
                leftSide = new CompoundListBox();
                firstLineContainer.AddChild(leftSide);
            }

            leftSide.Visible = true;

            leftSide.UpdateCompounds(normalInputs, EquationFromProcess!.LimitingCompounds);

            // And the arrow
            if (equationArrow == null)
            {
                equationArrow = new TextureRect
                {
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                    CustomMinimumSize = new Vector2(20, 20),
                    Texture = equationArrowTexture,
                };
                firstLineContainer.AddChild(equationArrow);
            }

            equationArrow.Visible = true;
        }
    }

    private void UpdateRightSide()
    {
        if (rightSide == null)
        {
            rightSide = new CompoundListBox();
            firstLineContainer.AddChild(rightSide);
        }

        rightSide.PrefixPositiveWithPlus = hasNoInputs;

        rightSide.UpdateCompounds(EquationFromProcess!.Outputs, EquationFromProcess.LimitingCompounds);
    }

    private void UpdateEnvironmentPart(List<KeyValuePair<Compound, float>> environmentalInputs)
    {
        if (environmentalInputs.Count > 0)
        {
            if (environmentSeparator == null)
            {
                environmentSeparator = new Label
                {
                    Text = GetEnvironmentLabelText(),
                    CustomMinimumSize = new Vector2(30, 20),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                firstLineContainer.AddChild(environmentSeparator);
            }

            environmentSeparator.Visible = true;

            if (environmentSection == null)
            {
                environmentSection = new CompoundListBox { PartSeparator = ", ", UsePercentageDisplay = true };
                firstLineContainer.AddChild(environmentSection);
            }

            environmentSection.Visible = true;

            environmentSection.UpdateCompounds(environmentalInputs, EquationFromProcess!.LimitingCompounds);
        }
        else
        {
            if (environmentSeparator != null)
                environmentSeparator.Visible = false;

            if (environmentSection != null)
                environmentSection.Visible = false;
        }
    }

    private string GetEnvironmentLabelText()
    {
        return Localization.Translate("PROCESS_ENVIRONMENT_SEPARATOR");
    }
}
