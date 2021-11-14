using Godot;

/// <summary>
///   Microbe editor tutorial
/// </summary>
public class MicrobeEditorTutorialGUI : Control, ITutorialGUI
{
    [Export]
    public NodePath EditorEntryReportPath;

    [Export]
    public NodePath PatchMapPath;

    [Export]
    public NodePath CellEditorIntroductionPath;

    [Export]
    public NodePath CellEditorUndoPath;

    [Export]
    public NodePath CellEditorUndoHighlightPath;

    [Export]
    public NodePath CellEditorRedoPath;

    [Export]
    public NodePath CellEditorClosingWordsPath;

    [Export]
    public NodePath AutoEvoPredictionPath;

    [Export]
    public NodePath AutoEvoPredictionHighlightPath;

    private CustomDialog editorEntryReport;
    private CustomDialog patchMap;
    private CustomDialog cellEditorIntroduction;
    private CustomDialog cellEditorUndo;
    private CustomDialog cellEditorRedo;
    private CustomDialog cellEditorClosingWords;
    private CustomDialog autoEvoPrediction;

    public MainGameState AssociatedGameState { get; } = MainGameState.MicrobeEditor;
    public ITutorialInput EventReceiver { get; set; }
    public bool IsClosingAutomatically { get; set; }
    public bool TutorialEnabledSelected { get; private set; } = true;
    public Node GUINode => this;

    public ControlHighlight CellEditorUndoHighlight { get; private set; }

    public ControlHighlight AutoEvoPredictionHighlight { get; private set; }

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

    public override void _Ready()
    {
        editorEntryReport = GetNode<CustomDialog>(EditorEntryReportPath);
        patchMap = GetNode<CustomDialog>(PatchMapPath);
        cellEditorIntroduction = GetNode<CustomDialog>(CellEditorIntroductionPath);
        cellEditorUndo = GetNode<CustomDialog>(CellEditorUndoPath);
        cellEditorRedo = GetNode<CustomDialog>(CellEditorRedoPath);
        cellEditorClosingWords = GetNode<CustomDialog>(CellEditorClosingWordsPath);
        autoEvoPrediction = GetNode<CustomDialog>(AutoEvoPredictionPath);

        CellEditorUndoHighlight = GetNode<ControlHighlight>(CellEditorUndoHighlightPath);
        AutoEvoPredictionHighlight = GetNode<ControlHighlight>(AutoEvoPredictionHighlightPath);
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
}
