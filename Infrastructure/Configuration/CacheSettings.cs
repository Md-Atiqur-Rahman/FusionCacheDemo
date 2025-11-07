using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configuration
{
    public class CacheSettings
    {
        public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan FactorySoftTimeout { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan FactoryHardTimeout { get; set; } = TimeSpan.FromSeconds(3);
        public bool EnableDistributedCache { get; set; } = true;
        public bool EnableBackplane { get; set; } = true;
        public bool EnableFailSafe { get; set; } = true;
    }
}
