using System.Net.Http.Headers;
using WebRequesterDll.Models;

namespace WebRequesterDll;

/// <summary>
/// Provides static methods for performing HTTP web requests and retrieving information about responses, including
/// handling of redirects.
/// </summary>
/// <remarks>The Requester class is intended for scenarios where you need to fetch web pages or analyze HTTP
/// redirect chains. All methods are thread-safe and can be used concurrently. Returned results include both the
/// original and final URLs, as well as the full HTTP response and any intermediate redirects encountered.</remarks>
public static class Requester
{
    /// <summary>
    /// Get page from web
    /// </summary>
    /// <param name="startUrl"></param>
    /// <returns></returns>
    public static async Task<WebResponseResult> GetFromWeb(string startUrl)
    {
        using var client = ClientInit(true);
        var response = await client.GetAsync(startUrl);

        var resonseHeadersRaw = response.Headers;
        var contentHeadersRaw = response.Content.Headers;
        var contentHeaders = contentHeadersRaw.ToDictionary(h => h.Key, h => string.Join("|", h.Value)); // join multiple values
        var resonseHeaders = resonseHeadersRaw.ToDictionary(h => h.Key, h => string.Join("|", h.Value)); // join multiple values

        return new WebResponseResult
        {
            Content = await response.Content.ReadAsStringAsync(),
            Properties = new WebReponseProps
            {
                StartUrl = startUrl,
                FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "null",
                RedirectChain = [],
                ContentLength = response.Content.Headers.ContentLength ?? -1,
                StatusCode = (int)response.StatusCode,
                // CharsetParsed = new CharsetParser(response),
                CharSet = response.Content.Headers.ContentType?.CharSet ?? "",
                MediaType = response.Content.Headers.ContentType?.MediaType ?? "",
                ResponseHeaders = resonseHeaders,
                ContentHeaders = contentHeaders
            }
        };
    }

    /// <summary>
    /// Init client object.
    /// </summary>
    /// <param name="allowAutoRedirect"></param>
    /// <returns></returns>
    private static HttpClient ClientInit(bool allowAutoRedirect)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = allowAutoRedirect
        };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/74.0.3729.1235");
        client.Timeout = TimeSpan.FromSeconds(30); // Set timeout to 30 seconds
        return client;
    }

    /// <summary>
    /// Get redirects, useful if a link with redirects is considered invalid
    /// </summary>
    /// <param name="startUrl"></param>
    /// <returns></returns>
    public static async Task<WebResponseResult> GetFromWebWithRedirects(string startUrl)
    {
        using var client = ClientInit(false);
        var currentUrl = startUrl;
        var rv = new WebResponseResult
        {
            Properties = new WebReponseProps
            {
                StartUrl = startUrl
            }
        };
        while (true)
        {
            var response = await client.GetAsync(currentUrl);

            Console.WriteLine($"Visited: {currentUrl} (Status: {(int)response.StatusCode})");

            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                var locationHeader = response.Headers.Location;
                if (locationHeader == null)
                {
                    continue;
                }
                var nextUrl = !locationHeader.IsAbsoluteUri ? new Uri(new Uri(currentUrl), locationHeader) : locationHeader;
                Console.WriteLine($"Redirected to: {nextUrl}");
                rv.Properties.RedirectChain.Add(nextUrl.ToString());
                currentUrl = nextUrl.ToString();
            }
            else
            {
                rv.Properties.FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "null";
                rv.Content = await response.Content.ReadAsStringAsync();
                return rv;
            }
        }

        // jeff2do
        throw new NotImplementedException("need to add more props... see other method above");
    }
}