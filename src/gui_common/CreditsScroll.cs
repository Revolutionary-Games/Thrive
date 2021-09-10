using Godot;

public class CreditsScroll : Container
{
    private bool scrolling = true;
    private float scrollOffset;

    private GameCredits credits;

    private Label label;

    [Export]
    public float ScrollSpeed { get; set; } = 20;

    public override void _Ready()
    {
        credits = SimulationParameters.Instance.GetCredits();

        label = new Label
        {
            Text = "Stuff...",
        };

        AddChild(label);
        label.RectPosition = new Vector2(200, 600 - scrollOffset);
    }

    public override void _Process(float delta)
    {
        if (!scrolling)
            return;

        scrollOffset += delta * ScrollSpeed;
        label.RectPosition = new Vector2(200, 600 - scrollOffset);
    }

    public void Restart()
    {
        scrolling = true;
        scrollOffset = 0;
    }

    public void Pause()
    {
        scrolling = false;
    }
}
