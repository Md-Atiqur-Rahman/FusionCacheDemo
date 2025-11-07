using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.RateLimiterService
{
    /// <summary>
    /// Implements rate limiting using Redis
    /// </summary>
    public interface IRateLimiterService
    {
        Task<bool> IsAllowedAsync(string identifier, int maxRequests, TimeSpan window);
    }
}
