using Godot;

/// <summary>
///   Multicellular editor tutorial GUI
/// </summary>
public partial class MulticellularEditorTutorialGUI : Control, ITutorialGUI
{
#pragma warning disable CA2213
    [Export]
    private CustomWindow specializationTutorial = null!;
#pragma warning restore CA2213

    public MainGameState AssociatedGameState => MainGameState.MulticellularEditor;
    public ITutorialInput? EventReceiver { get; set; }
    public bool IsClosingAutomatically { get; set; }
    public bool AllTutorialsDesiredState { get; private set; } = true;
    public Node GUINode => this;

    /// <summary>
    ///   This is used to ensure the scroll position shows elements related to active tutorials
    /// </summary>
    public ScrollContainer RightPanelScrollContainer { get; set; } = null!;

    public bool SpecializationTutorialVisible
    {
        get => specializationTutorial.Visible;
        set
        {
            if (value == specializationTutorial.Visible)
                return;

            if (value)
            {
                specializationTutorial.Show();
                RightPanelScrollContainer.ScrollVertical = 100;
            }
            else
            {
                specializationTutorial.Hide();
            }
        }
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        TutorialHelper.ProcessTutorialGUI(this, (float)delta);
    }

    public void OnClickedCloseAll()
    {
        TutorialHelper.HandleCloseAllForGUI(this);
    }

    public void OnSpecificCloseClicked(string closedThing)
    {
        TutorialHelper.HandleCloseSpecificForGUI(this, closedThing);
    }

    public void OnTutorialEnabledValueChanged(bool value)
    {
        AllTutorialsDesiredState = value;
    }
}
