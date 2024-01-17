using Godot;

/// <summary>
///   Formatted section of the main article content of a Thriveopedia page. Consists of a single rich text body and an
///   optional heading.
/// </summary>
public class WikiPageSection : VBoxContainer
{
    [Export]
    public NodePath? HeadingPath;

    [Export]
    public NodePath SeparatorPath = null!;

    [Export]
    public NodePath BodyPath = null!;

#pragma warning disable CA2213
    private Label? heading;
    private HSeparator? separator;
    private CustomRichTextLabel? body;
#pragma warning restore CA2213

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

        heading = GetNode<Label>(HeadingPath);
        separator = GetNode<HSeparator>(SeparatorPath);
        body = GetNode<CustomRichTextLabel>(BodyPath);
        UpdateText();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (HeadingPath != null)
            {
                HeadingPath.Dispose();
                SeparatorPath.Dispose();
                BodyPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateText()
    {
        if (heading == null || separator == null || body == null)
            return;

        heading.Visible = headingText != null;
        separator.Visible = headingText != null;

        heading.Text = headingText ?? string.Empty;
        body.ExtendedBbcode = bodyText;
    }
}
