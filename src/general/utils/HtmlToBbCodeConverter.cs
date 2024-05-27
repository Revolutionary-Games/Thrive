using System;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

/// <summary>
///   Converts HTML to Godot BBCode
/// </summary>
public class HtmlToBbCodeConverter
{
    private readonly StringBuilder stringBuilder = new();

    private readonly HtmlParser htmlParser;
    private readonly IElement dummyDom;

    private bool paragraphBreakQueued;

    public HtmlToBbCodeConverter()
    {
        htmlParser = new HtmlParser(new HtmlParserOptions
        {
            IsStrictMode = false,
        });

        dummyDom = htmlParser.ParseDocument(string.Empty).DocumentElement;
    }

    public static string SanitizeUrlForBbCode(string text)
    {
        return text.Replace("[", "%5B").Replace("]", "%5D").Replace("\"", "%22");
    }

    public string Convert(string html, out bool truncated)
    {
        truncated = false;
        paragraphBreakQueued = false;

        var document = htmlParser.ParseFragment(html, dummyDom);

        stringBuilder.Clear();

        foreach (var topLevelNode in document)
        {
            HandleNode(topLevelNode, ref truncated);
        }

        return stringBuilder.ToString();
    }

    private void HandleNode(INode node, ref bool truncated)
    {
        // TODO: unify as much of this handling with WikiUpdater.ConvertParagraphToBbcode as possible
        switch (node)
        {
            // Ignoring nodes we don't want to do anything with
            case IHtmlHeadElement:
            case IHtmlBodyElement:
                break;

            // Elements that only have post actions
            case IHtmlParagraphElement:
                break;

            case IText text:
            {
                if (truncated)
                    break;

                if (paragraphBreakQueued)
                {
                    paragraphBreakQueued = false;
                    AddLastTextIfDoesNotEndWithAlready("\n\n");
                }

                // var textToAdd = text.Data.Trim();
                var textToAdd = text.Data;

                if (stringBuilder.Length < 1)
                {
                    textToAdd = textToAdd.TrimStart();
                }

                if (stringBuilder.Length + textToAdd.Length >= Constants.MAX_NEWS_FEED_ITEM_LENGTH)
                {
                    textToAdd = textToAdd.Substring(0,
                        Math.Max(Constants.MAX_NEWS_FEED_ITEM_LENGTH - stringBuilder.Length, textToAdd.Length));
                    truncated = true;
                }

                // TODO: escaping Bbcode tags that are present in the text already

                stringBuilder.Append(textToAdd);

                if (truncated)
                    stringBuilder.Append("...");

                break;
            }

            case IHtmlAnchorElement anchorElement:
            {
                // We specifically ignore empty links (contained text), as AngleSharp seems to make multiple links
                // out of one for some reason
                if (!truncated && !string.IsNullOrWhiteSpace(anchorElement.Text))
                {
                    // We need to do our own clickable link colour setting for Godot
                    stringBuilder.Append(Constants.CLICKABLE_TEXT_BBCODE);

                    stringBuilder.Append($"[url={anchorElement.Href}]{anchorElement.Text.Trim()}[/url]");

                    stringBuilder.Append(Constants.CLICKABLE_TEXT_BBCODE_END);
                }

                // Anchor element children are not handled
                return;
            }

            // TODO: handling for the following element types:
            /*case IHtmlInlineFrameElement iFrame:
            {
                FlushTextIfPending();
                var match = LauncherConstants.YoutubeURLRegex.Match(iFrame.Source ?? string.Empty);

                if (match.Success)
                {
                    FinishCurrentItem();

                    currentItem = new Link(match.Groups[1].Value);
                }
                else
                {
                    logger.LogDebug("Removing iframe to: {Source}", iFrame.Source);
                }

                break;
            }

            case IHtmlImageElement:
            {
                // TODO: image support
                truncated = true;
                break;
            }

            case ISvgElement:
            {
                // TODO: svg support
                truncated = true;
                break;
            }*/

            case IHtmlUnorderedListElement or IHtmlOrderedListElement or IHtmlQuoteElement:
                stringBuilder.Append("[indent]");
                break;

            case IHtmlListItemElement:
            {
                AddLastTextIfDoesNotEndWithAlready("\n");
                stringBuilder.Append("- ");
                break;
            }

            case IHtmlHrElement:
            {
                AddLastTextIfDoesNotEndWithAlready("\n");
                stringBuilder.Append("---\n");
                break;
            }

            default:
            {
                var name = node.NodeName.ToLowerInvariant();

                if (name == TagNames.B || name == TagNames.Em || name == TagNames.Strong)
                {
                    stringBuilder.Append("[b]");
                    break;
                }

                if (name == TagNames.Strike)
                {
                    stringBuilder.Append("[s]");
                    break;
                }

                if (name == TagNames.Code || name ==
                    TagNames.Pre)
                {
                    stringBuilder.Append("[code]");
                    break;
                }

                if (name == TagNames.I)
                {
                    stringBuilder.Append("[i]");
                    break;
                }

                if (name == TagNames.U)
                {
                    stringBuilder.Append("[u]");
                    break;
                }

                if (name == TagNames.Dd || name == TagNames.Dt || name == TagNames.Big || name == TagNames.S ||
                    name == TagNames.Small || name == TagNames.Tt || name == TagNames.NoBr)
                {
                    // TODO: implement more text styling
                    break;
                }

                if (name is "aside" or "article")
                {
                    if (stringBuilder.Length > 0)
                        AddLastTextIfDoesNotEndWithAlready("\n");

                    stringBuilder.Append("[indent]");
                    break;
                }

                if (name == TagNames.Header)
                {
                    // TODO: bigger font

                    stringBuilder.Append("[center]");
                    break;
                }

                // Unknown node, so we are losing info here
                truncated = true;
                break;
            }
        }

        if (node.HasChildNodes)
        {
            foreach (var child in node.ChildNodes)
            {
                HandleNode(child, ref truncated);
            }
        }

        switch (node)
        {
            case IHtmlParagraphElement or IHtmlHeadingElement or IHtmlBreakRowElement or IHtmlDivElement:
            {
                paragraphBreakQueued = true;
                break;
            }

            case IHtmlUnorderedListElement or IHtmlOrderedListElement or IHtmlQuoteElement:
            {
                stringBuilder.Append("[/indent]\n");
                break;
            }

            case IHtmlSpanElement:
            {
                if (stringBuilder.Length > 0)
                    AddLastTextIfDoesNotEndWithAlready(" ");

                break;
            }

            default:
            {
                var name = node.NodeName.ToLowerInvariant();

                if (name == TagNames.B || name == TagNames.Em || name == TagNames.Strong)
                {
                    stringBuilder.Append("[/b]");
                    break;
                }

                if (name == TagNames.Strike)
                {
                    stringBuilder.Append("[/s]");
                    break;
                }

                if (name == TagNames.Code || name ==
                    TagNames.Pre)
                {
                    stringBuilder.Append("[/code]");
                    break;
                }

                if (name == TagNames.I)
                {
                    stringBuilder.Append("[/i]");
                    break;
                }

                if (name == TagNames.U)
                {
                    stringBuilder.Append("[/u]");
                    break;
                }

                if (name == TagNames.Dd || name == TagNames.Dt || name == TagNames.Big || name == TagNames.S ||
                    name == TagNames.Small || name == TagNames.Tt || name == TagNames.NoBr)
                {
                    // This is placeholder for closing styles once more are implemented
                    break;
                }

                if (name is "aside" or "article")
                {
                    if (stringBuilder.Length > 0)
                        AddLastTextIfDoesNotEndWithAlready("\n");

                    stringBuilder.Append("[/indent]\n");
                    break;
                }

                if (name == TagNames.Header)
                {
                    // TODO: bigger font

                    stringBuilder.Append("[/center]");
                }

                break;
            }
        }
    }

    // The following methods are general utility methods shared between the feed parser in the common module
    // TODO: combine these separate implementations into one once Thrive uses a general module
    private void AddLastTextIfDoesNotEndWithAlready(string text)
    {
        if (stringBuilder.Length < text.Length)
        {
            stringBuilder.Append(text);
        }

        bool match = true;

        for (int i = text.Length; i > 0; --i)
        {
            if (stringBuilder[stringBuilder.Length - i] != text[text.Length - i])
            {
                match = false;
                break;
            }
        }

        if (!match)
            stringBuilder.Append(text);
    }
}
