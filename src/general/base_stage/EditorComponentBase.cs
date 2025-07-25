﻿using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component base class (each editor tab is roughly one component)
/// </summary>
/// <typeparam name="TEditor">The type of editor this component is contained in</typeparam>
[JsonObject(MemberSerialization.OptIn)]
[GodotAbstract]
public partial class EditorComponentBase<TEditor> : ControlWithInput, IEditorComponent
    where TEditor : IEditor
{
#pragma warning disable CA2213
    protected AudioStream unableToPerformActionSound = null!;

    [Export]
    protected Button finishOrNextButton = null!;
#pragma warning restore CA2213

    private TEditor? editor;

    private double invalidSoundCooldown;

    protected EditorComponentBase()
    {
    }

    /// <summary>
    ///   If this is set, then the next / finish button on this tab is the next button.
    ///   This or <see cref="OnFinish"/> must be set before <see cref="Init(TEditor,bool)"/> is called.
    /// </summary>
    public Action? OnNextTab { get; set; }

    public Func<List<EditorUserOverride>?, bool>? OnFinish { get; set; }

    /// <summary>
    ///   Subeditor components don't require all functionality, so they override this to disable some initialization
    ///   logic
    /// </summary>
    [JsonIgnore]
    public virtual bool IsSubComponent => false;

    protected TEditor Editor => editor ?? throw new InvalidOperationException("Editor component not initialized");

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
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
            // This is the default state, so we don't need to do anything here
        }
        else if (OnFinish != null)
        {
            // Turn the next button into the finish button
            finishOrNextButton.Text = Localization.Translate("CONFIRM_CAPITAL");
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

    public override void _Process(double delta)
    {
        base._Process(delta);

        invalidSoundCooldown -= delta;
    }

    public virtual void OnEditorReady()
    {
        // Late initialisation stuff can go here (usually overridden by component types that need that)
        // Note that this only happens when not loading a save, as when loading a save auto-evo etc. data should be
        // ready immediately
    }

    /// <summary>
    ///   Called after the editor has a valid species to modify. Called shortly after <see cref="Init(TEditor,bool)"/>
    /// </summary>
    /// <param name="species">
    ///   The species that was set up, accessing more specific data through <see cref="Editor"/> rather than casting
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
    public virtual void OnFinishEditing()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public virtual void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public virtual void OnInsufficientMP(bool playSound = true)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public virtual void OnActionBlockedWhileAnotherIsInProgress()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public void OnInvalidAction()
    {
        PlayInvalidActionSound();
    }

    public virtual void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Notify this component about the freebuild status. Many components don't need to react to this, they can
    ///   instead just check <see cref="IEditor.FreeBuilding"/>
    /// </summary>
    /// <param name="freeBuilding">True if freebuild mode is on</param>
    public virtual void NotifyFreebuild(bool freeBuilding)
    {
    }

    public virtual void OnMutationPointsChanged(double mutationPoints)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    public virtual void OnLightLevelChanged(float lightLevel)
    {
    }

    internal void PlayInvalidActionSound()
    {
        // To avoid multiple sounds overlapping, there's a cooldown
        if (invalidSoundCooldown <= 0)
        {
            GUICommon.Instance.PlayCustomSound(unableToPerformActionSound, 0.4f);
            invalidSoundCooldown = 0.4f;
        }
    }

    /// <summary>
    ///   Blocks tab switch (and shows a tooltip) if there's an in-progress action
    /// </summary>
    /// <returns>Whether the tab switch was blocked</returns>
    protected bool BlockTabSwitchIfInProgressAction(bool actionInProgess)
    {
        if (actionInProgess)
        {
            ToolTipManager.Instance.ShowPopup(Localization.Translate("TAB_CHANGE_BLOCKED_WHILE_ACTION_IN_PROGRESS"),
                1.5f);

            return true;
        }

        return false;
    }

    /// <summary>
    ///   Rebuilds and recalculates all value-dependent UI elements on language change
    /// </summary>
    protected virtual void OnTranslationsChanged()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    /// <summary>
    ///   Registers tooltip for the already existing Controls in the editor GUI
    /// </summary>
    protected virtual void RegisterTooltips()
    {
        if (IsSubComponent)
            return;

        // By default, this is the next button
        finishOrNextButton.RegisterToolTipForControl("nextTabButton", "editor");
    }

    protected virtual void NextOrFinishClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!ModalManager.Instance.TryCancelModals())
        {
            GD.PrintErr("Cannot close open modals before continuing, not triggering next / finish action");
            return;
        }

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
}
