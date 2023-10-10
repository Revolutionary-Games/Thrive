using Godot;

public class WikiPageSection : VBoxContainer
{
    private Label heading = null!;
    private HSeparator separator = null!;
    private CustomRichTextLabel body = null!;

    private string? headingText;
    private string bodyText = null!;

    public string? HeadingText
    {
        get => headingText;
        set
        {
            headingText = value;
            UpdateText();
        }
    }

    public string BodyText
    {
        get => bodyText;
        set
        {
            bodyText = value;
            UpdateText();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        heading = GetNode<Label>("Heading");
        separator = GetNode<HSeparator>("HSeparator");
        body = GetNode<CustomRichTextLabel>("Body");
        UpdateText();
    }

    private void UpdateText()
    {
        if (heading == null || separator == null || body == null)
            return;
        
        heading.Visible = headingText != null;
        separator.Visible = headingText != null;

        heading.Text = headingText ?? "";
        body.ExtendedBbcode = bodyText;        
    }
}
