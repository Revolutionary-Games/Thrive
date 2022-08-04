using Godot;
using System;

public class FossilisationButton : TextureButton
{
    public Microbe AttachedMicrobe = null!;

    public Action<Species> OnFossilisationDialogOpened = null!;

    public void UpdatePosition()
    {        
        RectGlobalPosition = GetViewport().GetCamera().UnprojectPosition(AttachedMicrobe.GlobalTransform.origin);
    }

    private void OnPressed()
    {
        OnFossilisationDialogOpened.Invoke(AttachedMicrobe.Species);
    }
}