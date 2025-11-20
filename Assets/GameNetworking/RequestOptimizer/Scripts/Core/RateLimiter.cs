using System.Collections.Generic;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Rate limiter với sliding window algorithm để tối ưu performance
    /// </summary>
    public class RateLimiter : IRateLimiter
    {
        private readonly int _maxRequestsPerSecond;
        private readonly int _maxRequestsPerMinute;
        private readonly float _rateLimitCooldownDuration;
        
        private readonly Queue<float> _requestTimestampsSecond;
        private readonly Queue<float> _requestTimestampsMinute;
        
        private bool _isRateLimited;
        private float _rateLimitEndTime;
        
        public RateLimiter(int maxRequestsPerSecond = 10, int maxRequestsPerMinute = 300, 
            float rateLimitCooldownDuration = 60f)
        {
            this._maxRequestsPerSecond = maxRequestsPerSecond;
            this._maxRequestsPerMinute = maxRequestsPerMinute;
            this._rateLimitCooldownDuration = rateLimitCooldownDuration;
            
            this._requestTimestampsSecond = new Queue<float>();
            this._requestTimestampsMinute = new Queue<float>();
            this._isRateLimited = false;
            this._rateLimitEndTime = 0f;
        }
        
        public bool CanSendRequest()
        {
            if (this._isRateLimited)
            {
                return false;
            }
            
            if (this._requestTimestampsSecond.Count >= this._maxRequestsPerSecond)
            {
                return false;
            }
            
            if (this._requestTimestampsMinute.Count >= this._maxRequestsPerMinute)
            {
                return false;
            }
            
            return true;
        }
        
        public void RecordRequest()
        {
            var now = Time.realtimeSinceStartup;
            this._requestTimestampsSecond.Enqueue(now);
            this._requestTimestampsMinute.Enqueue(now);
        }
        
        public void ActivateRateLimitCooldown(float cooldownDuration)
        {
            this._isRateLimited = true;
            this._rateLimitEndTime = Time.realtimeSinceStartup + cooldownDuration;
        }
        
        public bool IsRateLimited => this._isRateLimited;
        
        public void Reset()
        {
            this._requestTimestampsSecond.Clear();
            this._requestTimestampsMinute.Clear();
            this._isRateLimited = false;
            this._rateLimitEndTime = 0f;
        }
        
        public void Update(float currentTime)
        {
            if (this._isRateLimited && currentTime >= this._rateLimitEndTime)
            {
                this._isRateLimited = false;
            }
            
            while (this._requestTimestampsSecond.Count > 0 && 
                   currentTime - this._requestTimestampsSecond.Peek() > 1f)
            {
                this._requestTimestampsSecond.Dequeue();
            }
            
            while (this._requestTimestampsMinute.Count > 0 && 
                   currentTime - this._requestTimestampsMinute.Peek() > 60f)
            {
                this._requestTimestampsMinute.Dequeue();
            }
        }
    }
}

