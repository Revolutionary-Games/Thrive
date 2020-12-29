using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Shows a single chemical equation in a control
/// </summary>
public class ChemicalEquation : VBoxContainer
{
    [Export]
    public NodePath TitlePath;

    [Export]
    public NodePath SpinnerPath;

    [Export]
    public NodePath FirstLineContainerPath;

    private Label title;
    private TextureRect spinner;
    private HBoxContainer firstLineContainer;
    private IProcessDisplayInfo equationFromProcess;
    private bool showSpinner;
    private Texture equationArrowTexture;

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

    // Dynamically generated controls
    private CompoundListBox leftSide;
    private TextureRect equationArrow;
    private CompoundListBox rightSide;
    private Label perSecondLabel;
    private Label environmentSeparator;
    private CompoundListBox environmentSection;

    public IProcessDisplayInfo EquationFromProcess
    {
        get => equationFromProcess;
        set
        {
            if (equationFromProcess == null && value == null)
                return;

            if (equationFromProcess != null && equationFromProcess.Equals(value))
                return;

            equationFromProcess = value;

            if (title != null)
                UpdateEquation();
        }
    }

    public bool ShowSpinner
    {
        get => showSpinner;
        set
        {
            showSpinner = value;
            if (spinner != null)
                spinner.Visible = showSpinner;
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

    public override void _Ready()
    {
        title = GetNode<Label>(TitlePath);
        spinner = GetNode<TextureRect>(SpinnerPath);
        firstLineContainer = GetNode<HBoxContainer>(FirstLineContainerPath);

        equationArrowTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/WhiteArrow.png");

        spinner.Visible = showSpinner;
        UpdateEquation();
    }

    public override void _Process(float delta)
    {
        if (ShowSpinner && EquationFromProcess != null)
        {
            currentSpinnerRotation += delta * EquationFromProcess.CurrentSpeed * SpinnerBaseSpeed;
            spinner.RectRotation = currentSpinnerRotation;
        }

        if (AutoRefreshProcess)
            UpdateEquation();
    }

    private void UpdateEquation()
    {
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

        // title.AddColorOverride("font_color", new Color(1.0f, 0.84f, 0.0f));
        title.Text = EquationFromProcess.Name;

        var normalInputs = EquationFromProcess.Inputs.ToList();
        var environmentalInputs = EquationFromProcess.EnvironmentalInputs.ToList();

        // TODO: add detection when this should be intelligently split onto multiple lines

        // Inputs of the process
        UpdateLeftSide(normalInputs);

        // Outputs of the process
        UpdateRightSide();

        if (perSecondLabel == null && ShowPerSecondLabel)
        {
            perSecondLabel = new Label { Text = TranslationServer.Translate("PER_SECOND_SLASH") };
            firstLineContainer.AddChild(perSecondLabel);
        }

        // Environment conditions
        UpdateEnvironmentPart(environmentalInputs);
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

            leftSide.UpdateCompounds(normalInputs);

            // And the arrow
            if (equationArrow == null)
            {
                equationArrow = new TextureRect
                {
                    Expand = true, RectMinSize = new Vector2(20, 20), Texture = equationArrowTexture,
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

        rightSide.UpdateCompounds(EquationFromProcess.Outputs);

        // TODO: colouring for individual amounts
        foreach (var entry in EquationFromProcess.Outputs)
        {
            // TODO: add property to control this (also needs default font colour for restoring that)
#pragma warning disable 162

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (entry.Value == 0 && false)
            {
                // title.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                // amountLabel.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
            }
#pragma warning restore 162
        }
    }

    private void UpdateEnvironmentPart(List<KeyValuePair<Compound, float>> environmentalInputs)
    {
        if (environmentalInputs.Count > 0)
        {
            if (environmentSeparator == null)
            {
                environmentSeparator = new Label
                {
                    Text = "@",
                    RectMinSize = new Vector2(30, 20),
                    Align = Label.AlignEnum.Center,
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

            environmentSection.UpdateCompounds(environmentalInputs);
        }
        else
        {
            if (environmentSeparator != null)
                environmentSeparator.Visible = false;

            if (environmentSection != null)
                environmentSection.Visible = false;
        }
    }
}
