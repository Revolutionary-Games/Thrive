using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FilterDialog : CustomConfirmationDialog
{
    [Export]
    public NodePath ItemsContainerPath = null!;

    private VBoxContainer itemsContainer = null!;

    public override void _Ready()
    {
        base._Ready();

        itemsContainer = GetNode<VBoxContainer>(ItemsContainerPath);
    }

    public void InitWithFilter(Filter filter)
    {
        itemsContainer.FreeChildren();

        foreach (var description in filter.Descriptions)
        {
            switch (description)
            {
                case MultipleChoiceFilterDescription multipleChoice:
                {
                    var item = new MultipleChoiceFilterItem();
                    item.ItemName = multipleChoice.DescriptionName;
                    item.MultipleChoice = multipleChoice.MultipleChoice;
                    item.ItemState = new Dictionary<string, bool>(multipleChoice.Values);

                    foreach (var entry in multipleChoice.Values)
                    {
                        if (multipleChoice.MultipleChoice)
                        {
                            item.ItemDropDown.AddItem(entry.Key, true, Colors.White).Checked = entry.Value;
                        }
                        else
                        {
                            item.ItemDropDown.AddItem(entry.Key, false, Colors.White);
                        }
                    }

                    break;
                }

                case NumericFilterDescription numeric:
                {
                    var item = new NumericFilterItem();
                    item.ItemName = numeric.DescriptionName;
                    item.MaxValue = numeric.MaxValue;
                    item.MinValue = numeric.MinValue;
                    item.Value = numeric.Value;
                    item.Operator = numeric.Operator;

                    break;
                }

                default:
                    throw new NotSupportedException();
            }
        }
    }

    public Filter GetOutcomeFilter()
    {
        var filter = new Filter();

        foreach (BaseFilterItem item in itemsContainer.GetChildren())
        {
            filter.Descriptions.Add(item switch
            {
                MultipleChoiceFilterItem multipleChoice => new MultipleChoiceFilterDescription(multipleChoice.ItemName)
                {
                    MultipleChoice = multipleChoice.MultipleChoice,
                    Values = multipleChoice.ItemState,
                },

                NumericFilterItem numeric => new NumericFilterDescription(numeric.ItemName)
                {
                    MaxValue = numeric.MaxValue,
                    MinValue = numeric.MinValue,
                    Value = numeric.Value,
                    Operator = numeric.Operator,
                },

                _ => throw new NotSupportedException(),
            });
        }

        return filter;
    }
}
