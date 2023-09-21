using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component base class (each editor tab is roughly one component)
/// </summary>
/// <typeparam name="TEditor">The type of editor this component is contained in</typeparam>
[JsonObject(MemberSerialization.OptIn)]
public abstract class EditorComponentBase<TEditor> : ControlWithInput, IEditorComponent
    where TEditor : IEditor
{
    [Export]
    public NodePath? FinishOrNextButtonPath;

#pragma warning disable CA2213
    protected AudioStream unableToPerformActionSound = null!;

    private Button finishOrNextButton = null!;
#pragma warning restore CA2213

    private TEditor? editor;

    /// <summary>
    ///   If this is set then the next / finish button on this tab is the next button.
    ///   This or <see cref="OnFinish"/> must be set before <see cref="Init(TEditor,bool)"/> is called.
    /// </summary>
    public Action? OnNextTab { get; set; }

    public Func<List<EditorUserOverride>?, bool>? OnFinish { get; set; }

    /// <summary>
    ///   Sub editor components don't require all functionality, so they override this to disable some initialization
    ///   logic
    /// </summary>
    [JsonIgnore]
    public virtual bool IsSubComponent => false;

    protected TEditor Editor => editor ?? throw new InvalidOperationException("Editor component not initialized");

    public override void _Ready()
    {
        base._Ready();

        // TODO: redesign this check. This currently fails as the child nodes get _Ready called first and this also
        // makes it harder to directly run the individual editor component scenes from Godot editor to debug them
        // if (editor == null)
        //     throw new InvalidOperationException("Editor component not initialized before _Ready was called");

        if (IsSubComponent)
            return;

        finishOrNextButton = GetNode<Button>(FinishOrNextButtonPath);
    }

    public virtual void Init(TEditor owningEditor, bool fresh)
    {
        editor = owningEditor;

        unableToPerformActionSound =
            GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/click_place_blocked.ogg");

        if (IsSubComponent)
            return;

        if (OnNextTab != null)
        {
            // This is the default state so we don't need to do anything here
        }
        else if (OnFinish != null)
        {
            // Turn the next button into the finish button
            finishOrNextButton.Text = TranslationServer.Translate("CONFIRM_CAPITAL");
            finishOrNextButton.UnRegisterFirstToolTipForControl();
            finishOrNextButton.RegisterToolTipForControl("finishButton", "editor");
        }
        else
        {
            throw new InvalidOperationException("Either next tab or finish callback needs to be set");
        }
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

    public abstract void UpdateUndoRedoButtons(bool canUndo, bool canRedo);
    public abstract void OnInsufficientMP(bool playSound = true);
    public abstract void OnActionBlockedWhileAnotherIsInProgress();

    public void OnInvalidAction()
    {
        PlayInvalidActionSound();
    }

    public abstract void OnValidAction();

    /// <summary>
    ///   Notify this component about the freebuild status. Many components don't need to react to this, they can
    ///   instead just check <see cref="IEditor.FreeBuilding"/>
    /// </summary>
    /// <param name="freeBuilding">True if freebuild mode is on</param>
    public virtual void NotifyFreebuild(bool freeBuilding)
    {
    }

    public abstract void OnMutationPointsChanged(int mutationPoints);

    public virtual void OnLightLevelChanged(float lightLevel)
    {
    }

    internal void PlayInvalidActionSound()
    {
        GUICommon.Instance.PlayCustomSound(unableToPerformActionSound, 0.4f);
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
        if (IsSubComponent)
            return;

        // By default this is the next button
        finishOrNextButton.RegisterToolTipForControl("nextTabButton", "editor");
    }

    protected virtual void NextOrFinishClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // If a modal won't close, don't do anything
        if (!ModalManager.Instance.ClearModals())
            return;

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FinishOrNextButtonPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
