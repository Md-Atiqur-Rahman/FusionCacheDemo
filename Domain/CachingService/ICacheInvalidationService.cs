using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.CachingService
{
    /// <summary>
    /// Centralized cache invalidation service
    /// Single Responsibility Principle: Manages cache invalidation logic
    /// </summary>
    public interface ICacheInvalidationService
    {
        Task InvalidateProductCachesAsync(string? category = null);
        Task InvalidateAllCachesAsync();
        Task InvalidateByPatternAsync(string pattern);
    }
}
