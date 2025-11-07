using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.CachingService
{
    /// <summary>
    /// Demonstrates Adaptive Caching - cache duration based on the value itself
    /// This is a UNIQUE FusionCache feature!
    /// </summary>
    public interface IAdaptiveCachingService
    {
        Task<Product?> GetProductWithAdaptiveCachingAsync(string id);
    }
}
