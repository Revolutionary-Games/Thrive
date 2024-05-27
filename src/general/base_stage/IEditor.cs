using System.Collections.Generic;
using Godot;

/// <summary>
///   Interface extracted to make GUI generic parameters work
/// </summary>
public interface IEditor : ISaveLoadedTracked
{
    /// <summary>
    ///   The number of mutation points left
    /// </summary>
    public int MutationPoints { get; }

    /// <summary>
    ///   When true nothing costs MP
    /// </summary>
    public bool FreeBuilding { get; }

    /// <summary>
    ///   True when there is an action that can be canceled
    /// </summary>
    public bool CanCancelAction { get; }

    /// <summary>
    ///   True once fade transition is finished when entering editor
    /// </summary>
    public bool TransitionFinished { get; }

    /// <summary>
    ///   True when the editor view is active and the user can perform an action (for example place an organelle)
    /// </summary>
    public bool ShowHover { get; set; }

    /// <summary>
    ///   Root node under which editor components should put their 3D space Nodes (placed things, editor controls etc.)
    /// </summary>
    public Node3D RootOfDynamicallySpawned { get; }

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    public GameProperties CurrentGame { get; }

    /// <summary>
    ///   Access to the base class typed species that is edited, for use in various things in the editor systems
    ///   that don't need to care about the concrete type of species being edited
    /// </summary>
    public Species EditedBaseSpecies { get; }

    /// <summary>
    ///   True once auto-evo (and possibly other stuff) the editor needs to wait for is ready
    /// </summary>
    public bool EditorReady { get; }

    public float DayLightFraction { get; set; }

    /// <summary>
    ///   Cancels the current editor action if possible
    /// </summary>
    /// <returns>True if canceled</returns>
    public bool CancelCurrentAction();

    public int WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions);

    /// <summary>
    ///   Perform all actions through this to make undo and redo work
    /// </summary>
    /// <returns>True when the action was successful</returns>
    /// <remarks>
    ///   <para>
    ///     This takes in a base action type so that this interface doesn't need to depend on the specific action
    ///     type of the editor which causes some pretty nasty generic constraint interdependencies
    ///   </para>
    /// </remarks>
    public bool EnqueueAction(ReversibleAction action);

    /// <summary>
    ///   Adds editor state specific context to given sequence of actions. <see cref="EnqueueAction"/> and
    ///   <see cref="WhatWouldActionsCost"/> perform this automatically. Only adds the context if not missing to give
    ///   flexibility for editor components to add their custom action context that is not overridden.
    /// </summary>
    /// <param name="actions">The action data to add the context to</param>
    public void AddContextToActions(IEnumerable<CombinableActionData> actions);

    public void NotifyUndoRedoStateChanged();

    public bool CheckEnoughMPForAction(int cost);

    public void OnInsufficientMP(bool playSound = true);

    public void OnActionBlockedWhileMoving();

    public void OnInvalidAction();

    public void OnValidAction(IEnumerable<CombinableActionData> actions);

    /// <summary>
    ///   Request from the user to exit the editor anyway
    /// </summary>
    /// <param name="userOverrides">
    ///   The new user overrides to be used when exiting. Caller should add their own override that was just approved
    ///   to the overrides they were given through <see cref="EditorComponentBase{TEditor}.CanFinishEditing"/>.
    /// </param>
    /// <returns>True if editing was able to finish now, false if still some component is not ready to exit</returns>
    public bool RequestFinishEditingWithOverride(List<EditorUserOverride> userOverrides);
}
