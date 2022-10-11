using Godot;

public class FossilisationButton : TextureButton
{
    public Microbe AttachedMicrobe = null!;

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