using Godot;

public class FossilisationButton : TextureButton
{
    [Export]
    public Texture AlreadyFossilisedTexture = null!;

    private bool alreadyFossilised;

    public Microbe AttachedMicrobe = null!;
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

    [Signal]
    public delegate void OnFossilisationDialogOpened(FossilisationButton button);

    public void UpdatePosition()
    {        
        RectGlobalPosition = GetViewport().GetCamera().UnprojectPosition(AttachedMicrobe.GlobalTransform.origin);
    }

    private void OnPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnFossilisationDialogOpened), this);
    }
}