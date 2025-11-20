using System.Collections.Generic;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.BatchingStrategies
{
    /// <summary>
    /// Chiến lược batching dựa trên kích thước - ưu tiên đạt batch size tối đa
    /// Phù hợp cho game events, player actions
    /// </summary>
    public class SizeBasedBatchingStrategy : BaseBatchingStrategy
    {
        private readonly int _optimalBatchSize;
        
        public SizeBasedBatchingStrategy(int maxBatchingSize, float maxBatchDelay, int optimalBatchSize = 0) 
            : base(maxBatchingSize, maxBatchDelay)
        {
            this._optimalBatchSize = optimalBatchSize > 0 ? optimalBatchSize : maxBatchingSize / 2;
        }
        
        public override bool ShouldSendBatch(IReadOnlyList<QueuedRequest> batch, float firstBatchTime)
        {
            if (batch.Count == 0)
            {
                return false;
            }
            
            if (batch.Count >= this.MaxBatchingSize)
            {
                return true;
            }
            
            if (batch.Count >= this._optimalBatchSize)
            {
                var elapsedTime = Time.realtimeSinceStartup - firstBatchTime;
                return elapsedTime >= this.MaxBatchingDelay * 0.5f;
            }
            
            var fullElapsedTime = Time.realtimeSinceStartup - firstBatchTime;
            return fullElapsedTime >= this.MaxBatchingDelay;
        }
    }
}

