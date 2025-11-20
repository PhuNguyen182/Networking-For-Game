using System;
using GameNetworking.RequestOptimizer.Scripts.Configuration;

namespace GameNetworking.RequestOptimizer.Scripts.Models
{
    /// <summary>
    /// Dữ liệu của một request trong queue với tất cả thông tin cần thiết
    /// </summary>
    [Serializable]
    public class QueuedRequest
    {
        public string endpoint;
        public string jsonBody;
        public RequestPriority priority;
        public RequestConfig config;
        public float queuedTime;
        public int retryCount;
        public Action<bool, string> Callback;
        public string requestId;
        
        public QueuedRequest(string endpoint, string body, RequestPriority priority, RequestConfig config,
            Action<bool, string> callback = null)
        {
            this.endpoint = endpoint;
            this.jsonBody = body;
            this.priority = priority;
            this.config = config;
            this.queuedTime = UnityEngine.Time.realtimeSinceStartup;
            this.retryCount = 0;
            this.Callback = callback;
            this.requestId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Constructor cho deserialization từ offline storage
        /// </summary>
        public QueuedRequest(string endpoint, string body, RequestPriority priority, RequestConfig config,
            string requestId, float queuedTime, int retryCount)
        {
            this.endpoint = endpoint;
            this.jsonBody = body;
            this.priority = priority;
            this.config = config;
            this.requestId = requestId;
            this.queuedTime = queuedTime;
            this.retryCount = retryCount;
            this.Callback = null;
        }
        
        /// <summary>
        /// Tạo một bản copy của request với retry count tăng lên
        /// </summary>
        public QueuedRequest WithIncrementedRetry()
        {
            return new QueuedRequest(
                this.endpoint,
                this.jsonBody,
                this.priority,
                this.config,
                this.requestId,
                this.queuedTime,
                this.retryCount + 1
            )
            {
                Callback = this.Callback
            };
        }
    }
}

