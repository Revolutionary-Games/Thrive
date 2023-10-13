using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

public class HtmlReader
{
    public static async Task<IHtmlDocument> RetrieveHtmlDocument(string url, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var parser = new HtmlParser();

        var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync(cancellationToken));

        if (document.Body == null)
            throw new Exception("Parsed document has no body");

        return document;
    }
}
