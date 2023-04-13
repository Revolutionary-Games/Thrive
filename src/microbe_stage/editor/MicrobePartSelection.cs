using System.Globalization;
using Godot;

/// <summary>
///   A specialized button to display a microbe part for selection in the cell editor.
/// </summary>
public class MicrobePartSelection : MarginContainer
{
#pragma warning disable CA2213
    [Export]
    public ButtonGroup SelectionGroup = null!;

    private Control contentContainer = null!;
    private Label? mpLabel;
    private Button? button;
    private TextureRect? iconRect;
    private Label? nameLabel;
#pragma warning restore CA2213

    private int mpCost;
    private Texture? partIcon;
    private string name = "Error: unset";
    private bool locked;
    private bool alwaysShowLabel;
    private bool selected;

    /// <summary>
    ///   Emitted whenever the button is selected. Note that this sends the Node's Name as the parameter
    ///   (and not PartName)
    /// </summary>
    [Signal]
    public delegate void OnPartSelected(string name);

    [Export]
    public int MPCost
    {
        get => mpCost;
        set
        {
            if (mpCost == value)
                return;

            mpCost = value;
            UpdateLabels();
        }
    }

    [Export]
    public Texture? PartIcon
    {
        get => partIcon;
        set
        {
            partIcon = value;
            UpdateIcon();
        }
    }

    /// <summary>
    ///   Translatable name. This needs to be the STRING_LIKE_THIS to make this automatically react to language change
    /// </summary>
    [Export]
    public string PartName
    {
        get => name;
        set
        {
            if (name == value)
                return;

            name = value;
            UpdateLabels();
        }
    }

    /// <summary>
    ///   Currently only makes the button unselectable if true.
    /// </summary>
    [Export]
    public bool Locked
    {
        get => locked;
        set
        {
            locked = value;

            UpdateButton();
            UpdateIcon();
            UpdateLabels();
        }
    }

    /// <summary>
    ///   Whether this button should always display the part name.
    /// </summary>
    [Export]
    public bool AlwaysShowLabel
    {
        get => alwaysShowLabel;
        set
        {
            alwaysShowLabel = value;
            UpdateLabels();
        }
    }

    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;

            UpdateButton();
            UpdateIcon();
        }
    }

    public override void _Ready()
    {
        contentContainer = GetChild<Control>(0);
        mpLabel = GetNode<Label>("VBoxContainer/HBoxContainer/MP");
        button = GetNode<Button>("VBoxContainer/Button");
        iconRect = GetNode<TextureRect>("VBoxContainer/Button/Icon");
        nameLabel = GetNode<Label>("VBoxContainer/Name");

        OnDisplayPartNamesChanged(Settings.Instance.DisplayPartNames);
        Settings.Instance.DisplayPartNames.OnChanged += OnDisplayPartNamesChanged;

        UpdateButton();
        UpdateLabels();
        UpdateIcon();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Settings.Instance.DisplayPartNames.OnChanged -= OnDisplayPartNamesChanged;
    }

    private void OnDisplayPartNamesChanged(bool displayed)
    {
        if (nameLabel == null)
            return;

        var showNameLabel = displayed || AlwaysShowLabel;

        nameLabel.Visible = showNameLabel;

        contentContainer.AddConstantOverride("separation", showNameLabel ? 1 : 4);
    }

    private void UpdateLabels()
    {
        if (mpLabel == null || nameLabel == null)
            return;

        var cost = mpCost.ToString(CultureInfo.CurrentCulture);

        if (mpCost < 0)
        {
            // Negative MP cost means it actually gives MP, to convey that to the player we need to explicitly
            // prefix the cost with a positive sign
            cost = TranslationServer.Translate("POSITIVE_SIGNED_NUMBER").FormatSafe(Mathf.Abs(mpCost));
        }

        mpLabel.Text = cost;
        nameLabel.Text = PartName;

        mpLabel.Modulate = Colors.White;
        nameLabel.Modulate = Colors.White;

        if (Locked)
        {
            mpLabel.Modulate = Colors.Gray;
            nameLabel.Modulate = Colors.Gray;
        }
    }

    private void UpdateIcon()
    {
        if (partIcon == null || iconRect == null)
            return;

        iconRect.Texture = PartIcon;
        iconRect.Modulate = Colors.White;

        if (Selected)
            iconRect.Modulate = Colors.Black;

        if (Locked)
            iconRect.Modulate = Colors.Gray;
    }

    private void UpdateButton()
    {
        if (button == null)
            return;

        button.Group = SelectionGroup;
        button.Pressed = Selected;
        button.Disabled = Locked;
    }

    private void OnPressed()
    {
        if (Selected)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnPartSelected), Name);
    }
}
