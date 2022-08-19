using System;
using System.Collections.Generic;
using Godot;

public class FilterWindow : CustomDialog
{
    [Export]
    public bool HideOnApply = true;

    // Paths for nodes
    // TODO see for having those in Godot
    public NodePath DialogLabelPath = "VBoxContainer/Label";
    public NodePath ApplyButtonPath = "VBoxContainer/HBoxContainer/ApplyButton";
    public NodePath CancelButtonPath = "VBoxContainer/HBoxContainer/CancelButton";
    public NodePath FilterContainersPath = "VBoxContainer/FiltersContainer";

    private List<AutoEvoExploringTool.Filter> filters = new List<AutoEvoExploringTool.Filter>();

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty;

    // Texts for buttons & labels
    private string dialogText = string.Empty;
    private string applyText = "APPLY";
    private string cancelText = "CANCEL";

    // Nodes
    private Label? dialogLabel;
    private Button? applyButton;
    private Button? cancelButton;
    private VBoxContainer filtersContainer = null!;

    [Signal]
    public delegate void Applied();

    [Signal]
    public delegate void Cancelled();

    /// <summary>
    ///   The text displayed by the dialog.
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string DialogText
    {
        get => dialogText;
        set
        {
            dialogText = value;

            if (dialogLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   The text to be shown on the confirm button.
    /// </summary>
    [Export]
    public string ApplyText
    {
        get => applyText;
        set
        {
            applyText = value;

            if (applyButton != null)
                UpdateButtons();
        }
    }

    /// <summary>
    ///   The text to be shown on the cancel button.
    /// </summary>
    [Export]
    public string CancelText
    {
        get => cancelText;
        set
        {
            cancelText = value;

            if (cancelButton != null)
                UpdateButtons();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        dialogLabel = GetNode<Label>(DialogLabelPath);
        applyButton = GetNode<Button>(ApplyButtonPath);
        cancelButton = GetNode<Button>(CancelButtonPath);
        filtersContainer = GetNode<VBoxContainer>(FilterContainersPath);

        UpdateLabel();
        UpdateButtons();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (dirty)
        {
            filtersContainer.Update();
            Update();
        }

        dirty = false;
    }

    public void Initialize(AutoEvoExploringTool.Filter filter, string defaultText = "--")
    {
        ClearFilters();
        SetupFilter(filter, defaultText);
    }

    public void ClearFilters()
    {
        filters.Clear();
        filtersContainer.FreeChildren(true);
    }

    public void SetupFilter(AutoEvoExploringTool.Filter filter, string defaultText = "--")
    {
        filters.Add(filter);

        if (filtersContainer == null)
            throw new SceneTreeAttachRequired();

        var filterContainer = new HBoxContainer();

        var filterButton = new CustomDropDown();
        filterButton.Text = defaultText;

        foreach (var option in filter.FilterItemsNames)
        {
            filterButton.AddItem(option, false, Colors.White);
        }

        filterButton.CreateElements();
        filterButton.Popup.Connect("index_pressed", this, nameof(OnNewFilterCategorySelected));

        filterContainer.AddChild(filterButton);
        filtersContainer.AddChild(filterContainer);

        dirty = true;
    }

    private void OnNewFilterCategorySelected(int index)
    {
        var filterIndex = 0;
        // assume only 1 filter TODO EXPAND
        var filterContainer = filtersContainer.GetChild(filterIndex);

        var filterCategoryButton = filterContainer.GetChild<CustomDropDown>(0);

        if (filterCategoryButton == null)
            throw new InvalidCastException("First child of container was not a CustomDropDown node!");

        var filterCategory = filterCategoryButton.Popup.GetItemText(index);
        filterCategoryButton.Text = filterCategory;

        UpdateFilterArguments(filterIndex, filterCategory);

        dirty = true;
    }

    private void UpdateFilterArguments(int filterIndex, string filterCategory)
    {
        var filter = filters[filterIndex];

        if (filter == null)
            throw new ArgumentOutOfRangeException($"No filter at index {filterIndex}");

        var filterNode = filtersContainer.GetChild(filterIndex);

        //TODO EXCEPTIONS

        ClearFilterArguments(filterNode);

        var filterArguments = filter.FilterItems[filterCategory].FilterArguments;

        for (var i = 0; i < filterArguments.Count; i++)
        {
            // We do not use foreach to keep i;
            var filterArgument = filterArguments[i];

            if (filterArgument is AutoEvoExploringTool.Filter.MultipleChoiceFilterArgument)
            {
                // Avoid casting if unnecessary to prevent requiring one variable per option
                var multipleChoiceFilterArgument = filterArgument as AutoEvoExploringTool.Filter.MultipleChoiceFilterArgument;

                var filterArgumentButton = new CustomDropDown();
                filterArgumentButton.Text = multipleChoiceFilterArgument!.Value;

                foreach (var option in multipleChoiceFilterArgument.Options)
                {
                    filterArgumentButton.AddItem(option, false, Colors.White);
                }

                filterArgumentButton.CreateElements();

                filterNode.AddChild(filterArgumentButton);
            }
            else if (filterArgument is AutoEvoExploringTool.Filter.NumberFilterArgument)
            {
                var filterArgumentButton = new CustomDropDown();

                // TODO
                filterArgumentButton.Text = "TODO";
                filterNode.AddChild(filterArgumentButton);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        dirty = true;
    }

    /// <summary>
    ///   Removes all arguments from a filter, but keeps the category.
    /// </summary>
    private void ClearFilterArguments(Node filterNode)
    {
        for (var i = 1; i < filterNode.GetChildCount(); i++)
        {
            var nodeToRemove = filterNode.GetChild(i);
            filterNode.RemoveChild(nodeToRemove);

            // We free for memory, but keeping could allow to save options...
            nodeToRemove.Free();
        }

        dirty = true;
    }

    private void UpdateLabel()
    {
        if (dialogLabel == null)
            throw new SceneTreeAttachRequired();

        dialogLabel.Text = TranslationServer.Translate(dialogText);
    }

    private void UpdateButtons()
    {
        if (applyButton == null || cancelButton == null)
            throw new SceneTreeAttachRequired();

        applyButton.Text = applyText;
        cancelButton.Text = cancelText;
    }

    private void OnApplyPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (HideOnApply)
            Hide();

        EmitSignal(nameof(Applied));
    }

    private void OnCancelPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
        EmitSignal(nameof(Cancelled));
    }
}
