/// <summary>
///   Handles input for the space stage
/// </summary>
public partial class PlayerSpaceInput : NodeWithInput
{
#pragma warning disable CA2213 // this is our parent object
    private SpaceStage stage = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        // Not the cleanest that the parent has to be a specific stage type...
        stage = (SpaceStage)GetParent();

        ProcessMode = ProcessModeEnum.Always;
    }

    // TODO: implement new thing building
    [RunOnKeyDown("g_build_structure")]
    public void OpenBuildMenu()
    {
        // stage.PerformBuildOrOpenMenu();
    }

    [RunOnKeyDown("g_science")]
    public void ToggleResearchScreen()
    {
        stage.ToggleResearchScreen();
    }

    [RunOnKeyDown("ui_cancel")]
    public bool CancelBuild()
    {
        return stage.CancelStructurePlaceIfInProgress();
    }

    [RunOnKeyDown("e_primary", Priority = -1)]
    public void SelectUnitUnderCursor()
    {
        if (stage.AttemptPlaceIfInProgress())
            return;

        // TODO: allow dragging a box to select multiple units

        stage.SelectUnitUnderCursor();
    }

    [RunOnKeyDown("e_secondary")]
    public void PerformUnitContextCommand()
    {
        if (stage.CancelStructurePlaceIfInProgress())
            return;

        stage.PerformUnitContextCommandIfSelected();
    }
}
