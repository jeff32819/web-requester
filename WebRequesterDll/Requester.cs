using System.Net.Sockets;
using System.Security.Authentication;

using WebRequesterDll.Models;

using static WebRequesterDll.MyEnum;

namespace WebRequesterDll;

/// <summary>
///     Provides static methods for performing HTTP web requests and retrieving information about responses, including
///     handling of redirects.
/// </summary>
/// <remarks>
///     The Requester class is intended for scenarios where you need to fetch web pages or analyze HTTP
///     redirect chains. Returned results include both the original and final URLs, as well as the full HTTP response
///     and any intermediate redirects encountered.
/// </remarks>
public static class Requester
{
    /// <summary>
    ///     Default timeout for HTTP requests in seconds, used when GetFromWeb is called without an explicit
    ///     <c>timeoutSeconds</c> argument. Changing this affects every subsequent call that relies on the default,
    ///     so concurrent callers that need different timeouts should pass <c>timeoutSeconds</c> explicitly instead
    ///     of mutating this shared property.
    /// </summary>
    public static int Timeout { get; set; } = 30;

    /// <summary>
    ///     Default User-Agent header, used when GetFromWeb is called without an explicit <c>userAgent</c> argument.
    ///     Concurrent callers that need different user agents should pass <c>userAgent</c> explicitly instead of
    ///     mutating this shared property.
    /// </summary>
    public static string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/74.0.3729.1235";

    /// <summary>
    ///     Shared client for the lifetime of the process. Reusing one HttpClient (with auto-redirect enabled)
    ///     avoids the socket-exhaustion issues caused by creating/disposing an HttpClient per request.
    /// </summary>
    private static readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = true });

    /// <summary>
    ///     Get page from web
    /// </summary>
    /// <param name="requestedUri"></param>
    /// <param name="timeoutSeconds">Per-call timeout override; defaults to <see cref="Timeout"/> when omitted.</param>
    /// <param name="userAgent">Per-call User-Agent override; defaults to <see cref="UserAgent"/> when omitted.</param>
    /// <param name="cancellationToken">
    ///     Lets the caller cancel the request explicitly, independent of the timeout. If this token is cancelled,
    ///     the resulting <see cref="OperationCanceledException"/> propagates rather than being reported as a
    ///     <see cref="MyEnum.RequestErrorCodeEnum.Timeout"/> result.
    /// </param>
    /// <param name="handler">
    ///     Test seam: an <see cref="HttpMessageHandler"/> to issue the request through instead of the shared
    ///     production <see cref="HttpClient"/>. Leave null in normal use.
    /// </param>
    /// <returns></returns>
    public static async Task<WebResponseResult> GetFromWeb(
        Uri requestedUri,
        int? timeoutSeconds = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default,
        HttpMessageHandler? handler = null)
    {
        var effectiveTimeout = timeoutSeconds ?? Timeout;
        var effectiveUserAgent = userAgent ?? UserAgent;
        var client = handler == null ? _client : new HttpClient(handler);

        WebRequesterEvents.RaiseProcessCompleted("WebRequesterDll.Requester.GetFromWeb()");
        WebRequesterEvents.RaiseProcessCompleted($"UserAgent = {effectiveUserAgent}");
        WebRequesterEvents.RaiseProcessCompleted($"Timeout = {effectiveTimeout} seconds");
        WebRequesterEvents.RaiseProcessCompleted("requestorConfig");

        var result = await GetFromWebEach(client, requestedUri, effectiveTimeout, effectiveUserAgent, cancellationToken);
        return result;
    }


    private static async Task<WebResponseResult> GetFromWebEach(
        HttpClient client, Uri requestedUri, int timeoutSeconds, string userAgent, CancellationToken cancellationToken)
    {
        var response = await Request(client, requestedUri.AbsoluteUri, timeoutSeconds, userAgent, cancellationToken);
        if (response.ResponseMessage == null)
        {
            WebRequesterEvents.RaiseProcessCompleted($"ErrorMessage = {response.ResponseStatus.ErrorMessage}");
            WebRequesterEvents.RaiseProcessCompleted($"ErrorCode = {response.ResponseStatus.ErrorCode}");
            return new WebResponseResult
            {
                IsCached = false,
                Content = "",
                Info = new WebResponseInfo
                {
                    Url = new UrlModel
                    {
                        Start = requestedUri.AbsoluteUri,
                        Final = "",
                        RedirectChain = []
                    },
                    Status = response.ResponseStatus,
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
        WebRequesterEvents.RaiseProcessCompleted($"ContentLength = {contentLength}");

        var tmp = new WebResponseResult
        {
            IsCached = false,
            Info = new WebResponseInfo
            {
                Url = new UrlModel
                {
                    Start = requestedUri.AbsoluteUri,
                    Final = response.ResponseMessage.RequestMessage!.RequestUri!.ToString(),
                    RedirectChain = []
                },
                Status = response.ResponseStatus,
                CharSet = response.ResponseMessage.Content.Headers.ContentType?.CharSet ?? "",
                MediaType = response.ResponseMessage.Content.Headers.ContentType?.MediaType ?? "",
                ResponseHeaders = resonseHeaders,
                ContentHeaders = contentHeaders
            },
            Content = await response.ResponseMessage.Content.ReadAsStringAsync()
        };
        WebRequesterEvents.RaiseProcessCompleted($"StartUrl = {requestedUri.AbsoluteUri}");
        WebRequesterEvents.RaiseProcessCompleted($"FinalUrl = {tmp.Info.Url.Final}");
        WebRequesterEvents.RaiseProcessCompleted($"Status = {tmp.Info.Status}");
        WebRequesterEvents.RaiseProcessCompleted($"CharSet = {tmp.Info.CharSet}");
        return tmp;
    }


    private static async Task<HttpResponseMsg> Request(
        HttpClient client, string url, int timeoutSeconds, string userAgent, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(userAgent);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            response.EnsureSuccessStatusCode();
            return new HttpResponseMsg
            {
                ResponseMessage = response,
                ResponseStatus = new HttpResponseStatus
                {
                    StatusCode = (int)response.StatusCode,
                    ErrorCode = RequestErrorCodeEnum.None
                }
            };
        }
        catch (HttpRequestException ex) when
            (ex.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound })
        {
            return ErrorResult(RequestErrorCodeEnum.DnsFailure, ex.Message);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return ErrorResult(RequestErrorCodeEnum.Timeout, $"Request timed out after {timeoutSeconds} second(s).");
        }
        catch (OperationCanceledException)
        {
            // Not our timeout firing - the caller's own cancellationToken was cancelled. Propagate it as-is
            // rather than reporting it as a Timeout/Unexpected result, per normal CancellationToken semantics.
            throw;
        }
        catch (HttpRequestException ex) when (ex.InnerException is IOException)
        {
            return ErrorResult(RequestErrorCodeEnum.ConnectionError, ex.Message);
        }
        catch (HttpRequestException ex) when
            (ex.InnerException is AuthenticationException ||
             ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResult(RequestErrorCodeEnum.SslError, ex.Message);
        }
        // ⭐ Detect 404, 500, 301, 403, etc.
        catch (HttpRequestException ex) when (ex.StatusCode is { } code)
        {
            return ErrorResult(RequestErrorCodeEnum.HttpError, ex.Message, (int)code);
        }
        // Fallback for HttpRequestException with NO status code (network errors)
        catch (HttpRequestException ex)
        {
            return ErrorResult(RequestErrorCodeEnum.HttpError, ex.Message);
        }
        catch (Exception ex)
        {
            return ErrorResult(RequestErrorCodeEnum.Unexpected, ex.Message);
        }
    }

    private static HttpResponseMsg ErrorResult(RequestErrorCodeEnum errorCode, string errorMessage, int statusCode = 0)
    {
        return new HttpResponseMsg
        {
            ResponseMessage = null,
            ResponseStatus = new HttpResponseStatus
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            }
        };
    }
}