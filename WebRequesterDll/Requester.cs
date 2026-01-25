using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

using WebRequesterDll.Models;

namespace WebRequesterDll;

/// <summary>
///     Provides static methods for performing HTTP web requests and retrieving information about responses, including
///     handling of redirects.
/// </summary>
/// <remarks>
///     The Requester class is intended for scenarios where you need to fetch web pages or analyze HTTP
///     redirect chains. All methods are thread-safe and can be used concurrently. Returned results include both the
///     original and final URLs, as well as the full HTTP response and any intermediate redirects encountered.
/// </remarks>
public static class Requester
{

    private static CacheService? GetCacheIfExists(string startUrl, string cacheFolder, MyEnum.CacheMode cacheMode)
    {
        if (string.IsNullOrEmpty(cacheFolder))
        {
            return null;
        }
        try
        {
            Directory.CreateDirectory(cacheFolder);
            return new CacheService(startUrl, cacheFolder);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot create folder {cacheFolder}", ex);
        }
    }
    
    /// <summary>
    ///     Get page from web
    /// </summary>
    /// <param name="startUrl">Start url</param>
    /// <param name="cacheFolder">Cache folder</param>
    /// <param name="cacheMode"></param>
    /// <returns></returns>
    public static async Task<WebResponseResult> GetFromWeb(string startUrl, string cacheFolder, MyEnum.CacheMode cacheMode)
    {
        if (!startUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Can only parse links that start with HTTPS://");
        }
        var cacheFileConfig = GetCacheIfExists(startUrl, cacheFolder, cacheMode);
        if (cacheFileConfig != null && cacheFileConfig.Exists() && cacheMode == MyEnum.CacheMode.UseCacheIfExists)
        {
            return cacheFileConfig.Read();
        }
        using var client = ClientInit(true);
        var response = await Request(client, startUrl);
        if (response.Response == null)
        {
            throw new Exception(response.ErrorMessage);
        }

        var resonseHeadersRaw = response.Response.Headers;
        var contentHeadersRaw = response.Response.Content.Headers;
        var contentHeaders = contentHeadersRaw.ToDictionary(h => h.Key, h => string.Join("|", h.Value)); // join multiple values
        var resonseHeaders = resonseHeadersRaw.ToDictionary(h => h.Key, h => string.Join("|", h.Value)); // join multiple values

        var webResponseResult = new WebResponseResult
        {
            IsCached = false,
            Content = await response.Response.Content.ReadAsStringAsync(),
            Properties = new WebReponseProps
            {
                StartUrl = startUrl,
                FinalUrl = response.Response.RequestMessage?.RequestUri?.ToString() ?? "null",
                RedirectChain = [],
                ContentLength = response.Response.Content.Headers.ContentLength ?? -1,
                StatusCode = (int)response.Response.StatusCode,
                // CharsetParsed = new CharsetParser(response),
                CharSet = response.Response.Content.Headers.ContentType?.CharSet ?? "",
                MediaType = response.Response.Content.Headers.ContentType?.MediaType ?? "",
                ResponseHeaders = resonseHeaders,
                ContentHeaders = contentHeaders,
                CachInfo = cacheFileConfig?.CachInfo
            }
        };
        cacheFileConfig?.Save(webResponseResult);
        return webResponseResult;
    }

    public static async Task<HttpReponseResult> Request(HttpClient client, string url)
    {
        try
        {
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            return new HttpReponseResult
            {
                Response = response,
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.None
            };
        }
        catch (HttpRequestException ex) when
            (ex.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound })
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.DnsFailure,
                ErrorMessage = $"DNS failure | {url} | {ex.Message}"
            };
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.Timeout,
                ErrorMessage = $"Timeout | {url} | {ex.Message}"
            };
        }
        catch (HttpRequestException ex) when (ex.InnerException is IOException)
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.ConnectionError,
                ErrorMessage = $"Connection reset, broken pipe, network drop | {url} | {ex.Message}"
            };
        }
        catch (HttpRequestException ex) when
            (ex.InnerException is AuthenticationException ||
             ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.SslError,
                ErrorMessage = $"SSL/TLS error | {url} | {ex.Message}"
            };
        }
        // ⭐ Detect 404, 500, 301, 403, etc.
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode code)
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.HttpError,
                HttpStatusCode = code,
                ErrorMessage = $"{(int)code} {code} | {url}"
            };
        }

        // Fallback for HttpRequestException with NO status code (network errors)
        catch (HttpRequestException ex)
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.HttpError,
                ErrorMessage = $"HTTP/network error | {url} | {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new HttpReponseResult
            {
                ErrorCode = HttpReponseResult.HttpErrorCodeEnum.Unexpected,
                ErrorMessage = $"Unexpected error | {url} | {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     Init client object.
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
    ///     Get redirects, useful if a link with redirects is considered invalid
    /// </summary>
    /// <param name="startUrl"></param>
    /// <returns></returns>
    public static async Task<WebResponseResult> GetFromWebWithRedirects(string startUrl)
    {
        // jeff2do
        throw new NotImplementedException("need to add more props... see other method above");


        //using var client = ClientInit(false);
        //var currentUrl = startUrl;
        //var rv = new WebResponseResult
        //{
        //    Properties = new WebReponseProps
        //    {
        //        StartUrl = startUrl
        //    }
        //};
        //while (true)
        //{
        //    var response = await client.GetAsync(currentUrl);

        //    Console.WriteLine($"Visited: {currentUrl} (Status: {(int)response.StatusCode})");

        //    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
        //    {
        //        var locationHeader = response.Headers.Location;
        //        if (locationHeader == null)
        //        {
        //            continue;
        //        }
        //        var nextUrl = !locationHeader.IsAbsoluteUri ? new Uri(new Uri(currentUrl), locationHeader) : locationHeader;
        //        Console.WriteLine($"Redirected to: {nextUrl}");
        //        rv.Properties.RedirectChain.Add(nextUrl.ToString());
        //        currentUrl = nextUrl.ToString();
        //    }
        //    else
        //    {
        //        rv.Properties.FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "null";
        //        rv.Content = await response.Content.ReadAsStringAsync();
        //        return rv;
        //    }
        //}
    }
}