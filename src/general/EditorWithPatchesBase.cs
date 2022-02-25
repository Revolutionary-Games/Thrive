using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   One step more specialized editor that supports patch migrations
/// </summary>
public abstract class EditorWithPatchesBase<TGUI, TAction, TStage> : EditorBase<TGUI, TAction, TStage>, IEditorWithPatches
    where TGUI : class, IEditorGUI
    where TAction : MicrobeEditorAction
    where TStage : Node, IReturnableGameState
{
    /// <summary>
    ///   Where the player wants to move after editing
    /// </summary>
    [JsonProperty]
    protected Patch? targetPatch;

    /// <summary>
    ///   When false the player is no longer allowed to move patches (other than going back to where they were at the
    ///   start)
    /// </summary>
    [JsonProperty]
    protected bool canStillMove;

    [JsonProperty]
    protected Patch playerPatchOnEntry = null!;

    /// <summary>
    ///   Returns the current patch the player is in
    /// </summary>
    [JsonIgnore]
    public Patch CurrentPatch => targetPatch ?? playerPatchOnEntry;

    public bool IsPatchMoveValid(Patch? patch)
    {
        if (patch == null)
            return false;

        var from = CurrentPatch;

        // Can't go to the patch you are in
        if (from == patch)
            return false;

        // Can return to the patch the player started in, as a way to "undo" the change
        if (patch == playerPatchOnEntry)
            return true;

        // If we are freebuilding, check if the target patch is connected by any means, then it is allowed
        if (FreeBuilding && CurrentPatch.GetAllConnectedPatches().Contains(patch))
            return true;

        // Can't move if out of moves
        if (!canStillMove)
            return false;

        // Need to have a connection to move
        foreach (var adjacent in from.Adjacent)
        {
            if (adjacent == patch)
                return true;
        }

        return false;
    }

    public virtual void SetPlayerPatch(Patch? patch)
    {
        if (!IsPatchMoveValid(patch))
            throw new ArgumentException("can't move to the specified patch");

        // One move per editor cycle allowed, unless freebuilding
        if (!FreeBuilding)
            canStillMove = false;

        if (patch == playerPatchOnEntry)
        {
            targetPatch = null;

            // Undoing the move, restores the move
            canStillMove = true;
        }
        else
        {
            targetPatch = patch;
        }
    }

    public override void OnFinishEditing()
    {
        base.OnFinishEditing();

        // Move patches
        if (targetPatch != null)
        {
            GD.Print(GetType().Name, ": applying player move to patch: ",
                TranslationServer.Translate(targetPatch.Name));
            CurrentGame!.GameWorld.Map.CurrentPatch = targetPatch;

            // Add the edited species to that patch to allow the species to gain population there
            // TODO: Log player species' migration
            CurrentGame.GameWorld.Map.CurrentPatch.AddSpecies(EditedBaseSpecies, 0);
        }
    }

    protected override void InitEditorFresh()
    {
        base.InitEditorFresh();

        targetPatch = null;

        playerPatchOnEntry = CurrentGame!.GameWorld.Map.CurrentPatch ??
            throw new InvalidOperationException("Map current patch needs to be set before entering the editor");

        canStillMove = true;
    }
}
