using Godot;

/// <summary>
///   Button shown above organisms in pause mode to fossilise (save) them.
/// </summary>
public class FossilisationButton : TextureButton
{
    [Export]
    public Texture AlreadyFossilisedTexture = null!;

    /// <summary>
    ///   The organism this button is attached to.
    /// </summary>
    public Spatial AttachedOrganism = null!;

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

        RectGlobalPosition = camera.UnprojectPosition(AttachedOrganism.GlobalTransform.origin);
    }

    private void OnPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnFossilisationDialogOpened), this);
    }
}
