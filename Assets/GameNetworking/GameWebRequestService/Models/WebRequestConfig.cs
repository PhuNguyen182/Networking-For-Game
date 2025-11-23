using System;

namespace GameNetworking.GameWebRequestService.Models
{
    /// <summary>
    /// Configuration cho web request service
    /// </summary>
    /// <remarks>
    /// Class này chứa các cấu hình cho request như timeout, retry logic, v.v.
    /// Tuân thủ Single Responsibility Principle.
    /// </remarks>
    [Serializable]
    public class WebRequestConfig
    {
        /// <summary>
        /// Base URL cho tất cả các request (ví dụ: "https://api.example.com")
        /// </summary>
        public string baseUrl;
        
        /// <summary>
        /// Timeout mặc định cho request (milliseconds)
        /// </summary>
        public int defaultTimeoutMs;
        
        /// <summary>
        /// Số lần retry tối đa khi request thất bại
        /// </summary>
        public int maxRetries;
        
        /// <summary>
        /// Delay giữa các lần retry (milliseconds)
        /// </summary>
        public int retryDelayMs;
        
        /// <summary>
        /// Có sử dụng exponential backoff cho retry không
        /// </summary>
        public bool useExponentialBackoff;
        
        /// <summary>
        /// Có log request details không (for debugging)
        /// </summary>
        public bool enableLogging;
        
        /// <summary>
        /// Có log request body không (có thể chứa sensitive data)
        /// </summary>
        public bool logRequestBody;
        
        /// <summary>
        /// Có log response body không
        /// </summary>
        public bool logResponseBody;
        
        /// <summary>
        /// Khởi tạo config với giá trị mặc định
        /// </summary>
        public WebRequestConfig()
        {
            this.baseUrl = string.Empty;
            this.defaultTimeoutMs = 30000; // 30 seconds
            this.maxRetries = 3;
            this.retryDelayMs = 1000; // 1 second
            this.useExponentialBackoff = true;
            this.enableLogging = true;
            this.logRequestBody = false; // Disable by default for security
            this.logResponseBody = true;
        }
    }
}
