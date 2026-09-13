using System.Net;
using System.Net.Sockets;
using WebRequesterDll.Models;
using static WebRequesterDll.MyEnum;

namespace WebRequesterDll.Tests;

public class RequesterTests
{
    private static readonly Uri TestUri = new("https://example.invalid/");

    [Fact]
    public async Task GetFromWeb_ReturnsContentAndNoError_WhenServerRespondsOk()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent("<html>hi</html>", System.Text.Encoding.UTF8, "text/html")
            };
            return Task.FromResult(response);
        });

        var result = await Requester.GetFromWeb(TestUri, handler: handler);

        Assert.Equal(RequestErrorCodeEnum.None, result.Info.Status.ErrorCode);
        Assert.Equal(200, result.Info.Status.StatusCode);
        Assert.Equal("<html>hi</html>", result.Content);
        Assert.Equal("text/html", result.Info.MediaType);
    }

    [Fact]
    public async Task GetFromWeb_ReturnsDnsFailure_WhenHostCannotBeResolved()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new HttpRequestException("simulated failure", new SocketException((int)SocketError.HostNotFound)));

        var result = await Requester.GetFromWeb(TestUri, handler: handler);

        Assert.Equal(RequestErrorCodeEnum.DnsFailure, result.Info.Status.ErrorCode);
        Assert.Equal("", result.Content);
    }

    [Fact]
    public async Task GetFromWeb_ReturnsHttpError_WhenServerRespondsNotFound()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
            return Task.FromResult(response);
        });

        var result = await Requester.GetFromWeb(TestUri, handler: handler);

        Assert.Equal(RequestErrorCodeEnum.HttpError, result.Info.Status.ErrorCode);
        Assert.Equal(404, result.Info.Status.StatusCode);
    }

    [Fact]
    public async Task GetFromWeb_ReturnsTimeout_WhenServerNeverResponds()
    {
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await Requester.GetFromWeb(TestUri, timeoutSeconds: 1, handler: handler);

        Assert.Equal(RequestErrorCodeEnum.Timeout, result.Info.Status.ErrorCode);
    }

    [Fact]
    public async Task GetFromWeb_PropagatesCancellation_WhenCallerCancelsExplicitly()
    {
        using var cts = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Requester.GetFromWeb(TestUri, timeoutSeconds: 30, cancellationToken: cts.Token, handler: handler));
    }
}
