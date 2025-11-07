using Domain.RateLimiterService;

namespace Api.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimiterService _rateLimiter;
        private static readonly int MaxRequests = 100;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        public RateLimitMiddleware(
            RequestDelegate next,
            IRateLimiterService rateLimiter)
        {
            _next = next;
            _rateLimiter = rateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var allowed = await _rateLimiter.IsAllowedAsync(ipAddress, MaxRequests, Window);

            if (!allowed)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }

            await _next(context);
        }
    }
}
