using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.API.Tests.Fakes;

/// <summary>
/// A test double for <see cref="HttpMessageHandler"/> that returns a configurable response.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _jsonBody;
    private readonly Action<HttpResponseMessage>? _configureResponse;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string jsonBody, Action<HttpResponseMessage>? configureResponse = null)
    {
        _statusCode = statusCode;
        _jsonBody = jsonBody;
        _configureResponse = configureResponse;
    }

    /// <summary>The last request received by this handler.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>The number of requests this handler has received.</summary>
    public int RequestCount { get; private set; }

    /// <summary>
    /// The last request's content, read eagerly since the caller may dispose its
    /// <see cref="HttpContent"/> before the test gets a chance to inspect it.
    /// </summary>
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_jsonBody, Encoding.UTF8, "application/json")
        };
        _configureResponse?.Invoke(response);
        return response;
    }
}
