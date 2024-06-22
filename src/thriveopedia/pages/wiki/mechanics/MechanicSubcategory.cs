using System.Linq;
using Godot;

/// <summary>
///   A heading of a subcategory of mechanics present in <see cref="ThriveopediaMechanicsRootPage"/>
/// </summary>
public partial class MechanicSubcategory : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    public ThriveopediaMechanicsRootPage ParentPage = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        ParentPage.Connect(ThriveopediaMechanicsRootPage.SignalName.OnStageChanged,
            new Callable(this, nameof(OnSelectedStageChanged)));
    }

    public void OnSelectedStageChanged()
    {
        // Note: this won't work if the order of children is changed
        var children = GetChild<HFlowContainer>(2).GetChildren();
        Visible = children.Any(c => c.GetChild<Button>(0).Visible);
    }
}
