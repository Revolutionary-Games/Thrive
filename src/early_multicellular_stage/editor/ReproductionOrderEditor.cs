using System.Collections.Generic;
using Godot;

/// <summary>
///   This is used to change the order hexes are created in for hex-based organisms (i.e. <see cref="MicrobeSpecies"/>
///   and <see cref="EarlyMulticellularSpecies"/>).
/// </summary>
public class ReproductionOrderEditor<THex, TEditor, TCombinedAction, TAction, THexMove, TContext> : MarginContainer
    where THex : IActionHex
    where TEditor : class, IHexEditor, IEditorWithActions
    where TCombinedAction : CombinedEditorAction
    where TAction : EditorAction
    where THexMove : HexWithData<THex>, IActionHex
{
    [Export]
    public NodePath ReproductionOrderListPath = null!;

    /// <summary>
    ///   Whether islands will exists at some point in the organisms life cycle. If this is true, the reproduction order
    ///   is invalid.
    /// </summary>
    public bool HasOrderedIslands;

    private List<Label> orderNumberLabels = new();

    private HexLayout<THexMove>? orderedItems;

    private HexEditorComponentBase<TEditor, TCombinedAction, TAction, THexMove, TContext>? hexEditor;

    private IEditor? editor;

    private VBoxContainer reproductionOrderList = null!;

#pragma warning disable CA2213

    private PackedScene reproductionOrderEntryScene = null!;

#pragma warning restore CA2213

    public override void _Ready()
    {
        reproductionOrderList = GetNode<VBoxContainer>(ReproductionOrderListPath);

        reproductionOrderEntryScene =
            GD.Load<PackedScene>("res://src/early_multicellular_stage/editor/ReproductionOrderEntry.tscn");
    }

    /// <summary>
    ///   Sets up this <see cref="ReproductionOrderEditor{THex,TEditor,TCombinedAction,TAction,THexMove,TContext}"/> for
    ///   the provided parent editor.
    /// </summary>
    /// <param name="hexLayout">The hexes this reproduction editor should modify the order of.</param>
    /// <param name="parentEditor">The hex editor this reproduction editor belongs to.</param>
    /// <param name="editorBase">
    ///   The overarching <see cref="IEditor"/> this reproduction editor and its parent hex editor belong to.
    /// </param>
    public void Initialize(HexLayout<THexMove> hexLayout,
        HexEditorComponentBase<TEditor, TCombinedAction, TAction, THexMove, TContext> parentEditor, IEditor editorBase)
    {
        orderedItems = hexLayout;
        hexEditor = parentEditor;
        editor = editorBase;
    }

    /// <summary>
    ///   Updates the numbers displayed over hexes in the editor that show their reproduction order.
    /// </summary>
    public void UpdateReproductionOrderLabels()
    {
        foreach (var label in orderNumberLabels)
        {
            label.DetachAndQueueFree();
        }

        orderNumberLabels.Clear();

        if (hexEditor == null || !hexEditor.DisplayOrderNumbers || hexEditor.Camera == null || orderedItems == null ||
            editor == null)
        {
            return;
        }

        var font = GD.Load<Font>("res://src/gui_common/fonts/Lato-Bold-Smaller.tres");

        // We need to know how zoomed-in the camera is to know how big the numbers should be
        var cameraScale = new Vector2(hexEditor.Camera.DefaultCameraHeight / hexEditor.Camera.CameraHeight,
            hexEditor.Camera.DefaultCameraHeight / hexEditor.CameraHeight);

        for (var index = 0; index < reproductionOrderList.GetChildCount(); index++)
        {
            var control = (ReproductionOrderEntry)reproductionOrderList.GetChild(index);
            var hex = orderedItems[index];

            var label = new Label();
            label.Text = (index + 1).ToString();
            label.Modulate = control.IsIsland ? Colors.Red : Colors.White;

            var hexPosition = Hex.AxialToCartesian(hex.Position);
            label.RectPosition = hexEditor.Camera.UnprojectPosition(hexPosition);

            label.AddFontOverride("font", font);
            label.RectScale = cameraScale;

            editor.RootOfDynamicallySpawned.AddChild(label);
            label.SetAnchorsPreset(LayoutPreset.Center);
            orderNumberLabels.Add(label);

            label.Visible = true;
        }
    }

    /// <summary>
    ///   Updates the reproduction order list in the editor and determines if the order is valid.
    /// </summary>
    public void UpdateReproductionOrderList()
    {
        // Clear the existing list using QueueFree because a simple Free will cause problems when MoveUp and MoveDown
        // call this method
        reproductionOrderList.QueueFreeChildren();

        HasOrderedIslands = false;

        if (orderedItems == null)
            return;

        for (var index = 0; index < orderedItems.Count; index++)
        {
            var control = (ReproductionOrderEntry)reproductionOrderEntryScene.Instance();

            control.Index = $"{index + 1}.";
            var hex = orderedItems[index];
            control.Description = $"{hex.Data?.ToString()} ({hex.Position.Q},{hex.Position.R})";

            control.IsIsland = orderedItems.GetIslandHexes(index).Contains(hex.Position);
            HasOrderedIslands |= control.IsIsland;

            control.Connect(nameof(ReproductionOrderEntry.OnUp), this, nameof(MoveUp));
            control.Connect(nameof(ReproductionOrderEntry.OnDown), this, nameof(MoveDown));

            reproductionOrderList.AddChild(control);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReproductionOrderListPath.Dispose();
            reproductionOrderList.Dispose();
        }

        base.Dispose(disposing);
    }

    private void MoveUp(int index)
    {
        if (index <= 0)
            return;

        var data = new ReproductionOrderActionData(index, index - 1);

        var action = new SingleEditorAction<ReproductionOrderActionData>(DoReproductionOrderAction,
            UndoReproductionOrderAction, data);

        editor?.EnqueueAction(new CombinedEditorAction(action));
    }

    private void MoveDown(int index)
    {
        if (orderedItems == null || index >= orderedItems.Count - 1)
            return;

        var data = new ReproductionOrderActionData(index, index + 1);

        var action = new SingleEditorAction<ReproductionOrderActionData>(DoReproductionOrderAction,
            UndoReproductionOrderAction, data);

        editor?.EnqueueAction(new CombinedEditorAction(action));
    }

    [DeserializedCallbackAllowed]
    private void DoReproductionOrderAction(ReproductionOrderActionData data)
    {
        orderedItems?.SwapIndices(data.OldIndex, data.NewIndex);

        hexEditor?.MarkDirty();
    }

    [DeserializedCallbackAllowed]
    private void UndoReproductionOrderAction(ReproductionOrderActionData data)
    {
        orderedItems?.SwapIndices(data.NewIndex, data.OldIndex);

        hexEditor?.MarkDirty();
    }
}
