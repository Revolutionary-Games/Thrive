using Godot;

/// <summary>
///   Microbe editor tutorial
/// </summary>
public class MicrobeEditorTutorialGUI : Control, ITutorialGUI
{
    [Export]
    public NodePath? EditorEntryReportPath;

    [Export]
    public NodePath PatchMapPath = null!;

    [Export]
    public NodePath CellEditorIntroductionPath = null!;

    [Export]
    public NodePath CellEditorUndoPath = null!;

    [Export]
    public NodePath CellEditorUndoHighlightPath = null!;

    [Export]
    public NodePath CellEditorRedoPath = null!;

    [Export]
    public NodePath CellEditorRedoHighlightPath = null!;

    [Export]
    public NodePath CellEditorClosingWordsPath = null!;

    [Export]
    public NodePath AutoEvoPredictionPath = null!;

    [Export]
    public NodePath AutoEvoPredictionHighlightPath = null!;

    [Export]
    public NodePath StaySmallTutorialPath = null!;

#pragma warning disable CA2213
    private CustomDialog editorEntryReport = null!;
    private CustomDialog patchMap = null!;
    private CustomDialog cellEditorIntroduction = null!;
    private CustomDialog cellEditorUndo = null!;
    private CustomDialog cellEditorRedo = null!;
    private CustomDialog cellEditorClosingWords = null!;
    private CustomDialog autoEvoPrediction = null!;
    private CustomDialog staySmallTutorial = null!;
#pragma warning restore CA2213

    public MainGameState AssociatedGameState => MainGameState.MicrobeEditor;
    public ITutorialInput? EventReceiver { get; set; }
    public bool IsClosingAutomatically { get; set; }
    public bool TutorialEnabledSelected { get; private set; } = true;
    public Node GUINode => this;

    public ControlHighlight? CellEditorUndoHighlight { get; private set; }

    public ControlHighlight? CellEditorRedoHighlight { get; private set; }

    public ControlHighlight? AutoEvoPredictionHighlight { get; private set; }

    public bool EditorEntryReportVisible
    {
        get => editorEntryReport.Visible;
        set
        {
            if (value == editorEntryReport.Visible)
                return;

            if (value)
            {
                editorEntryReport.Show();
            }
            else
            {
                editorEntryReport.Hide();
            }
        }
    }

    public bool PatchMapVisible
    {
        get => patchMap.Visible;
        set
        {
            if (value == patchMap.Visible)
                return;

            if (value)
            {
                patchMap.Show();
            }
            else
            {
                patchMap.Hide();
            }
        }
    }

    public bool CellEditorIntroductionVisible
    {
        get => cellEditorIntroduction.Visible;
        set
        {
            if (value == cellEditorIntroduction.Visible)
                return;

            if (value)
            {
                cellEditorIntroduction.Show();
            }
            else
            {
                cellEditorIntroduction.Hide();
            }
        }
    }

    public bool CellEditorUndoVisible
    {
        get => cellEditorUndo.Visible;
        set
        {
            if (value == cellEditorUndo.Visible)
                return;

            if (value)
            {
                cellEditorUndo.Show();
            }
            else
            {
                cellEditorUndo.Hide();
            }
        }
    }

    public bool CellEditorRedoVisible
    {
        get => cellEditorRedo.Visible;
        set
        {
            if (value == cellEditorRedo.Visible)
                return;

            if (value)
            {
                cellEditorRedo.Show();
            }
            else
            {
                cellEditorRedo.Hide();
            }
        }
    }

    public bool CellEditorClosingWordsVisible
    {
        get => cellEditorClosingWords.Visible;
        set
        {
            if (value == cellEditorClosingWords.Visible)
                return;

            if (value)
            {
                cellEditorClosingWords.Show();
            }
            else
            {
                cellEditorClosingWords.Hide();
            }
        }
    }

    public bool AutoEvoPredictionVisible
    {
        get => autoEvoPrediction.Visible;
        set
        {
            if (value == autoEvoPrediction.Visible)
                return;

            if (value)
            {
                autoEvoPrediction.Show();
            }
            else
            {
                autoEvoPrediction.Hide();
            }
        }
    }

    public bool StaySmallTutorialVisible
    {
        get => staySmallTutorial.Visible;
        set
        {
            if (value == staySmallTutorial.Visible)
                return;

            if (value)
            {
                staySmallTutorial.Show();
            }
            else
            {
                staySmallTutorial.Hide();
            }
        }
    }

    public override void _Ready()
    {
        editorEntryReport = GetNode<CustomDialog>(EditorEntryReportPath);
        patchMap = GetNode<CustomDialog>(PatchMapPath);
        cellEditorIntroduction = GetNode<CustomDialog>(CellEditorIntroductionPath);
        cellEditorUndo = GetNode<CustomDialog>(CellEditorUndoPath);
        cellEditorRedo = GetNode<CustomDialog>(CellEditorRedoPath);
        cellEditorClosingWords = GetNode<CustomDialog>(CellEditorClosingWordsPath);
        autoEvoPrediction = GetNode<CustomDialog>(AutoEvoPredictionPath);
        staySmallTutorial = GetNode<CustomDialog>(StaySmallTutorialPath);

        CellEditorUndoHighlight = GetNode<ControlHighlight>(CellEditorUndoHighlightPath);
        CellEditorRedoHighlight = GetNode<ControlHighlight>(CellEditorRedoHighlightPath);
        AutoEvoPredictionHighlight = GetNode<ControlHighlight>(AutoEvoPredictionHighlightPath);

        PauseMode = PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
        TutorialHelper.ProcessTutorialGUI(this, delta);
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
        TutorialEnabledSelected = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (EditorEntryReportPath != null)
            {
                EditorEntryReportPath.Dispose();
                PatchMapPath.Dispose();
                CellEditorIntroductionPath.Dispose();
                CellEditorUndoPath.Dispose();
                CellEditorUndoHighlightPath.Dispose();
                CellEditorRedoPath.Dispose();
                CellEditorRedoHighlightPath.Dispose();
                CellEditorClosingWordsPath.Dispose();
                AutoEvoPredictionPath.Dispose();
                AutoEvoPredictionHighlightPath.Dispose();
                StaySmallTutorialPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
