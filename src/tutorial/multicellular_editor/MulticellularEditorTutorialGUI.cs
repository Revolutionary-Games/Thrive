using Godot;

/// <summary>
///   Multicellular editor tutorial GUI
/// </summary>
public partial class MulticellularEditorTutorialGUI : Control, ITutorialGUI
{
#pragma warning disable CA2213
    [Export]
    private CustomWindow specializationTutorial = null!;

    [Export]
    private CustomWindow cellBodyPlanEditorIntroductionTutorial = null!;

    [Export]
    private CustomWindow macroscopicRequirementsTutorial = null!;

    [Export]
    private CustomWindow cellDuplicationTutorial = null!;

    [Export]
    private CustomWindow multicellularCellEditorTutorial = null!;

    [Export]
    private CustomWindow cellBodyPlanLayoutTutorial = null!;
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

    /// <summary>
    ///   This is used to focus this button when a relevant tutorial is active
    /// </summary>
    public Button MacroscopicRequirementsButton { get; set; } = null!;

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

    public bool CellDuplicationTutorialVisible
    {
        get => cellDuplicationTutorial.Visible;
        set
        {
            if (value == cellDuplicationTutorial.Visible)
                return;

            if (value)
            {
                cellDuplicationTutorial.Show();
            }
            else
            {
                cellDuplicationTutorial.Hide();
            }
        }
    }

    public bool CellBodyPlanIntroductionTutorialVisible
    {
        get => cellBodyPlanEditorIntroductionTutorial.Visible;
        set
        {
            if (value == cellBodyPlanEditorIntroductionTutorial.Visible)
                return;

            if (value)
            {
                cellBodyPlanEditorIntroductionTutorial.Show();
            }
            else
            {
                cellBodyPlanEditorIntroductionTutorial.Hide();
            }
        }
    }

    public bool MulticellularCellEditorTutorialVisible
    {
        get => multicellularCellEditorTutorial.Visible;
        set
        {
            if (value == multicellularCellEditorTutorial.Visible)
                return;

            if (value)
            {
                multicellularCellEditorTutorial.Show();
            }
            else
            {
                multicellularCellEditorTutorial.Hide();
            }
        }
    }

    public bool CellBodyPlanLayoutTutorialVisible
    {
        get => cellBodyPlanLayoutTutorial.Visible;
        set
        {
            if (value == cellBodyPlanLayoutTutorial.Visible)
                return;

            if (value)
            {
                cellBodyPlanLayoutTutorial.Show();
            }
            else
            {
                cellBodyPlanLayoutTutorial.Hide();
            }
        }
    }

    public bool MacroscopicRequirementsTutorialVisible
    {
        get => macroscopicRequirementsTutorial.Visible;
        set
        {
            if (value == macroscopicRequirementsTutorial.Visible)
                return;

            if (value)
            {
                macroscopicRequirementsTutorial.Show();
                RightPanelScrollContainer.ScrollVertical = 800;

                // Probably not needed, but just in case we invoke the focus grab
                Invoke.Instance.Perform(() => { MacroscopicRequirementsButton.GrabFocus(); });
            }
            else
            {
                macroscopicRequirementsTutorial.Hide();
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
