using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;

public class HtmlReader
{
    public static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(50);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ThriveScripts",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()));

        return client;
    }

    public static async Task<IHtmlDocument> RetrieveHtmlDocument(HttpClient client, string url,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // This retries once on failure in case we hit a temporary failure or rate limit
            response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        // Need to set up some config to ensure right relative URLs are generated
        var config = Configuration.Default.WithDefaultLoader();
        var browsingContext = BrowsingContext.New(config);

        var document = await browsingContext.OpenAsync(action =>
        {
            action.Address(url);
            action.Content(response.Content.ReadAsStream());
        }, cancellationToken);

        if (document.Body == null || document is not IHtmlDocument htmlDocument)
            throw new Exception("Parsed document has no body (or not HTML document)");

        return htmlDocument;
    }
}
