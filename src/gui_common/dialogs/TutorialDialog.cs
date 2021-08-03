public class TutorialDialog : CustomWindowDialog
{
    public override void _Ready()
    {
        isExclusive = true;
        isEscapeCloseable = false;
        base._Ready();
    }
}
