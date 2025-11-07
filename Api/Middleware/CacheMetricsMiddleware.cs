using System.Diagnostics;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware to track cache-related metrics
    /// </summary>
    public class CacheMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CacheMetricsMiddleware> _logger;

        public CacheMetricsMiddleware(
            RequestDelegate next,
            ILogger<CacheMetricsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            // Add cache-related headers
            context.Response.OnStarting(() =>
            {
                sw.Stop();
                context.Response.Headers["X-Response-Time"] = $"{sw.ElapsedMilliseconds}ms";

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
