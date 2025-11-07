using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.CachingService
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task RemoveByPatternAsync(string pattern);
    }
}
