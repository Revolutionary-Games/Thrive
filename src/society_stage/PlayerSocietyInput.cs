/// <summary>
///   Handles input for the society stage
/// </summary>
public partial class PlayerSocietyInput : NodeWithInput
{
#pragma warning disable CA2213 // this is our parent object
    private SocietyStage stage = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        // Not the cleanest that the parent has to be a specific stage type...
        stage = (SocietyStage)GetParent();

        ProcessMode = ProcessModeEnum.Always;
    }

    [RunOnKeyDown("g_build_structure")]
    public void OpenBuildMenu()
    {
        stage.PerformBuildOrOpenMenu();
    }

    [RunOnKeyDown("g_science")]
    public void ToggleResearchScreen()
    {
        stage.ToggleResearchScreen();
    }

    [RunOnKeyDown("ui_cancel")]
    public bool CancelBuild()
    {
        return stage.CancelBuildingPlaceIfInProgress();
    }
}
