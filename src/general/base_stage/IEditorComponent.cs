using System;
using System.Collections.Generic;

public interface IEditorComponent
{
    /// <summary>
    ///   If this is set then the next / finish button on this tab is the next button.
    ///   This or <see cref="OnFinish"/> must be set before <see cref="Init"/> is called.
    /// </summary>
    public Action? OnNextTab { get; set; }

    public Func<List<EditorUserOverride>?, bool>? OnFinish { get; set; }

    public bool Visible { get; }

    public void Init(IEditor owningEditor, bool fresh);

    /// <summary>
    ///   Called when the species data is ready in the editor
    /// </summary>
    /// <param name="species">
    ///   The species that was setup, accessing more specific data through
    ///   <see cref="EditorComponentBase{TEditor}.Editor"/> rather than casting to a derived class is recommended.
    /// </param>
    public void OnEditorSpeciesSetup(Species species);

    public bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides);

    /// <summary>
    ///   Applies the new (edited) state that this editor component handled
    /// </summary>
    public void OnFinishEditing();

    public void UpdateUndoRedoButtons(bool canUndo, bool canRedo);
    public void OnInsufficientMP(bool playSound);
    public void OnActionBlockedWhileAnotherIsInProgress();

    public void OnInvalidAction();

    /// <summary>
    ///   Triggered when a valid action occurs. Can be used to implement sounds for example or other GUI feedback.
    /// </summary>
    /// <param name="actions">
    ///   The valid actions, can be used to filter when something special should happen based on the action type
    /// </param>
    public void OnValidAction(IEnumerable<CombinableActionData> actions);

    /// <summary>
    ///   Notify this component about the freebuild status. Many components don't need to react to this, they can
    ///   instead just check <see cref="IEditor.FreeBuilding"/>
    /// </summary>
    /// <param name="freeBuilding">True if freebuild mode is on</param>
    public void NotifyFreebuild(bool freeBuilding);

    public void OnMutationPointsChanged(int mutationPoints);

    /// <summary>
    ///   Called when <see cref="IEditor.DayLightFraction"/> is changed.
    /// </summary>
    /// <param name="dayLightFraction">The editor's new fraction of daylight.</param>
    public void OnLightLevelChanged(float dayLightFraction);
}
