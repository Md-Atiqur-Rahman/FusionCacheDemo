using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Monitoring
{
    public class CacheMetrics
    {
        private readonly Counter<long> _cacheHits;
        private readonly Counter<long> _cacheMisses;
        private readonly Histogram<double> _cacheOperationDuration;

        public CacheMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("RedisCacheDemo.Cache");

            _cacheHits = meter.CreateCounter<long>(
                "cache.hits",
                description: "Number of cache hits");

            _cacheMisses = meter.CreateCounter<long>(
                "cache.misses",
                description: "Number of cache misses");

            _cacheOperationDuration = meter.CreateHistogram<double>(
                "cache.operation.duration",
                unit: "ms",
                description: "Duration of cache operations");
        }

        public void RecordCacheHit(string key)
        {
            _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", key));
        }

        public void RecordCacheMiss(string key)
        {
            _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key", key));
        }

        public void RecordOperationDuration(string operation, double durationMs)
        {
            _cacheOperationDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
        }
    }
}
