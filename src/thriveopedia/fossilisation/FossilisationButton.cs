﻿using Godot;

/// <summary>
///   Button shown above organisms in pause mode to fossilise (save) them.
/// </summary>
public class FossilisationButton : TextureButton
{
    [Export]
    public Texture AlreadyFossilisedTexture = null!;

    /// <summary>
    ///   The entity (organism) this button is attached to.
    /// </summary>
    public IEntity AttachedEntity = null!;

    /// <summary>
    ///   Whether this species has already been fossilised.
    /// </summary>
    private bool alreadyFossilised;

    private Camera camera = null!;

    [Signal]
    public delegate void OnFossilisationDialogOpened(FossilisationButton button);

    /// <summary>
    ///   Whether this species has already been fossilised.
    /// </summary>
    public bool AlreadyFossilised
    {
        get => alreadyFossilised;
        set
        {
            alreadyFossilised = value;

            if (alreadyFossilised)
                TextureNormal = AlreadyFossilisedTexture;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        camera = GetViewport().GetCamera();
    }

    /// <summary>
    ///   Update the position of this button, e.g. to reflect player zooming.
    /// </summary>
    public void UpdatePosition()
    {
        if (camera is not { Current: true })
            camera = GetViewport().GetCamera();

        // If the entity is removed (e.g. forcefully despawned)
        if (AttachedEntity.AliveMarker.Alive == false)
        {
            this.DetachAndQueueFree();
            return;
        }

        RectGlobalPosition = camera.UnprojectPosition(AttachedEntity.EntityNode.GlobalTransform.origin);
    }

    private void OnPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnFossilisationDialogOpened), this);
    }
}
