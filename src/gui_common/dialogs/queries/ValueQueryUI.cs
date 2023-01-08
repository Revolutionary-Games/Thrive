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

    [Export]
    public NodePath WholeNumberInputFieldPath = null!;

    /// <summary>
    ///   Button for choosing which category of property will be queried
    ///   (e.g. for species: behavior or number of a given organelle).
    /// </summary>
    private CustomDropDown categoryButton = null!;

    /// <summary>
    ///   Button for choosing the property whose value is to be retrieved (e.g. speed, or the number of flagella).
    /// </summary>
    private CustomDropDown propertyButton = null!;

    private SpinBox wholeNumberInputField = null!;

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

        categoryButton = GetNode<CustomDropDown>(CategoryButtonPath);
        propertyButton = GetNode<CustomDropDown>(PropertyButtonPath);
        wholeNumberInputField = GetNode<SpinBox>(WholeNumberInputFieldPath);

        categoryButton.AddItem("NUMBER", false, Colors.White);
        foreach (var category in valueQuery.CategorizedProperties.Keys)
        {
            categoryButton.AddItem(category, false, Colors.White);
        }

        categoryButton.CreateElements();
        ChangeCategory(valueQuery.CurrentCategory);
        propertyButton.Text = valueQuery.CurrentProperty;

        categoryButton.Popup.Connect("index_pressed", this, nameof(OnNewCategorySelected));
        propertyButton.Popup.Connect("index_pressed", this, nameof(OnNewPropertySelected));
        wholeNumberInputField.Connect("value_changed", this, nameof(OnSpinBoxValueChanged));
    }

    public void OnNewCategorySelected(int choiceIndex)
    {
        ChangeCategory(categoryButton.Popup.GetItemText(choiceIndex));
    }

    public void OnNewPropertySelected(int choiceIndex)
    {
        ChangeProperty(propertyButton.Popup.GetItemText(choiceIndex));
    }

    public void MakeSnapshot()
    {
        valueQuery.CurrentCategory = categoryButton.Text;
        valueQuery.CurrentProperty = propertyButton.Text;
        lastUsedProperties[valueQuery.CurrentCategory] = valueQuery.CurrentProperty;
    }

    public void RestoreLastSnapshot()
    {
        ChangeCategory(valueQuery.CurrentCategory);
        propertyButton.Text = lastUsedProperties[valueQuery.CurrentCategory];
    }

    private void ChangeCategory(string newCategory)
    {
        // TEMP
        var NUMBER_FIELD = "NUMBER";

        // Do nothing if no change actually happened
        if (newCategory == categoryButton.Text)
            return;

        categoryButton.Text = newCategory;
        valueQuery.CurrentCategory = newCategory;

        if (newCategory == NUMBER_FIELD)
        {
            propertyButton.Visible = false;
            wholeNumberInputField.Visible = true;
        }
        else
        {
            propertyButton.Visible = true;
            wholeNumberInputField.Visible = false;

            // Update properties
            propertyButton.ClearAllItems();

            foreach (var property in valueQuery.CategorizedProperties[newCategory])
            {
                propertyButton.AddItem(property, false, Colors.White);
            }

            propertyButton.CreateElements();

            // Restore previous property value
            ChangeProperty(lastUsedProperties[newCategory]);
        }
       
    }

    private void ChangeProperty(string newProperty)
    {
        if (newProperty == propertyButton.Text)
            return;

        propertyButton.Text = newProperty;
        valueQuery.CurrentProperty = newProperty;
    }

    private void OnSpinBoxValueChanged(float value)
    {
        // WHY NEED DOUBLE APPLY FOR RESULTS?
        GD.Print("changing value", value);
        valueQuery.CurrentNumericValue = value;
    }
}
