using System.Diagnostics;
using System.Net.Cache;
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
    /// <summary>
    ///     Get page from web
    /// </summary>
    /// <param name="startUrl">Start url</param>
    /// <param name="cacheFolder"></param>
    /// <param name="cacheMode"></param>
    /// <returns></returns>
    public static async Task<WebResponseResult> GetFromWeb(string startUrl, string cacheFolder, MyEnum.CacheMode cacheMode)
    {
        var result = await GetFromWebEach(startUrl);
        if (!startUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Can only parse links that start with HTTPS://"); 
        }
        var cache = new CacheService(startUrl, cacheFolder, cacheMode);
        if (cache.Exists() && cache.CacheMode == MyEnum.CacheMode.UseCacheIfExists)
        {
            return cache.Read();
        }

        result.Info.Cache = cache.CacheInfo;
        cache.Save(result);
        return result;
    }


    private static async Task<WebResponseResult> GetFromWebEach(string startUrl)
    {
        using var client = ClientInit(true);
        var response = await Request(client, startUrl);
        if (response.ResponseMessage == null)
        {
            return new WebResponseResult
            {
                IsCached = false,
                Content = "",
                Info = new WebResponseInfo
                {
                    Url = new UrlModel
                    {
                        Start = startUrl,
                        Final = "",
                        RedirectChain = []
                    },
                    Status = response.ResponseStatus,
                    // CharsetParsed = new CharsetParser(response),
                    CharSet = "",
                    MediaType = "",
                    ResponseHeaders = new Dictionary<string, string>(),
                    ContentHeaders = new Dictionary<string, string>()
                }
            };
        }

        var resonseHeadersRaw = response.ResponseMessage.Headers;
        var contentHeadersRaw = response.ResponseMessage.Content.Headers;
        var contentHeaders = contentHeadersRaw.ToDictionary(h => h.Key, h => string.Join("|", h.Value)); // join multiple values
        var resonseHeaders = resonseHeadersRaw.ToDictionary(h => h.Key, h => string.Join("|", h.Value)); // join multiple values

        var contentLength = response.ResponseMessage.Content.Headers.ContentLength;
        Debug.WriteLine($"contentLength = {contentLength}");

        return new WebResponseResult
        {
            IsCached = false,
            Info = new WebResponseInfo
            {
                Url = new UrlModel
                {
                    Start = startUrl,
                    Final = response.ResponseMessage.RequestMessage!.RequestUri!.ToString(),
                    RedirectChain = []
                },
                // CharsetParsed = new CharsetParser(response),
                Status = response.ResponseStatus,
                CharSet = response.ResponseMessage.Content.Headers.ContentType?.CharSet ?? "",
                MediaType = response.ResponseMessage.Content.Headers.ContentType?.MediaType ?? "",
                ResponseHeaders = resonseHeaders,
                ContentHeaders = contentHeaders
            },
            Content = await response.ResponseMessage.Content.ReadAsStringAsync()
        };
    }


    private static async Task<HttpResponseMsg> Request(HttpClient client, string url)
    {
        try
        {
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return new HttpResponseMsg
            {
                ResponseMessage = response,
                ResponseStatus = new HttpReponseStatus
                {
                    StatusCode = (int)response.StatusCode,
                    ErrorCode = ""
                }
            };
        }
        catch (HttpRequestException ex) when
            (ex.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound })
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.DnsFailure),
                    ErrorMessage = ex.Message
                }
            };
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.Timeout),
                    ErrorMessage = ex.Message
                }
            };
        }
        catch (HttpRequestException ex) when (ex.InnerException is IOException)
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.ConnectionError),
                    ErrorMessage = ex.Message
                }
            };
        }
        catch (HttpRequestException ex) when
            (ex.InnerException is AuthenticationException ||
             ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.SslError),
                    ErrorMessage = ex.Message
                }
            };
        }
        // ⭐ Detect 404, 500, 301, 403, etc.
        catch (HttpRequestException ex) when (ex.StatusCode is { } code)
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.HttpError),
                    StatusCode = (int)code,
                    ErrorMessage = ex.Message
                }
            };
        }

        // Fallback for HttpRequestException with NO status code (network errors)
        catch (HttpRequestException ex)
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.HttpError),
                    ErrorMessage = ex.Message
                }
            };
        }
        catch (Exception ex)
        {
            return new HttpResponseMsg
            {
                ResponseMessage = null,
                ResponseStatus = new HttpReponseStatus
                {
                    ErrorCode = nameof(MyEnum.RequestErrorCodeEnum.Unexpected),
                    ErrorMessage = ex.Message
                }
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