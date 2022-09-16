using System.Collections.Generic;
using Godot;

public class MultipleChoiceFilterItem : BaseFilterItem
{
    [Export]
    public NodePath ItemDropDownPath = null!;

    public CustomDropDown ItemDropDown { get; private set; } = null!;

    public bool MultipleChoice { get; set; }

    public Dictionary<string, bool> ItemState { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        ItemDropDown = GetNode<CustomDropDown>(ItemDropDownPath);
        ItemDropDown.Connect("index_pressed", this, nameof(UpdateItemState));
    }

    private void UpdateItemState(int index)
    {
        if (MultipleChoice)
        {
            ItemState[ItemDropDown.Popup.GetItemText(index)] = ItemDropDown.Popup.IsItemChecked(index);
        }
        else
        {
            foreach (var key in ItemState.Keys)
                ItemState[key] = false;

            ItemState[ItemDropDown.Popup.GetItemText(index)] = true;
        }
    }
}
