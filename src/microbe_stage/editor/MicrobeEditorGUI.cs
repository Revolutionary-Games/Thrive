using System;
using Godot;

/// <summary>
///   Main class managing the microbe editor GUI
/// </summary>
public class MicrobeEditorGUI : Node
{
    private MicrobeEditor editor;

    public override void _Ready()
    {
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
        // TODO: fix
        throw new NotImplementedException();
    }

    internal void SetSpeciesInfo(string name, MembraneType membrane, Color colour,
        float rigidity)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }
}
