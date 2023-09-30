using Godot;

/// <summary>
///   Handles showing and changing the order in which cells in an early multicellular creature will divide.
/// </summary>
public class ReproductionOrder : MarginContainer
{
#pragma warning disable CA2213
    [Export]
    public NodePath IndexPath = null!;

    [Export]
    public NodePath CellDescriptionPath = null!;

    private Label? indexLabel;

    private Label? cellDescriptionLabel;
#pragma warning restore CA2213

    private string index = "Error: unset";
    private string cellDescription = "Error: unset";
    private bool isIsland;

    [Signal]
    public delegate void OnCellUp(int index);

    [Signal]
    public delegate void OnCellDown(int index);

    /// <summary>
    ///   When this cell will be created. 1 is the starting cell.
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
    ///   The name and location of this cell.
    /// </summary>
    public string CellDescription
    {
        get => cellDescription;
        set
        {
            cellDescription = value;
            UpdateCellDescription();
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
        cellDescriptionLabel = GetNode<Label>(CellDescriptionPath);

        UpdateIndex();
        UpdateCellDescription();
        UpdateColor();
    }

    public void OnUpPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnCellUp), GetParsedIndex());
    }

    public void OnDownPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnCellDown), GetParsedIndex());
    }

    private void UpdateIndex()
    {
        if (indexLabel != null)
            indexLabel.Text = index;
    }

    private void UpdateCellDescription()
    {
        if (cellDescriptionLabel != null)
            cellDescriptionLabel.Text = cellDescription;
    }

    private void UpdateColor()
    {
        if (indexLabel != null)
            indexLabel.SelfModulate = IsIsland ? Colors.Red : Colors.White;

        if (cellDescriptionLabel != null)
            cellDescriptionLabel.SelfModulate = IsIsland ? Colors.Red : Colors.White;
    }

    private int GetParsedIndex()
    {
        if (int.TryParse(index.Trim('.'), out var indexInt))
            return indexInt - 1;

        return -1;
    }
}
