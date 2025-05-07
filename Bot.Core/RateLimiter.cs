namespace Bot.Core;

using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Telegram.Bot.Exceptions;


internal sealed class ClientSideRateLimitedHandler(
    RateLimiter limiter)
    : DelegatingHandler(new HttpClientHandler()), IAsyncDisposable
{
    private int TryCount = 0;
    private const int MaxRetries = 10; 
    private const int DelayBetweenRetriesMs = 60; 

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using RateLimitLease lease = await limiter.AcquireAsync(
            permitCount: 1, cancellationToken);
        HttpResponseMessage responseMessage =  new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        
        if (lease.IsAcquired)
        {
            responseMessage = await base.SendAsync(request, cancellationToken);
                
            // Check if the response indicates to retry
            if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests && TryCount < MaxRetries)
            {
                TryCount++;
                await Task.Delay(DelayBetweenRetriesMs, cancellationToken);
                return await SendAsync(request, cancellationToken);
            }
                
            TryCount = 0;
        }
        else
        {
            if (TryCount < MaxRetries)
            {
                await Task.Delay(DelayBetweenRetriesMs, cancellationToken);
                return await SendAsync(request, cancellationToken);
            }
            else
            {
                responseMessage = await base.SendAsync(request, cancellationToken);
            }
            
        }

        return responseMessage;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    { 
        await limiter.DisposeAsync().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            limiter.Dispose();
        }
    }
}
