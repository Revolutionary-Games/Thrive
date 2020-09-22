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

    private WindowDialog editorEntryReport;
    private WindowDialog patchMap;
    private WindowDialog cellEditorIntroduction;
    private WindowDialog cellEditorUndo;
    private WindowDialog cellEditorRedo;
    private WindowDialog cellEditorClosingWords;

    public MainGameState AssociatedGameState { get; } = MainGameState.MicrobeEditor;
    public ITutorialInput EventReceiver { get; set; }
    public bool IsClosingAutomatically { get; set; }
    public bool TutorialEnabledSelected { get; private set; } = true;
    public Node GUINode => this;

    public ControlHighlight CellEditorUndoHighlight { get; private set; }

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
                editorEntryReport.Visible = false;
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
                patchMap.Visible = false;
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
                cellEditorIntroduction.Visible = false;
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
                cellEditorUndo.Visible = false;
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
                cellEditorRedo.Visible = false;
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
                cellEditorClosingWords.Visible = false;
            }
        }
    }

    public override void _Ready()
    {
        editorEntryReport = GetNode<WindowDialog>(EditorEntryReportPath);
        patchMap = GetNode<WindowDialog>(PatchMapPath);
        cellEditorIntroduction = GetNode<WindowDialog>(CellEditorIntroductionPath);
        cellEditorUndo = GetNode<WindowDialog>(CellEditorUndoPath);
        cellEditorRedo = GetNode<WindowDialog>(CellEditorRedoPath);
        cellEditorClosingWords = GetNode<WindowDialog>(CellEditorClosingWordsPath);
        CellEditorUndoHighlight = GetNode<ControlHighlight>(CellEditorUndoHighlightPath);
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
