using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PawSharp.API.RateLimit
{
    public class RateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly ConcurrentQueue<DateTime> _requestTimestamps;

        public RateLimiter(int maxRequests, TimeSpan timeWindow)
        {
            _maxRequests = maxRequests;
            _timeWindow = timeWindow;
            _requestTimestamps = new ConcurrentQueue<DateTime>();
        }

        public bool TryAcquire()
        {
            DateTime now = DateTime.UtcNow;

            // Remove timestamps that are outside the time window
            while (_requestTimestamps.TryPeek(out DateTime timestamp) && (now - timestamp) > _timeWindow)
            {
                _requestTimestamps.TryDequeue(out _);
            }

            // Check if we can add a new request
            if (_requestTimestamps.Count < _maxRequests)
            {
                _requestTimestamps.Enqueue(now);
                return true;
            }

            return false; // Rate limit exceeded
        }
    }
}