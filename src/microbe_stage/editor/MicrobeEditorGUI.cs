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
    }

    public void OnRemoveOrganelleClicked()
    {
    }

    public void UpdateSize()
    {
        // var size = editor.ActualMicrobeSize;
    }

    public void UpdateGeneration(int generation)
    {
    }

    public void UpdateSpeed()
    {
        // var speed = editor.MicrobeSpeed;
    }

    internal void SetUndoButtonStatus(bool v)
    {
        throw new NotImplementedException();
    }

    internal void SetRedoButtonStatus(bool v)
    {
        throw new NotImplementedException();
    }

    internal void NotifyFreebuild(object freebuilding)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    internal void OnOrganelleToPlaceSelected(string organelle)
    {
        throw new NotImplementedException();
    }

    internal void SetSpeciesInfo(string name, MembraneType membrane, Color colour,
        float rigidity)
    {
        throw new NotImplementedException();
    }
}
