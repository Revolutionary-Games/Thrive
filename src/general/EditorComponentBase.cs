using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component base class (each editor tab is roughly one component)
/// </summary>
/// <typeparam name="TEditor">The type of editor this component is contained in</typeparam>
[JsonObject(MemberSerialization.OptIn)]
public abstract class EditorComponentBase<TEditor> : ControlWithInput, IEditorComponent
    where TEditor : Godot.Object, IEditor
{
    [Export]
    public NodePath FinishOrNextButtonPath = null!;

    private Button finishOrNextButton = null!;

    // TODO: rename
    protected AudioStream unableToPlaceHexSound = null!;

    private TEditor? editor;

    /// <summary>
    ///   If this is set then the next / finish button on this tab is the next button.
    ///   This or <see cref="OnFinish"/> must be set before <see cref="Init"/> is called.
    /// </summary>
    public Action? OnNextTab { get; set; }

    public Func<List<EditorUserOverride>?, bool>? OnFinish { get; set; }

    protected TEditor Editor => editor ?? throw new InvalidOperationException("Editor component not initialized");

    public override void _Ready()
    {
        base._Ready();

        if (editor == null)
            throw new InvalidOperationException("Editor component not initialized before _Ready was called");

        finishOrNextButton = GetNode<Button>(FinishOrNextButtonPath);
    }

    public virtual void Init(TEditor owningEditor, bool fresh)
    {
        if (OnNextTab != null)
        {
            // TODO: do we need to do something here?
        }
        else if (OnFinish != null)
        {
            // Turn the next button into the finish button
            throw new NotImplementedException();
        }
        else
        {
            throw new InvalidOperationException("Either next tab or finish callback needs to be set");
        }

        editor = owningEditor;

        unableToPlaceHexSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/click_place_blocked.ogg");
    }

    /// <summary>
    ///   This exists for interface compatibility
    /// </summary>
    public void Init(IEditor owningEditor, bool fresh)
    {
        Init((TEditor)owningEditor, fresh);
    }

    public override void _Notification(int what)
    {
        // Rebuilds and recalculates all value dependent UI elements on language change
        if (what == NotificationTranslationChanged)
        {
            OnTranslationsChanged();
        }
    }

    /// <summary>
    ///   Called
    /// </summary>
    /// <param name="species">
    ///   The species that was setup, accessing more specific data through <see cref="Editor"/> rather than casting
    ///   to a derived class is recommended.
    /// </param>
    public virtual void OnEditorSpeciesSetup(Species species)
    {
    }

    public virtual bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        return true;
    }

    /// <summary>
    ///   Applies the new (edited) state that this editor component handled
    /// </summary>
    public abstract void OnFinishEditing();

    internal void PlayInvalidActionSound()
    {
        GUICommon.Instance.PlayCustomSound(unableToPlaceHexSound, 0.4f);
    }

    /// <summary>
    ///   Rebuilds and recalculates all value dependent UI elements on language change
    /// </summary>
    protected abstract void OnTranslationsChanged();

    /// <summary>
    ///   Registers tooltip for the already existing Controls in the editor GUI
    /// </summary>
    protected virtual void RegisterTooltips()
    {
        // TODO: tooltip probably needs to change whether this is the finish or next button
        finishOrNextButton.RegisterToolTipForControl("finishButton", "editor");
    }

    protected virtual void NextOrFinishClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (OnFinish != null)
        {
            if (OnFinish!.Invoke(null))
            {
                // To prevent being clicked twice
                finishOrNextButton.MouseFilter = MouseFilterEnum.Ignore;
            }
        }
        else
        {
            OnNextTab!.Invoke();
        }
    }

    public abstract void UpdateUndoRedoButtons(bool canUndo, bool canRedo);
    public abstract void OnInsufficientMP(bool playSound = true);
    public abstract void OnActionBlockedWhileAnotherIsInProgress();

    public void OnInvalidAction()
    {
        PlayInvalidActionSound();
    }

    /// <summary>
    ///   Notify this component about the freebuild status. Many components don't need to react to this, they can
    ///   instead just check <see cref="IEditor.FreeBuilding"/>
    /// </summary>
    /// <param name="freeBuilding">True if freebuild mode is on</param>
    public virtual void NotifyFreebuild(bool freeBuilding)
    {
    }

    public abstract void OnMutationPointsChanged(int mutationPoints);
}
