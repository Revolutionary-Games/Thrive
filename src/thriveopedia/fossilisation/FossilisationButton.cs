using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Button shown above organisms in pause mode to fossilise (save) them.
/// </summary>
public partial class FossilisationButton : TextureButton
{
#pragma warning disable CA2213
    [Export]
    public Texture2D AlreadyFossilisedTexture = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The entity (organism) this button is attached to.
    /// </summary>
    public Entity AttachedEntity;

    /// <summary>
    ///   Whether this species has already been fossilised.
    /// </summary>
    private bool alreadyFossilised;

#pragma warning disable CA2213

    /// <summary>
    ///   Active camera grabbed when this is created in order to properly position this on that camera's view
    /// </summary>
    private Camera3D camera = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnFossilisationDialogOpenedEventHandler(FossilisationButton button);

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

        camera = GetViewport().GetCamera3D();
    }

    /// <summary>
    ///   Update the position of this button, e.g. to reflect player.Zooming.
    /// </summary>
    public void UpdatePosition()
    {
        if (camera is not { Current: true })
            camera = GetViewport().GetCamera3D();

        // If the entity is removed (e.g. forcefully despawned)
        if (!AttachedEntity.IsAlive || !AttachedEntity.Has<WorldPosition>())
        {
            this.DetachAndQueueFree();
            return;
        }

        GlobalPosition = camera.UnprojectPosition(AttachedEntity.Get<WorldPosition>().Position);
    }

    private void OnPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnFossilisationDialogOpened, this);
    }
}
