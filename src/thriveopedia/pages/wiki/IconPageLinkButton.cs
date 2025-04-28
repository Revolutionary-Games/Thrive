using Godot;

/// <summary>
///   Button which links to a Thriveopedia page and uses an icon. Added automatically via code.
/// </summary>
/// <seealso cref="TextPageLinkButton"/>
public partial class IconPageLinkButton : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private Button button = null!;

    [Export]
    private Label label = null!;
#pragma warning restore CA2213

    public string IconPath { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string PageName { get; set; } = null!;

    public override void _Ready()
    {
        base._Ready();

        button.Icon = GD.Load<Texture2D>(IconPath);
        label.Text = DisplayName;
    }

    public void OnPressed()
    {
        ThriveopediaManager.OpenPage(PageName);
    }
}
