using Godot;

/// <summary>
///   Handles showing and changing the order in which cells in an early multicellular creature will divide.
/// </summary>
public class ReproductionOrderEntry : MarginContainer
{
#pragma warning disable CA2213
    [Export]
    public NodePath IndexPath = null!;

    [Export]
    public NodePath DescriptionPath = null!;

    private Label? indexLabel;

    private Label? descriptionLabel;
#pragma warning restore CA2213

    private string index = "Error: unset";
    private string description = "Error: unset";
    private bool isIsland;

    [Signal]
    public delegate void OnUp(int index);

    [Signal]
    public delegate void OnDown(int index);

    /// <summary>
    ///   When this hex will be created. 1 is the starting hex.
    /// </summary>
    public string Index
    {
        get => index;
        set
        {
            index = value;
            UpdateIndex();
        }
    }

    /// <summary>
    ///   The name and location of this hex.
    /// </summary>
    public string Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    public bool IsIsland
    {
        get => isIsland;
        set
        {
            isIsland = value;
            UpdateColor();
        }
    }

    public override void _Ready()
    {
        indexLabel = GetNode<Label>(IndexPath);
        descriptionLabel = GetNode<Label>(DescriptionPath);

        UpdateIndex();
        UpdateDescription();
        UpdateColor();
    }

    public void OnUpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnUp), GetParsedIndex());
    }

    public void OnDownPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnDown), GetParsedIndex());
    }

    private void UpdateIndex()
    {
        if (indexLabel != null)
            indexLabel.Text = index;
    }

    private void UpdateDescription()
    {
        if (descriptionLabel != null)
            descriptionLabel.Text = description;
    }

    private void UpdateColor()
    {
        if (indexLabel != null)
            indexLabel.SelfModulate = IsIsland ? Colors.Red : Colors.White;

        if (descriptionLabel != null)
            descriptionLabel.SelfModulate = IsIsland ? Colors.Red : Colors.White;
    }

    private int GetParsedIndex()
    {
        if (int.TryParse(index.Trim('.'), out var indexInt))
            return indexInt - 1;

        return -1;
    }
}
