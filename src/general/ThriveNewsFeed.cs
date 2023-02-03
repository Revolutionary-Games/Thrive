using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;
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
            GD.PrintErr($"Error in feed fetching or processing: {e}");
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

    private static FeedItem CreateErrorItem(string error)
    {
        GD.PrintErr($"Fetching Thrive news feed failed due to: {error}");

        return new FeedItem(TranslationServer.Translate("ERROR_FETCHING_NEWS"), null,
            TranslationServer.Translate("ERROR_FETCHING_EXPLANATION").FormatSafe(error), null);
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

            link = HtmlToBbCodeConverter.SanitizeUrlForBbCode(link);

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
        var results = new List<FeedItem>();

        var converter = new HtmlToBbCodeConverter();

        foreach (var item in items)
        {
            // Special handling for items that don't have a summary for some reason
            if (string.IsNullOrWhiteSpace(item.Summary))
            {
                results.Add(new FeedItem(item.Title, item.Link,
                    TranslationServer.Translate("FEED_ITEM_MISSING_CONTENT"), item.PublishedAt));

                continue;
            }

            bool truncated;
            string bbCode;

            try
            {
                bbCode = converter.Convert(item.Summary!, out truncated).TrimEnd();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to parse content of feed item ({item.ID}): {e}");

                results.Add(new FeedItem(item.Title, item.Link,
                    TranslationServer.Translate("FEED_ITEM_CONTENT_PARSING_FAILED"), item.PublishedAt));
                continue;
            }

            // In case all the HTML didn't result in any displayable text
            if (string.IsNullOrWhiteSpace(bbCode))
            {
                results.Add(new FeedItem(item.Title, item.Link,
                    TranslationServer.Translate("FEED_ITEM_MISSING_CONTENT"), item.PublishedAt));

                continue;
            }

            results.Add(new FeedItem(item.Title, item.Link, bbCode, item.PublishedAt, truncated));
        }

        return results;
    }

    public class FeedItem
    {
        public FeedItem(string title, string? readLink, string contentBbCode, DateTime? publishedAt,
            bool truncated = false)
        {
            Title = title;
            ReadLink = readLink;
            ContentBbCode = contentBbCode;
            PublishedAt = publishedAt;
            Truncated = truncated;
        }

        public string Title { get; }
        public string? ReadLink { get; }
        public string ContentBbCode { get; }

        public bool Truncated { get; }
        private DateTime? PublishedAt { get; }

        public string GetFooterText()
        {
            string footer = string.Empty;

            if (PublishedAt != null)
            {
                footer = TranslationServer.Translate("FEED_ITEM_PUBLISHED_AT")
                    .FormatSafe(PublishedAt.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
            }

            if (Truncated)
            {
                return TranslationServer.Translate("FEED_ITEM_TRUNCATED_NOTICE").FormatSafe(footer);
            }

            return footer;
        }
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
