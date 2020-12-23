using System;
using System.Linq;
using System.Text;
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
    private Texture equationArrow;

    public IProcessDisplayInfo EquationFromProcess
    {
        get => equationFromProcess;
        set
        {
            if (equationFromProcess == value)
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
    ///   If true this will automatically check the set process for changes
    /// </summary>
    public bool AutoRefreshProcess { get; set; } = true;

    public float SpinnerBaseSpeed { get; set; } = Constants.DEFAULT_PROCESS_SPINNER_SPEED;

    public override void _Ready()
    {
        title = GetNode<Label>(TitlePath);
        spinner = GetNode<TextureRect>(SpinnerPath);
        firstLineContainer = GetNode<HBoxContainer>(FirstLineContainerPath);

        equationArrow = GD.Load<Texture>("res://assets/textures/gui/bevel/WhiteArrow.png");

        spinner.Visible = showSpinner;
        UpdateEquation();
    }

    public override void _Process(float delta)
    {
        if (ShowSpinner && EquationFromProcess != null)
        {
            spinner.RectRotation += delta * EquationFromProcess.CurrentSpeed * SpinnerBaseSpeed;
        }

        // if(AutoRefreshProcess)
        // UpdateEquation();
    }

    private void UpdateEquation()
    {
        if (EquationFromProcess == null)
        {
            Visible = false;
            firstLineContainer.FreeChildren();
            return;
        }

        Visible = true;

        // title.AddColorOverride("font_color", new Color(1.0f, 0.84f, 0.0f));
        title.Text = EquationFromProcess.Name;

        // If true, a plus sign will be used before the output amounts
        bool usePlus;

        var normalInputs = EquationFromProcess.Inputs.ToList();
        var environmentalInputs = EquationFromProcess.EnvironmentalInputs.ToList();

        // TODO: add detection when this should be intelligently split onto multiple lines

        if (normalInputs.Count == 0)
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
            foreach (var entry in normalInputs)
            {
                var amountLabel = new Label();
                amountLabel.Text = Math.Round(entry.Value, 3) + " ";
                firstLineContainer.AddChild(amountLabel);
                firstLineContainer.AddChild(GUICommon.Instance.CreateCompoundIcon(entry.Key.InternalName));
            }

            // And the arrow
            var arrow = new TextureRect();
            arrow.Expand = true;
            arrow.RectMinSize = new Vector2(20, 20);
            arrow.Texture = equationArrow;
            firstLineContainer.AddChild(arrow);
        }

        var stringBuilder = new StringBuilder(string.Empty, 25);

        // Outputs of the process
        foreach (var entry in EquationFromProcess.Outputs)
        {
            stringBuilder.Clear();

            var amountLabel = new Label();

            // TODO: add property to control this (also needs default font colour for restoring that)
#pragma warning disable 162

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (entry.Value == 0 && false)
            {
                // title.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                amountLabel.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
            }

            // ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162

            if (usePlus)
            {
                stringBuilder.Append(entry.Value >= 0 ? "+" : string.Empty);
            }

            stringBuilder.Append(Math.Round(entry.Value, 3) + " ");

            amountLabel.Text = stringBuilder.ToString();

            firstLineContainer.AddChild(amountLabel);
            firstLineContainer.AddChild(GUICommon.Instance.CreateCompoundIcon(entry.Key.InternalName));
        }

        var perSecondLabel = new Label();
        perSecondLabel.Text = TranslationServer.Translate("PER_SECOND_SLASH");

        firstLineContainer.AddChild(perSecondLabel);

        // Environment conditions
        if (environmentalInputs.Count > 0)
        {
            var atSymbol = new Label();

            atSymbol.Text = "@";
            atSymbol.RectMinSize = new Vector2(30, 20);
            atSymbol.Align = Label.AlignEnum.Center;
            firstLineContainer.AddChild(atSymbol);

            var first = true;

            foreach (var entry in environmentalInputs)
            {
                if (!first)
                {
                    var commaLabel = new Label();
                    commaLabel.Text = ", ";
                    firstLineContainer.AddChild(commaLabel);
                }

                first = false;

                var percentageLabel = new Label();

                // TODO: show also the maximum speed required environmental compounds
                percentageLabel.Text = Math.Round(entry.Value * 100, 1) + "%";

                firstLineContainer.AddChild(percentageLabel);
                firstLineContainer.AddChild(
                    GUICommon.Instance.CreateCompoundIcon(entry.Key.InternalName));
            }
        }
    }
}
