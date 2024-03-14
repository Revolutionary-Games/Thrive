using System;
using System.Text.RegularExpressions;
using Godot;

public partial class EditorComponentBottomLeftButtons : MarginContainer
{
    [Export]
    public bool ShowSymmetryButton = true;

    [Export]
    public bool HandleRandomSpeciesName = true;

    [Export]
    public bool UseSpeciesNameValidation = true;

    [Export]
    public NodePath? SymmetryButtonPath;

    [Export]
    public NodePath SymmetryIconPath = null!;

    [Export]
    public NodePath UndoButtonPath = null!;

    [Export]
    public NodePath RedoButtonPath = null!;

    [Export]
    public NodePath NewButtonPath = null!;

    [Export]
    public NodePath NameEditPath = null!;

    [Export]
    public NodePath RandomizeNameButtonPath = null!;

#pragma warning disable CA2213
    private TextureButton? newButton;
    private LineEdit speciesNameEdit = null!;
    private TextureButton? randomizeNameButton;

    private TextureButton symmetryButton = null!;
    private TextureRect symmetryIcon = null!;

    private Texture2D symmetryIconDefault = null!;
    private Texture2D symmetryIcon2X = null!;
    private Texture2D symmetryIcon4X = null!;
    private Texture2D symmetryIcon6X = null!;
#pragma warning restore CA2213

    private bool showNewButton = true;
    private bool showRandomizeButton = true;

    /// <summary>
    ///   True when one of our (name related) Controls is hovered. This needs to be known to know if a click happened
    ///   outside the name editing controls, for detecting when the name needs to be validated.
    /// </summary>
    private bool controlsHoveredOver;

    [Signal]
    public delegate void OnNewClickedEventHandler();

    [Signal]
    public delegate void OnNameSetEventHandler(string name);

    [Signal]
    public delegate void OnRandomNameEventHandler();

    [Signal]
    public delegate void OnSymmetryChangedEventHandler();

    [Signal]
    public delegate void OnUndoEventHandler();

    [Signal]
    public delegate void OnRedoEventHandler();

    [Export]
    public bool ShowNewButton
    {
        get => showNewButton;
        set
        {
            showNewButton = value;
            UpdateNewButtonVisibility();
        }
    }

    [Export]
    public bool ShowRandomizeButton
    {
        get => showRandomizeButton = true;
        set
        {
            showRandomizeButton = value;
            UpdateRandomButtonVisibility();
        }
    }

    public TextureButton UndoButton { get; private set; } = null!;

    public TextureButton RedoButton { get; private set; } = null!;

    public bool UndoEnabled { get => !UndoButton.Disabled; set => UndoButton.Disabled = !value; }
    public bool RedoEnabled { get => !RedoButton.Disabled; set => RedoButton.Disabled = !value; }
    public bool SymmetryEnabled { get => !symmetryButton.Disabled; set => symmetryButton.Disabled = !value; }

    public override void _Ready()
    {
        UndoButton = GetNode<TextureButton>(UndoButtonPath);
        RedoButton = GetNode<TextureButton>(RedoButtonPath);

        newButton = GetNode<TextureButton>(NewButtonPath);
        speciesNameEdit = GetNode<LineEdit>(NameEditPath);
        randomizeNameButton = GetNode<TextureButton>(RandomizeNameButtonPath);

        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        symmetryIcon = GetNode<TextureRect>(SymmetryIconPath);

        symmetryIconDefault = GD.Load<Texture2D>("res://assets/textures/gui/bevel/1xSymmetry.png");
        symmetryIcon2X = GD.Load<Texture2D>("res://assets/textures/gui/bevel/2xSymmetry.png");
        symmetryIcon4X = GD.Load<Texture2D>("res://assets/textures/gui/bevel/4xSymmetry.png");
        symmetryIcon6X = GD.Load<Texture2D>("res://assets/textures/gui/bevel/6xSymmetry.png");

        UpdateNewButtonVisibility();
        UpdateRandomButtonVisibility();

        if (!ShowSymmetryButton)
            symmetryButton.Visible = false;
    }

    public void RegisterTooltips()
    {
        UndoButton.RegisterToolTipForControl("undoButton", "editor");
        RedoButton.RegisterToolTipForControl("redoButton", "editor");

        symmetryButton.RegisterToolTipForControl("symmetryButton", "editor");

        newButton!.RegisterToolTipForControl("newCellButton", "editor");
        randomizeNameButton!.RegisterToolTipForControl("randomizeNameButton", "editor");
    }

    public void SetNewName(string name)
    {
        speciesNameEdit.Text = name;

        // Callback is manually called because the function isn't called automatically here
        OnNameTextChanged(name);
    }

    public void ResetSymmetry()
    {
        SetSymmetry(HexEditorSymmetry.None);
    }

    public void SetSymmetry(HexEditorSymmetry symmetry)
    {
        symmetryIcon.Texture = symmetry switch
        {
            HexEditorSymmetry.None => symmetryIconDefault,
            HexEditorSymmetry.XAxisSymmetry => symmetryIcon2X,
            HexEditorSymmetry.FourWaySymmetry => symmetryIcon4X,
            HexEditorSymmetry.SixWaySymmetry => symmetryIcon6X,
            _ => throw new ArgumentException("unknown symmetry value"),
        };
    }

    public void ReportValidityOfName(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(speciesNameEdit);
        }
        else
        {
            GUICommon.MarkInputAsInvalid(speciesNameEdit);
        }
    }

    public void SetNamePlaceholder(string placeholder)
    {
        speciesNameEdit.PlaceholderText = placeholder;
    }

    public void OnClickedOffName()
    {
        var focused = GetViewport().GuiGetFocusOwner();

        // Ignore if the species name line edit wasn't focused or if one of our controls is hovered
        if (focused != speciesNameEdit || controlsHoveredOver)
            return;

        PerformValidation(speciesNameEdit.Text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SymmetryButtonPath != null)
            {
                SymmetryButtonPath.Dispose();
                SymmetryIconPath.Dispose();
                UndoButtonPath.Dispose();
                RedoButtonPath.Dispose();
                NewButtonPath.Dispose();
                NameEditPath.Dispose();
                RandomizeNameButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnSymmetryHold()
    {
        symmetryIcon.Modulate = new Color(0, 0, 0);
    }

    private void OnSymmetryReleased()
    {
        symmetryIcon.Modulate = new Color(1, 1, 1);
    }

    private void OnSymmetryClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnSymmetryChanged);
    }

    private void UndoPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnUndo);
    }

    private void RedoPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnRedo);
    }

    private void OnNewButtonClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnNewClicked);
    }

    private void OnNameTextChanged(string newText)
    {
        if (UseSpeciesNameValidation)
        {
            ReportValidityOfName(Regex.IsMatch(newText, Constants.SPECIES_NAME_REGEX) && ValidateNameLength(newText));
        }

        EmitSignal(SignalName.OnNameSet, newText);
    }

    private void OnNameTextEntered(string newText)
    {
        PerformValidation(newText);
        EmitSignal(SignalName.OnNameSet, newText);
    }

    private void PerformValidation(string text)
    {
        if (UseSpeciesNameValidation)
        {
            bool nameLengthValid = ValidateNameLength(text);

            // Only defocus if the name is valid to indicate invalid namings to the player
            if (Regex.IsMatch(text, Constants.SPECIES_NAME_REGEX) && nameLengthValid)
            {
                speciesNameEdit.ReleaseFocus();
            }
            else
            {
                // Prevents user from doing other actions with an invalid name
                GetViewport().SetInputAsHandled();

                // TODO: Make the popup appear at the top of the line edit instead of at the last mouse position
                if (!nameLengthValid)
                {
                    ToolTipManager.Instance.ShowPopup(Localization.Translate("SPECIES_NAME_TOO_LONG_POPUP"), 2.5f);
                }
                else
                {
                    ToolTipManager.Instance.ShowPopup(Localization.Translate("INVALID_SPECIES_NAME_POPUP"), 2.5f);
                }

                speciesNameEdit.GetNode<AnimationPlayer>("AnimationPlayer").Play("invalidSpeciesNameFlash");
            }
        }
        else
        {
            speciesNameEdit.ReleaseFocus();
        }
    }

    private bool ValidateNameLength(string name)
    {
        return speciesNameEdit.GetThemeFont("font").GetStringSize(name).X < Constants.MAX_SPECIES_NAME_LENGTH_PIXELS;
    }

    private void OnRandomizeNamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (HandleRandomSpeciesName)
        {
            var nameGenerator = SimulationParameters.Instance.NameGenerator;
            var randomizedName = nameGenerator.GenerateNameSection() + " " +
                nameGenerator.GenerateNameSection(null, true);

            speciesNameEdit.Text = randomizedName;
            OnNameTextChanged(randomizedName);
        }
        else
        {
            EmitSignal(SignalName.OnRandomName);
        }
    }

    private void UpdateNewButtonVisibility()
    {
        if (newButton != null)
        {
            newButton.Visible = ShowNewButton;
        }
    }

    private void UpdateRandomButtonVisibility()
    {
        if (randomizeNameButton != null)
        {
            randomizeNameButton.Visible = showRandomizeButton;
        }
    }

    private void OnControlMouseEntered()
    {
        controlsHoveredOver = true;
    }

    private void OnControlMouseExited()
    {
        controlsHoveredOver = false;
    }
}
