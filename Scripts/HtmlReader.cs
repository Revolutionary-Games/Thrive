using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

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
            response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // This retries once on failure in case we hit a temporary failure or rate limit
            response = await client.GetAsync(url, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var parser = new HtmlParser();

        var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync(cancellationToken));

        if (document.Body == null)
            throw new Exception("Parsed document has no body");

        return document;
    }
}
