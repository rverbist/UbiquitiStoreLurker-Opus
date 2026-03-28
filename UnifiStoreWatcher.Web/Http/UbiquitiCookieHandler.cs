namespace UnifiStoreWatcher.Web.Http;

/// <summary>
/// DelegatingHandler that injects and captures cookies for the Ubiquiti EU store.
/// Registered as transient; shares state via the singleton <see cref="UbiquitiCookieJar"/>.
/// </summary>
public sealed class UbiquitiCookieHandler(UbiquitiCookieJar jar) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Inject cookies from shared jar into the outgoing request.
        if (request.RequestUri is { } uri)
        {
            var cookieHeader = jar.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Capture any Set-Cookie headers back into the shared jar.
        if (request.RequestUri is { } responseUri)
            jar.UpdateFromResponse(responseUri, response);

        return response;
    }
}
