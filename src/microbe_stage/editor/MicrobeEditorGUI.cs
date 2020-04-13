using System;
using Godot;

/// <summary>
///   Main class managing the microbe editor GUI
/// </summary>
public class MicrobeEditorGUI : Node
{
    private MicrobeEditor editor;

    private Godot.Collections.Array organelleSelectionElements;
    private Godot.Collections.Array membraneSelectionElements;

    public override void _Ready()
    {
        organelleSelectionElements = GetTree().GetNodesInGroup("OrganelleSelectionElement");
        membraneSelectionElements = GetTree().GetNodesInGroup("MembraneSelectionElement");

        // Fade out for that smooth satisfying transition
        TransitionManager.AddFade(Fade.FadeType.FadeOut, 0.5f);
        TransitionManager.StartTransitions(null, string.Empty);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            MenuButtonPressed();
        }
    }

    public void Init(MicrobeEditor editor)
    {
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public override void _Process(float delta)
    {
        // TODO: set these
        // editor.mutationPoints;
        // Constants.BASE_MUTATION_POINTS;
    }

    public void OnPlaceOrganelleClicked()
    {
        editor.PlaceOrganelle();
    }

    public void OnRemoveOrganelleClicked()
    {
        editor.RemoveOrganelle();
    }

    public void UpdateSize()
    {
        // var size = editor.ActualMicrobeSize;
    }

    public void UpdateGeneration(int generation)
    {
        // TODO: fix
    }

    public void UpdateSpeed()
    {
        // TODO: fix
        // var speed = editor.MicrobeSpeed;
    }

    /// <summary>
    ///   Called once when the mouse enters the editor GUI.
    /// </summary>
    internal void OnMouseEnter()
    {
    }

    /// <summary>
    ///   Called when the mouse is no longer hovering
    //    the editor GUI.
    /// </summary>
    internal void OnMouseExit()
    {
    }

    internal void SetUndoButtonStatus(bool v)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    internal void SetRedoButtonStatus(bool v)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    internal void NotifyFreebuild(object freebuilding)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    /// <summary>
    ///   lock / unlock the organelles  that need a nuclues
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: rename to something more sensible
    ///   </para>
    /// </remarks>
    internal void UpdateGuiButtonStatus(bool hasNucleus)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    internal void OnOrganelleToPlaceSelected(string organelle)
    {
        editor.ActiveActionName = organelle;

        // Make all buttons unselected except the one that is now selected
        foreach (Button element in organelleSelectionElements)
        {
            var selectedLabel = element.GetNode<Label>(
                "MarginContainer/VBoxContainer/SelectedLabelMargin/SelectedLabel");

            if (element.Name == organelle)
            {
                if (!element.Pressed)
                    element.Pressed = true;

                selectedLabel.Show();
            }
            else
            {
                selectedLabel.Hide();
            }
        }

        GD.Print("Editor action is now: " + editor.ActiveActionName);
    }

    internal void OnMembraneSelected(string membrane)
    {
        // todo: Send selected membrane to the editor script

        // Updates the GUI buttons based on current membrane
        foreach (Button element in membraneSelectionElements)
        {
            var selectedLabel = element.GetNode<Label>(
                "MarginContainer/VBoxContainer/SelectedLabelMargin/SelectedLabel");

            if (element.Name == membrane)
            {
                if (!element.Pressed)
                    element.Pressed = true;

                selectedLabel.Show();
            }
            else
            {
                selectedLabel.Hide();
            }
        }
    }

    internal void SetSpeciesInfo(string name, MembraneType membrane, Color colour,
        float rigidity)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    private void SetCellTab(string tab)
    {
        var structureTab = GetNode<Control>("CellEditor/LeftPanel/Panel/Structure");
        var membraneTab = GetNode<Control>("CellEditor/LeftPanel/Panel/Membrane");

        // Hide all
        structureTab.Hide();
        membraneTab.Hide();

        // Show selected
        if (tab == "structure")
        {
            structureTab.Show();
        }
        else if (tab == "membrane")
        {
            membraneTab.Show();
        }
        else
        {
            GD.PrintErr("Invalid tab");
        }
    }

    private void MenuButtonPressed()
    {
        var menu = GetNode<Control>("PauseMenu");

        if (menu.Visible)
        {
            menu.Hide();
        }
        else
        {
            menu.Show();
        }

        GUICommon.PlayButtonPressSound();
    }

    private void ExitPressed()
    {
        GUICommon.PlayButtonPressSound();
        GetTree().Quit();
    }
}
