namespace WebRequesterDll.Tests;

/// <summary>
///     A minimal HttpMessageHandler test double. Pass a delegate describing how to respond to (or fail) a
///     request, and hand an instance to Requester.GetFromWeb's `handler` parameter to exercise its logic
///     without making a real network call.
/// </summary>
internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return respond(request, cancellationToken);
    }
}
