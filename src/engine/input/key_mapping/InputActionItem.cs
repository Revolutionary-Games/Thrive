using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Godot;

/// <summary>
///   Shows keys associated with one Godot input action, for example g_move_forward.
///   Each InputActionItem has <see cref="InputEventItem">InputEventItems</see> associated with it.
/// </summary>
/// <remarks>
///   <para>
///     Used by OptionsMenu>Inputs>InputGroupContainer>InputGroupItem>InputActionItem.
///   </para>
///   <para>
///     Does not contain changing inputs logic, but contains InputEventItems, which do.
///   </para>
///   <para>
///     Handles the + button for adding new bindings.
///   </para>
/// </remarks>
public partial class InputActionItem : VBoxContainer
{
    [Export]
    public NodePath? AddInputEventPath;

    [Export]
    public NodePath InputActionHeaderPath = null!;

    [Export]
    public NodePath InputEventsContainerPath = null!;

#pragma warning disable CA2213
    private Label inputActionHeader = null!;
    private HBoxContainer inputEventsContainer = null!;
    private Button addInputEvent = null!;
#pragma warning restore CA2213

    private FocusFlowDynamicChildrenHelper focusHelper = null!;

    private bool nodeReferencesResolved;

    /// <summary>
    ///   The group in which this action is defined.
    /// </summary>
    public WeakReference<InputGroupItem>? AssociatedGroup { get; set; }

    /// <summary>
    ///   The godot specific action name
    /// </summary>
    /// <example>
    ///   g_move_left, g_zoom_in
    /// </example>
    public string InputName { get; set; } = string.Empty;

    /// <summary>
    ///   The string presented to the user
    /// </summary>
    /// <example>
    ///   Move left, Zoom in
    /// </example>
    public string? DisplayName { get; set; }

    /// <summary>
    ///   All the associated inputs executing this action. Must be initialized before using this class
    /// </summary>
    public ObservableCollection<InputEventItem> Inputs { get; private set; } = null!;

    public InputGroupItem? Group
    {
        get
        {
            if (AssociatedGroup == null)
                return null;

            AssociatedGroup.TryGetTarget(out var associatedGroup);
            return associatedGroup;
        }
    }

    public InputGroupList? GroupList
    {
        get
        {
            var group = Group;

            if (group == null)
                return null;

            group.AssociatedList.TryGetTarget(out var associatedList);

            return associatedList;
        }
    }

    /// <summary>
    ///   Sets up the inputs and adds them as children.
    /// </summary>
    public override void _Ready()
    {
        if (string.IsNullOrEmpty(InputName))
            throw new InvalidOperationException($"{nameof(InputName)} can't be empty");

        if (DisplayName == null)
            throw new InvalidOperationException($"{nameof(DisplayName)} can't be null");

        if (Inputs == null)
            throw new InvalidOperationException($"{nameof(Inputs)} can't be null");

        ResolveNodeReferences();

        inputActionHeader.Text = DisplayName;

        foreach (var input in Inputs)
        {
            input.AssociatedAction = new WeakReference<InputActionItem>(this);
            inputEventsContainer.AddChild(input);
        }

        inputEventsContainer.MoveChild(addInputEvent, Inputs.Count);

        focusHelper = new FocusFlowDynamicChildrenHelper(this,
            FocusFlowDynamicChildrenHelper.NavigationToChildrenDirection.None,
            FocusFlowDynamicChildrenHelper.NavigationInChildrenDirection.Horizontal);
    }

    public void ResolveNodeReferences()
    {
        if (nodeReferencesResolved)
            return;

        nodeReferencesResolved = true;

        inputActionHeader = GetNode<Label>(InputActionHeaderPath);
        inputEventsContainer = GetNode<HBoxContainer>(InputEventsContainerPath);
        addInputEvent = GetNode<Button>(AddInputEventPath);
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Inputs.CollectionChanged += OnInputsChanged;

        ResolveNodeReferences();

        addInputEvent.RegisterToolTipForControl("addInputButton", "options", false);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Inputs.CollectionChanged -= OnInputsChanged;

        addInputEvent.UnRegisterToolTipForControl("addInputButton", "options");
    }

    /// <summary>
    ///   Called by <see cref="InputGroupItem.NotifyFocusAdjusted"/> to finish the recursive adjustment of navigation
    ///   flow
    /// </summary>
    public void NotifyFocusAdjusted()
    {
        focusHelper.ReReadOwnerNeighbours();

        var focusableChildren = Inputs.SelectFirstFocusableChild().Append(addInputEvent).ToList();

        focusHelper.ApplyNavigationFlow(focusableChildren);

        // To make navigation a bit nicer we trap these like this as exiting the inputs list accidentally will
        // make it hard to get back to where the user was
        focusHelper.MakeFirstAndLastChildDeadEnds(focusableChildren);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return string.Equals(((InputActionItem)obj).InputName, InputName, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return InputName.GetHashCode();
    }

    internal static InputActionItem BuildGUI(InputGroupItem associatedGroup, NamedInputAction data,
        IEnumerable<SpecifiedInputKey> inputs)
    {
        associatedGroup.AssociatedList.TryGetTarget(out var target);

        if (target == null)
            throw new ArgumentException("associatedGroup has no associated list");

        var inputActionItem = (InputActionItem)target.InputActionItemScene.Instantiate();

        inputActionItem.InputName = data.InputName;
        inputActionItem.DisplayName = data.Name;
        inputActionItem.AssociatedGroup = new WeakReference<InputGroupItem>(associatedGroup);
        inputActionItem.Inputs =
            new ObservableCollection<InputEventItem>(inputs.Select(d => InputEventItem.BuildGUI(inputActionItem, d)));

        return inputActionItem;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (AddInputEventPath != null)
            {
                AddInputEventPath.Dispose();
                InputActionHeaderPath.Dispose();
                InputEventsContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   The small + button has been pressed
    /// </summary>
    private void OnAddEventButtonPressed()
    {
        var newInput = (InputEventItem)GroupList!.InputEventItemScene.Instantiate();
        newInput.AssociatedAction = new WeakReference<InputActionItem>(this);
        newInput.JustAdded = true;
        Inputs.Add(newInput);
    }

    private void OnInputsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:

                if (e.NewItems != null)
                {
                    foreach (InputEventItem newItem in e.NewItems)
                    {
                        inputEventsContainer.AddChild(newItem);
                    }
                }
                else
                {
                    GD.PrintErr("Collection notify add action doesn't have a new items list");
                }

                inputEventsContainer.MoveChild(addInputEvent, Inputs.Count);

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (InputEventItem oldItem in e.OldItems)
                    {
                        inputEventsContainer.RemoveChild(oldItem);
                    }
                }
                else
                {
                    GD.PrintErr("Collection notify remove action doesn't have an old items list");
                }

                break;
            default:
                throw new NotSupportedException($"{e.Action} is not supported on {nameof(Inputs)}");
        }
    }
}
