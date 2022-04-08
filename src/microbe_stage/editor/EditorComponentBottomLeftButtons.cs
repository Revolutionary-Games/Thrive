using System;
using System.Text.RegularExpressions;
using Godot;

public class EditorComponentBottomLeftButtons : MarginContainer
{
    [Export]
    public bool ShowSymmetryButton = true;

    [Export]
    public bool HandleRandomSpeciesName = true;

    [Export]
    public bool UseSpeciesNameValidation = true;

    [Export]
    public NodePath SymmetryButtonPath = null!;

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

    [Export]
    public NodePath NewHiddenAlternativeSpacerPath = null!;


    private TextureButton? newButton;
    private LineEdit speciesNameEdit = null!;
    private TextureButton? randomizeNameButton;

    private TextureButton symmetryButton = null!;
    private TextureRect symmetryIcon = null!;

    private Control newHiddenAlternativeSpacer = null!;

    private Texture symmetryIconDefault = null!;
    private Texture symmetryIcon2X = null!;
    private Texture symmetryIcon4X = null!;
    private Texture symmetryIcon6X = null!;

    private bool showNewButton = true;
    private bool showRandomizeButton = true;

    [Signal]
    public delegate void OnNewClicked();

    [Signal]
    public delegate void OnNameSet(string name);

    [Signal]
    public delegate void OnRandomName();

    [Signal]
    public delegate void OnSymmetryChanged();

    [Signal]
    public delegate void OnUndo();

    [Signal]
    public delegate void OnRedo();

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

    public override void _Ready()
    {
        UndoButton = GetNode<TextureButton>(UndoButtonPath);
        RedoButton = GetNode<TextureButton>(RedoButtonPath);

        newButton = GetNode<TextureButton>(NewButtonPath);
        speciesNameEdit = GetNode<LineEdit>(NameEditPath);
        randomizeNameButton = GetNode<TextureButton>(RandomizeNameButtonPath);

        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        symmetryIcon = GetNode<TextureRect>(SymmetryIconPath);

        newHiddenAlternativeSpacer = GetNode<Control>(NewHiddenAlternativeSpacerPath);

        symmetryIconDefault = GD.Load<Texture>("res://assets/textures/gui/bevel/1xSymmetry.png");
        symmetryIcon2X = GD.Load<Texture>("res://assets/textures/gui/bevel/2xSymmetry.png");
        symmetryIcon4X = GD.Load<Texture>("res://assets/textures/gui/bevel/4xSymmetry.png");
        symmetryIcon6X = GD.Load<Texture>("res://assets/textures/gui/bevel/6xSymmetry.png");

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
        EmitSignal(nameof(OnSymmetryChanged));
    }

    private void UndoPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnUndo));
    }

    private void RedoPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnRedo));
    }

    private void OnNewButtonClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnNewClicked));
    }

    private void OnNameTextChanged(string newText)
    {
        if (UseSpeciesNameValidation)
        {
            ReportValidityOfName(Regex.IsMatch(newText, Constants.SPECIES_NAME_REGEX));
        }

        EmitSignal(nameof(OnNameSet), newText);
    }

    private void OnNameTextEntered(string newText)
    {
        if (UseSpeciesNameValidation)
        {
            // Only defocus if the name is valid to indicate invalid namings to the player
            if (Regex.IsMatch(newText, Constants.SPECIES_NAME_REGEX))
            {
                speciesNameEdit.ReleaseFocus();
            }
            else
            {
                // TODO: Make the popup appear at the top of the line edit instead of at the last mouse position
                ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("INVALID_SPECIES_NAME_POPUP"), 2.5f);

                speciesNameEdit.GetNode<AnimationPlayer>("AnimationPlayer").Play("invalidSpeciesNameFlash");
            }
        }

        EmitSignal(nameof(OnNameSet), newText);
    }

    private void OnRandomizeNamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (HandleRandomSpeciesName)
        {
            var nameGenerator = SimulationParameters.Instance.NameGenerator;
            var randomizedName = nameGenerator.GenerateNameSection() + " " + nameGenerator.GenerateNameSection();

            speciesNameEdit.Text = randomizedName;
            OnNameTextChanged(randomizedName);
        }
        else
        {
            EmitSignal(nameof(OnRandomName));
        }
    }

    private void UpdateNewButtonVisibility()
    {
        if (newButton != null)
        {
            newButton.Visible = ShowNewButton;
            newHiddenAlternativeSpacer.Visible = !ShowNewButton;
        }
    }

    private void UpdateRandomButtonVisibility()
    {
        if (randomizeNameButton != null)
        {
            randomizeNameButton.Visible = showRandomizeButton;
        }
    }
}
