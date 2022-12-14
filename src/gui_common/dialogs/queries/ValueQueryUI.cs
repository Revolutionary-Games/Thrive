using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   A button-based UI for retrieving a given value like a species speed.
/// </summary>
/// <TODO>
///   Allow for translations; ----> let valuequery handle it
///   Find a way to retrieve values for the value query item
/// </TODO>
public class ValueQueryUI : HBoxContainer, ISnapshotable
{
    [Export]
    public NodePath CategoryButtonPath = null!;

    [Export]
    public NodePath PropertyButtonPath = null!;

    /// <summary>
    ///   Button for choosing which category of property will be queried
    ///   (e.g. for species: behavior or number of a given organelle).
    /// </summary>
    private CustomDropDown categoryButton = null!;

    /// <summary>
    ///   Button for choosing the property whose value is to be retrieved (e.g. speed, or the number of flagella).
    /// </summary>
    private CustomDropDown propertyButton = null!;

    /// <summary>
    ///   The ValueQuery instance that will translate selected options to a value.
    /// </summary>
    /// <remarks>
    ///   We use a separate, interface object to create a context-free UI element (e.g. not tied to species)
    /// </remarks>
    private IValueQuery valueQuery = null!;

    /// <summary>
    ///   Dictionary containing the last property used within a category,
    ///   so that if the user goes back to a category, their last choice will be retrieved.
    /// </summary>
    private Dictionary<string, string> lastUsedProperties = new Dictionary<string, string>();

    public string CategoryName => categoryButton.Text;
    public string PropertyName => propertyButton.Text;

    public void Initialize(IValueQuery valueQuery)
    {
        this.valueQuery = valueQuery;

        /*lastSnapshotCategory = valueQuery.CategorizedProperties.Keys.First(_ => true);
        valueQueryCurrentProperty = valueQuery.CategorizedProperties[lastSnapshotCategory].First(_ => true);*/

        foreach (var categorizedProperties in valueQuery.CategorizedProperties)
        {
            lastUsedProperties[categorizedProperties.Key] = categorizedProperties.Value.First(_ => true);
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (valueQuery == null)
            throw new InvalidOperationException("Node was not initialized!");

        GD.Print(CategoryButtonPath);
        categoryButton = GetNode<CustomDropDown>(CategoryButtonPath);
        propertyButton = GetNode<CustomDropDown>(PropertyButtonPath);

        foreach (var category in valueQuery.CategorizedProperties.Keys)
        {
            categoryButton.AddItem(category, false, Colors.White);
        }

        categoryButton.CreateElements();
        categoryButton.Text = valueQuery.CurrentCategory;

        categoryButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected));
        propertyButton.Popup.Connect("index_pressed", this, nameof(OnNewPropertySelected));
    }

    public void OnNewCategorySelected(int choiceIndex)
    {
        OnCategoryChanged(categoryButton.Popup.GetItemText(choiceIndex));
    }

    public void OnNewPropertySelected(int choiceIndex)
    {
        propertyButton.Text = categoryButton.Popup.GetItemText(choiceIndex);
    }

    public void MakeSnapshot()
    {
        valueQuery.CurrentCategory = categoryButton.Text;
        valueQuery.CurrentProperty = propertyButton.Text;
    }

    public void RestoreLastSnapshot()
    {
        OnCategoryChanged(valueQuery.CurrentCategory);
        propertyButton.Text = valueQuery.CurrentProperty;
    }

    private void OnCategoryChanged(string newCategory)
    {
        // Do nothing if no change actually happened
        if (newCategory == categoryButton.Text)
            return;

        categoryButton.Text = newCategory;

        // Update properties
        propertyButton.ClearAllItems();

        foreach (var property in valueQuery.CategorizedProperties[newCategory])
        {
            propertyButton.AddItem(property, false, Colors.White);
        }

        propertyButton.CreateElements();

        // Restore previous property value
        propertyButton.Text = lastUsedProperties[newCategory];
    }
}
