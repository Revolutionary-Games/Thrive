using System.Text.RegularExpressions;

/// <summary>
///   Converts markdown to bbcode for displaying
/// </summary>
public static class MarkdownToBbCodeConverter
{
    private static readonly Regex MarkdownLink = new(@"\[([^\]]+)\]\((http[^\)]+)\)", RegexOptions.Multiline);

    private static readonly Regex BasicHtmlLink =
        new(@"<a\s+[^>]*href=\""(http\S+)\""[^>]*>\s*(.+)\s*</a\s*>", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex UrlWithoutMarkup = new(@"([^=])(https?:\/\/[^\s)\]}]+)");

    public static string Convert(string markdown)
    {
        // Convert markdown links
        var converted = MarkdownLink.Replace(markdown,
            $"{Constants.CLICKABLE_TEXT_BBCODE}[url=$2]$1[/url]{Constants.CLICKABLE_TEXT_BBCODE_END}");

        // Convert HTML style links
        converted = BasicHtmlLink.Replace(converted,
            $"{Constants.CLICKABLE_TEXT_BBCODE}[url=$1]$2[/url]{Constants.CLICKABLE_TEXT_BBCODE_END}");

        // Handle plain URLs not contained in any kind of markup
        converted = UrlWithoutMarkup.Replace(converted,
            $"$1{Constants.CLICKABLE_TEXT_BBCODE}[url=$2]$2[/url]{Constants.CLICKABLE_TEXT_BBCODE_END}");

        // TODO: convert text emphasis

        return converted;
    }
}
