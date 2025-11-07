using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.CachingService
{
    /// <summary>
    /// Demonstrates tag-based cache invalidation
    /// Invalidate multiple related cache entries at once
    /// </summary>
    public interface ITagBasedCachingService
    {
        Task<string> GetUserDataAsync(string userId, string[] tags);
        Task InvalidateByTagAsync(string tag);
    }
}
