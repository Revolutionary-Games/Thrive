using System;
using Godot;

public class FilterArgumentPopupMenu : CustomDropDown, IFilterArgumentNode
{
    private FilterArgument.MultipleChoiceFilterArgument filterArgument = null!;
    private string valueSnapshot = null!;

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty = true;

    public void Initialize(FilterArgument.MultipleChoiceFilterArgument filterArgument)
    {
        this.filterArgument = filterArgument;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (filterArgument == null)
            throw new InvalidOperationException("Node was not initialized!");

        Text = filterArgument.Value;

        foreach (var option in filterArgument.Options)
        {
            AddItem(option, false, Colors.White);
        }

        CreateElements();
        Popup.Connect("index_pressed", this, nameof(OnNewChoice));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (dirty)
        {
            Update();
            dirty = false;
        }
    }

    public void MakeSnapshot()
    {
        valueSnapshot = Text;
        GD.Print("FAPM making snapshot ", valueSnapshot);
    }

    public void RestoreLastSnapshot()
    {
        GD.Print("FAPM restoring value from snapshot: ", valueSnapshot);
        Text = valueSnapshot;
        filterArgument.Value = Text;
        dirty = true;
    }

    private void OnNewChoice(int choiceIndex)
    {
        Text = Popup.GetItemText(choiceIndex);
        filterArgument.Value = Text;
        dirty = true;
    }
}
