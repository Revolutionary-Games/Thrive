using Godot;
using System;
using System.Collections.Generic;

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

    private const float FILTER_NODE_SEPARATION = 100.0f;

    private Dictionary<string, int> filterOptions = null!;

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
        }

        dirty = false;
    }

    public void Initialize(Dictionary<string, int> filterOptionsWithArity, string defaultText = "--")
    {
        ClearFilters();
        SetupFilter(filterOptionsWithArity, defaultText);
    }

    public void ClearFilters()
    {
        filtersContainer.FreeChildren(true);
    }

    // TODO ARITY
    public void SetupFilter(Dictionary<string, int> filterOptionsWithArity, string defaultText = "--")
    {
        filterOptions = filterOptionsWithArity;

        if (filtersContainer == null)
            throw new SceneTreeAttachRequired();

        var filterContainer = new HBoxContainer();

        var filter = new CustomDropDown(); // filterScene.Instance<FilterNode>();
        filter.Text = defaultText;

        foreach (var option in filterOptionsWithArity.Keys)
        {
            filter.AddItem(option, false, Colors.White);
        }

        filter.CreateElements();
        filter.Popup.Connect("index_pressed", this, nameof(OnNewFilterCategorySelected));

        filterContainer.AddChild(filter);
        filtersContainer.AddChild(filterContainer);

        dirty = true;
    }

    private void OnNewFilterCategorySelected(int index)
    {
        var filterIndex = 0;
        // assume only 1 filter TODO EXPAND
        var filterContainer = filtersContainer.GetChild(filterIndex);

        var filterCategoryButton = filterContainer.GetChild<CustomDropDown>(0);

        // TODO set right exception
        if (filterCategoryButton == null)
            throw new Exception();

        var filterCategory = filterCategoryButton.Popup.GetItemText(index);
        filterCategoryButton.Text = filterCategory;

        UpdateFilterArguments(filterIndex, filterCategory);

        dirty = true;
    }

    private void UpdateFilterArguments(int filterIndex, string filterCategory)
    {
        var filter = filtersContainer.GetChild(filterIndex);

        // Remove all arguments, i.e. all children but the first one
        // TODO own function
        for (var i = 1; i < filter.GetChildCount(); i++)
        {
            var nodeToRemove = filter.GetChild(i);
            filter.RemoveChild(nodeToRemove);

            // We free for memory, but keeping could allow to save options...
            nodeToRemove.Free();
        }

        for (var i = 0; i < filterOptions[filterCategory]; i++)
        {
            var filterArgument = new CustomDropDown();
            filterArgument.Text = "??";

            filter.AddChild(filterArgument);
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
