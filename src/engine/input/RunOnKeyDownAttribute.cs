using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is pressed down.
/// </summary>
/// <example>
///   [RunOnKeyDown("screenshot")]
///   public void TakeScreenshotPressed()
/// </example>
public class RunOnKeyDownAttribute : RunOnKeyAttribute
{
    public RunOnKeyDownAttribute(string godotInputName) : base(godotInputName)
    {
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) && !before)
        {
            return CallMethod();
        }

        return false;
    }

    public override void OnProcess(float delta)
    {
    }
}
