using System.Collections.Generic;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.BatchingStrategies
{
    /// <summary>
    /// Chiến lược batching thích ứng - điều chỉnh dựa trên load và network conditions
    /// Phù hợp cho mixed workloads
    /// </summary>
    public class AdaptiveBatchingStrategy : BaseBatchingStrategy
    {
        private float _currentBatchDelay;
        private int _currentOptimalSize;
        private int _successfulBatchCount = 0;
        private int _failedBatchCount = 0;
        
        private readonly float _minDelay;
        private readonly float _maxDelay;
        private readonly int _minBatchSize;
        
        public AdaptiveBatchingStrategy(int maxBatchingSize, float maxBatchDelay, 
            float minDelay = 0.5f, int minBatchSize = 5) 
            : base(maxBatchingSize, maxBatchDelay)
        {
            this._minDelay = minDelay;
            this._maxDelay = maxBatchDelay;
            this._minBatchSize = minBatchSize;
            this._currentBatchDelay = (minDelay + maxBatchDelay) / 2f;
            this._currentOptimalSize = maxBatchingSize / 2;
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
            
            if (batch.Count >= this._currentOptimalSize)
            {
                var elapsedTime = Time.realtimeSinceStartup - firstBatchTime;
                return elapsedTime >= this._currentBatchDelay;
            }
            
            var fullElapsedTime = Time.realtimeSinceStartup - firstBatchTime;
            return fullElapsedTime >= this._maxDelay;
        }
        
        /// <summary>
        /// Cập nhật strategy dựa trên kết quả của batch trước
        /// </summary>
        public void RecordBatchResult(bool success, int batchSize)
        {
            if (success)
            {
                this._successfulBatchCount++;
                
                if (batchSize >= this._currentOptimalSize)
                {
                    this._currentOptimalSize = Mathf.Min(
                        this._currentOptimalSize + 2,
                        this.MaxBatchingSize
                    );
                }
                
                this._currentBatchDelay = Mathf.Max(
                    this._currentBatchDelay * 0.95f,
                    this._minDelay
                );
            }
            else
            {
                this._failedBatchCount++;
                
                this._currentOptimalSize = Mathf.Max(
                    this._currentOptimalSize - 5,
                    this._minBatchSize
                );
                
                this._currentBatchDelay = Mathf.Min(
                    this._currentBatchDelay * 1.2f,
                    this._maxDelay
                );
            }
        }
        
        /// <summary>
        /// Reset về giá trị mặc định
        /// </summary>
        public void Reset()
        {
            this._currentBatchDelay = (this._minDelay + this._maxDelay) / 2f;
            this._currentOptimalSize = this.MaxBatchingSize / 2;
            this._successfulBatchCount = 0;
            this._failedBatchCount = 0;
        }
    }
}

