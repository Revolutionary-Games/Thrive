using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Godot;

/// <summary>
///   Manages downloading and parsing the Thrive news feed into Godot-friendly bbcode
/// </summary>
public static class ThriveNewsFeed
{
    private static readonly Lazy<Task<IReadOnlyCollection<FeedItem>>> FeedItemsTask = new(StartFetchingFeed);

    /// <summary>
    ///   Gets the news feed contents. If already retrieved returns the existing copy
    /// </summary>
    /// <returns>Task that resolves with the feed content once ready (or an error representing item)</returns>
    public static Task<IReadOnlyCollection<FeedItem>> GetFeedContents()
    {
        return FeedItemsTask.Value;
    }

    private static Task<IReadOnlyCollection<FeedItem>> StartFetchingFeed()
    {
        GD.Print("Beginning Thrive news feed fetch");
        var task = new Task<IReadOnlyCollection<FeedItem>>(FetchFeed);

        TaskExecutor.Instance.AddTask(task);

        return task;
    }

    private static IReadOnlyCollection<FeedItem> FetchFeed()
    {
        using var client = SetupHttpClient();

        var timeout = TimeSpan.FromMinutes(1);

        try
        {
            // This done a bit awkwardly to allow running this easily on our executor pool
            var responseTask = client.GetAsync(Constants.MainSiteFeedURL, HttpCompletionOption.ResponseHeadersRead);
            if (!responseTask.Wait(timeout))
                throw new Exception("Download timed out");

            var response = responseTask.Result;

            response.EnsureSuccessStatusCode();

            var readTask = response.Content.ReadAsStreamAsync();

            if (!readTask.Wait(timeout))
                throw new Exception("Data read timed out");

            var responseStream = readTask.Result;
            var feedDocument = XDocument.Load(responseStream);

            var items = ExtractFeedItems(feedDocument);

            return ParseHtmlItemsToBbCode(items);
        }
        catch (Exception e)
        {
            return new[] { CreateErrorItem(e.Message) };
        }
    }

    private static HttpClient SetupHttpClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(45);

        var version = Constants.Version;

        if (version.Contains("error"))
        {
            GD.PrintErr("Can't access our version, sending web requests with unknown version");
            version = "unknown";
        }

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Thrive", version));

        return client;
    }

    // TODO: test displaying this
    private static FeedItem CreateErrorItem(string error)
    {
        GD.PrintErr($"Fetching Thrive news feed failed due to: {error}");

        return new FeedItem(TranslationServer.Translate("ERROR_FETCHING_NEWS"), null,
            TranslationServer.Translate("ERROR_FETCHING_EXPLANATION").FormatSafe(error), string.Empty);
    }

    private static IEnumerable<ExtractedFeedItem> ExtractFeedItems(XDocument document)
    {
        var potentialItems = document.Descendants().Where(e => e.Name.LocalName == "item");

        int seenItems = 0;

        foreach (var element in potentialItems)
        {
            // This code is duplicated from FeedParser
            // TODO: combine this to use the same code once possible (for now this is a slightly cut down version
            // as not all data is needed by Thrive to reduce the amount of duplication)
            var id = element.Descendants().FirstOrDefault(p => p.Name.LocalName is "id" or "guid")
                ?.Value;

            // Can't handle entries with no id
            if (id == null)
                continue;

            var link = "Link is missing";

            var linkElement = element.Descendants().FirstOrDefault(p => p.Name.LocalName == "link");

            if (linkElement != null)
            {
                link = linkElement.Attribute("href")?.Value ?? linkElement.Value;
            }

            // TODO: link should be sanitized against bbcode

            var title = element.Descendants().FirstOrDefault(p => p.Name.LocalName == "title")?.Value ??
                "Unknown title";

            var parsed = new ExtractedFeedItem(id, link, title)
            {
                Summary = element.Descendants().FirstOrDefault(p => p.Name.LocalName == "summary")?.Value ??
                    element.Descendants().FirstOrDefault(p => p.Name.LocalName == "description")?.Value ??
                    element.Descendants().FirstOrDefault(p => p.Name.LocalName == "content")?.Value,
            };

            var published = element.Descendants().FirstOrDefault(p => p.Name.LocalName is "published" or "pubDate")
                ?.Value;

            if (published != null && DateTime.TryParse(published, out var parsedTime))
            {
                parsed.PublishedAt = parsedTime.ToUniversalTime();
            }

            yield return parsed;

            if (++seenItems > Constants.MAX_NEWS_FEED_ITEMS_TO_SHOW)
                break;
        }
    }

    private static List<FeedItem> ParseHtmlItemsToBbCode(IEnumerable<ExtractedFeedItem> items)
    {
        var htmlParser = new HtmlParser(new HtmlParserOptions
        {
            IsStrictMode = false,
        });

        var dummyDom = htmlParser.ParseDocument(string.Empty).DocumentElement;

        var stringBuilder = new StringBuilder();

        var results = new List<FeedItem>();

        foreach (var item in items)
        {
            stringBuilder.Clear();

            string footer = string.Empty;

            if (item.PublishedAt != null)
            {
                footer = TranslationServer.Translate("FEED_ITEM_PUBLISHED_AT")
                    .FormatSafe(item.PublishedAt.Value.ToLocalTime().ToLongTimeString());
            }

            // Special handling for items that don't have a summary for some reason
            if (string.IsNullOrWhiteSpace(item.Summary))
            {
                results.Add(new FeedItem(item.Title, item.Link,
                    TranslationServer.Translate("FEED_ITEM_MISSING_CONTENT"), footer));

                continue;
            }

            bool truncated = false;

            try
            {
                var document = htmlParser.ParseFragment(item.Summary!, dummyDom);

                foreach (var topLevelNode in document)
                {
                    HandleNode(topLevelNode, stringBuilder, ref truncated);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to parse content of feed item ({item.ID}): {e}");

                results.Add(new FeedItem(item.Title, item.Link,
                    TranslationServer.Translate("FEED_ITEM_CONTENT_PARSING_FAILED"), footer));
                continue;
            }

            if (truncated)
                footer = TranslationServer.Translate("FEED_ITEM_TRUNCATED_NOTICE").FormatSafe(footer);

            results.Add(new FeedItem(item.Title, item.Link, stringBuilder.ToString(), footer));
        }

        return results;
    }

    private static void HandleNode(INode node, StringBuilder stringBuilder, ref bool truncated)
    {
        // TODO: escaping Bbcode tags that are present in the text already
        switch (node)
        {
            case IText text:
            {
                if (!truncated)
                {
                    var textToAdd = text.Data;

                    if (stringBuilder.Length < 1)
                    {
                        textToAdd = textToAdd.TrimStart();
                    }

                    if (stringBuilder.Length + textToAdd.Length >= Constants.MAX_NEWS_FEED_ITEM_LENGTH)
                    {
                        textToAdd = textToAdd.Substring(0, Constants.MAX_NEWS_FEED_ITEM_LENGTH - stringBuilder.Length);
                        truncated = true;
                    }

                    // Avoid a bunch of useless whitespace
                    if (string.IsNullOrWhiteSpace(textToAdd))
                    {
                        if (stringBuilder.Length > 0)
                        {
                            var newLines = textToAdd.Count(c => c == '\n');
                            if (newLines > 0)
                            {
                                // TODO:
                                // if (CountEndingCharactersMatching('\n') < 3)
                                {
                                    stringBuilder.Append('\n');
                                }
                            }
                            else
                            {
                                // AddLastTextIfDoesNotEndWithAlready(" ");
                            }
                        }
                    }
                    else
                    {
                        stringBuilder.Append(textToAdd);
                    }
                }

                break;
            }

            case IHtmlAnchorElement anchorElement:
            {
                if (!truncated)
                {
                    stringBuilder.Append($"[url={anchorElement.Href}]{anchorElement.Text}[/url]");
                }

                // Anchor element children are not handled
                return;
            }
        }

        // int length = Constants.MAX_NEWS_FEED_ITEM_LENGTH;

        if (node.HasChildNodes)
        {
            foreach (var child in node.ChildNodes)
            {
                HandleNode(child, stringBuilder, ref truncated);
            }
        }

        switch (node)
        {
            case IHtmlParagraphElement or IHtmlHeadingElement or IHtmlBreakRowElement:
            {
                // TODO: add the AddLastTextIfDoesNotEndWIthAlready
                if (!truncated)
                    stringBuilder.Append("\n\n");
                break;
            }
        }

        /*
                    case IHtmlUnorderedListElement or IHtmlOrderedListElement:
                        // These are handled in the below case
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

                    case IHtmlParagraphElement or IHtmlHeadingElement or IHtmlBreakRowElement:
                    {
                        if (PendingText)
                        {
                            AddLastTextIfDoesNotEndWithAlready("\n\n");
                        }

                        break;
                    }

                    case IHtmlQuoteElement:
                    {
                        AddLastTextIfDoesNotEndWithAlready("> ");

                        break;
                    }

                    case IHtmlSpanElement or IHtmlDivElement:
                    {
                        if (PendingText)
                            AddLastTextIfDoesNotEndWithAlready(" ");

                        break;
                    }

                    case IHtmlInlineFrameElement iFrame:
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

                    case IHtmlAnchorElement aElement:
                    {
                        HandleAElement(aElement, ref seenLength, length);
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
                    }

                    default:
                    {
                        var name = node.NodeName.ToLowerInvariant();

                        if (name == TagNames.Dd || name ==
                            TagNames.Dt || name ==
                            TagNames.B || name ==
                            TagNames.Big || name ==
                            TagNames.Strike || name ==
                            TagNames.Code || name ==
                            TagNames.Em || name ==
                            TagNames.I || name ==
                            TagNames.S || name ==
                            TagNames.Small || name ==
                            TagNames.Strong || name ==
                            TagNames.U || name ==
                            TagNames.Tt || name ==
                            TagNames.Pre || name ==
                            TagNames.NoBr)
                        {
                            // TODO: implement text styling
                            continue;
                        }

                        if (name is "aside" or "article")
                        {
                            if (!string.IsNullOrEmpty(AsideStart))
                            {
                                if (PendingText)
                                    AddLastTextIfDoesNotEndWithAlready("\n\n");

                                stringBuilder.Append(AsideStart);
                            }

                            continue;
                        }

                        if (name == TagNames.Header)
                        {
                            // TODO: better handling for this
                            AddLastTextIfDoesNotEndWithAlready("# ");
                            continue;
                        }

                        // Unknown node, so we are losing info here
                        truncated = true;
                        FlushTextIfPending();

                        logger.LogDebug("Not parsing unknown HTML in feed: {Node}", node);
                        break;
                    }
         *
         */
    }

    public class FeedItem
    {
        public FeedItem(string title, string? readLink, string contentBbCode, string footerLine)
        {
            Title = title;
            ReadLink = readLink;
            ContentBbCode = contentBbCode;
            FooterLine = footerLine;
        }

        public string Title { get; }
        public string? ReadLink { get; }
        public string ContentBbCode { get; }
        public string FooterLine { get; }
    }

    private class ExtractedFeedItem
    {
        public ExtractedFeedItem(string id, string link, string title)
        {
            ID = id;
            Link = link;
            Title = title;
        }

        public string ID { get; }
        public string Link { get; }
        public string Title { get; }
        public string? Summary { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
