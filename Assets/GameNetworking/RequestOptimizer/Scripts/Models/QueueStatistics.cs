using System;

namespace GameNetworking.RequestOptimizer.Scripts.Models
{
    /// <summary>
    /// Thống kê về trạng thái của request queue
    /// </summary>
    public readonly struct QueueStatistics
    {
        public readonly int TotalQueued;
        public readonly bool IsRateLimited;
        public readonly bool IsOnline;
        public readonly DateTime Timestamp;
        
        private readonly int _activeRequests;
        private readonly int _totalBatched;
        private readonly int _offlineQueueCount;
        
        public QueueStatistics(int totalQueued, int totalBatched, int activeRequests, 
            bool isRateLimited, bool isOnline, int offlineQueueCount)
        {
            this.TotalQueued = totalQueued;
            this._totalBatched = totalBatched;
            this._activeRequests = activeRequests;
            this.IsRateLimited = isRateLimited;
            this.IsOnline = isOnline;
            this._offlineQueueCount = offlineQueueCount;
            this.Timestamp = DateTime.UtcNow;
        }
        
        public override string ToString()
        {
            return $"[QueueStats] Queued: {this.TotalQueued}, Batched: {this._totalBatched}, " +
                   $"Active: {this._activeRequests}, RateLimited: {this.IsRateLimited}, " +
                   $"Online: {this.IsOnline}, Offline: {this._offlineQueueCount}";
        }
    }
}

