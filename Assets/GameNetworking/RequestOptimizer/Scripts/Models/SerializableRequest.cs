using System;

namespace GameNetworking.RequestOptimizer.Scripts.Models
{
    /// <summary>
    /// Request có thể serialize để lưu vào offline storage
    /// Không chứa callback để có thể serialize an toàn
    /// </summary>
    [Serializable]
    public struct SerializableRequest
    {
        public string endpoint;
        public string jsonBody;
        public RequestPriority priority;
        public string requestId;
        public float queuedTime;
        public int retryCount;
        
        public SerializableRequest(QueuedRequest request)
        {
            this.endpoint = request.endpoint;
            this.jsonBody = request.jsonBody;
            this.priority = request.priority;
            this.requestId = request.requestId;
            this.queuedTime = request.queuedTime;
            this.retryCount = request.retryCount;
        }
    }
}

