using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DistributedLockService
{
    /// <summary>
    /// Implements distributed locking to prevent cache stampede
    /// </summary>
    public interface IDistributedLockService
    {
        Task<bool> AcquireLockAsync(string key, TimeSpan expiry);
        Task<bool> ReleaseLockAsync(string key);
    }
}
