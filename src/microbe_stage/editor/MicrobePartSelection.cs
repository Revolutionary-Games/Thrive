using System.Globalization;
using Godot;

/// <summary>
///   A specialized button to display a microbe part for selection in the cell editor.
/// </summary>
public class MicrobePartSelection : MarginContainer
{
    [Export]
    public ButtonGroup SelectionGroup = null!;

    private Label? mpLabel;
    private Button? button;
    private TextureRect? iconRect;
    private Label? nameLabel;

    private int mpCost;
    private Texture? partIcon;
    private string name = "Error: unset";
    private bool locked;
    private bool selected;

    /// <summary>
    ///   Emitted whenever the button is selected.
    /// </summary>
    [Signal]
    public delegate void OnPartSelected(string name);

    [Export]
    public int MPCost
    {
        get => mpCost;
        set
        {
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
    ///   User readable name. Note that the node Name property should be an "InternalName".
    /// </summary>
    [Export]
    public string PartName
    {
        get => name;
        set
        {
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
        mpLabel = GetNode<Label>("VBoxContainer/MP");
        button = GetNode<Button>("VBoxContainer/Button");
        iconRect = GetNode<TextureRect>("VBoxContainer/Button/Icon");
        nameLabel = GetNode<Label>("VBoxContainer/Name");

        UpdateButton();
        UpdateLabels();
        UpdateIcon();
    }

    private void UpdateLabels()
    {
        if (mpLabel == null || nameLabel == null)
            return;

        mpLabel.Text = string.Format(
            CultureInfo.CurrentCulture, TranslationServer.Translate("MP_COST"), MPCost);

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

    private void OnSelected()
    {
        EmitSignal(nameof(OnPartSelected), Name);
    }
}
