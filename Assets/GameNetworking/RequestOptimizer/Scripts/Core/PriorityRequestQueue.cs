using System;
using System.Collections.Generic;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Priority-based request queue với tối ưu performance
    /// </summary>
    public class PriorityRequestQueue : IRequestQueue
    {
        private readonly Dictionary<RequestPriority, Queue<QueuedRequest>> _priorityQueues;
        private readonly int _maxQueueSize;
        
        public PriorityRequestQueue(int maxQueueSize = 1000)
        {
            this._maxQueueSize = maxQueueSize;
            this._priorityQueues = new Dictionary<RequestPriority, Queue<QueuedRequest>>();
            
            foreach (RequestPriority priority in Enum.GetValues(typeof(RequestPriority)))
            {
                this._priorityQueues[priority] = new Queue<QueuedRequest>();
            }
        }
        
        public void Enqueue(QueuedRequest request)
        {
            if (this.TotalQueuedCount >= this._maxQueueSize)
            {
                this.DropLowPriorityRequests(100);
            }
            
            this._priorityQueues[request.priority].Enqueue(request);
        }
        
        public QueuedRequest Dequeue()
        {
            foreach (RequestPriority priority in Enum.GetValues(typeof(RequestPriority)))
            {
                if (this._priorityQueues[priority].Count > 0)
                {
                    return this._priorityQueues[priority].Dequeue();
                }
            }
            
            return null;
        }
        
        public bool HasRequests
        {
            get
            {
                foreach (var queue in this._priorityQueues.Values)
                {
                    if (queue.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public int GetQueueCount(RequestPriority priority)
        {
            return this._priorityQueues[priority].Count;
        }
        
        public int TotalQueuedCount
        {
            get
            {
                var total = 0;
                foreach (var queue in this._priorityQueues.Values)
                {
                    total += queue.Count;
                }
                return total;
            }
        }
        
        public void Clear()
        {
            foreach (var queue in this._priorityQueues.Values)
            {
                queue.Clear();
            }
        }
        
        public void DropLowPriorityRequests(int count)
        {
            var dropped = 0;
            
            for (var i = (int)RequestPriority.Batch; i >= 0 && dropped < count; i--)
            {
                var priority = (RequestPriority)i;
                var queue = this._priorityQueues[priority];
                
                while (queue.Count > 0 && dropped < count)
                {
                    queue.Dequeue();
                    dropped++;
                }
                
                if (dropped >= count)
                {
                    break;
                }
            }
        }
    }
}

