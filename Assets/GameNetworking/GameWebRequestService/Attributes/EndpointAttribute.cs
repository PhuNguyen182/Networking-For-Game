using System;
using GameNetworking.RequestOptimizer.Scripts;

namespace GameNetworking.GameWebRequestService.Attributes
{
    /// <summary>
    /// Attribute để đánh dấu thông tin endpoint cho các response class
    /// </summary>
    /// <remarks>
    /// Attribute này giúp tự động mapping giữa response class và API endpoint,
    /// hỗ trợ object pooling, caching và request optimization.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Critical request - không batch, gửi ngay
    /// [Endpoint("/api/v1/payment/process", "Payment Process", priority: RequestPriority.Critical)]
    /// public class PaymentResponse : BasePostResponse&lt;PaymentData&gt; { }
    /// 
    /// // Analytics - batch aggressive
    /// [Endpoint("/api/v1/analytics/track", "Analytics Tracking", priority: RequestPriority.Batch)]
    /// public class AnalyticsResponse : BasePostResponse&lt;AnalyticsData&gt; { }
    /// 
    /// // Normal request
    /// [Endpoint("/api/v1/user/profile", "User Profile", priority: RequestPriority.Normal)]
    /// public class UserProfileResponse : BaseGetResponse&lt;UserProfileData&gt; { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class EndpointAttribute : Attribute
    {
        /// <summary>
        /// Đường dẫn endpoint API (ví dụ: "/api/v1/user/login")
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Tên mô tả của API (ví dụ: "User Login")
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Timeout mặc định (ms) cho endpoint này
        /// </summary>
        public int TimeoutMilliseconds { get; }

        /// <summary>
        /// Cho phép retry request khi thất bại
        /// </summary>
        public bool AllowRetry { get; }

        /// <summary>
        /// Số lần retry tối đa
        /// </summary>
        public int MaxRetries { get; }
        
        /// <summary>
        /// Priority của request để xác định batching behavior
        /// </summary>
        /// <remarks>
        /// - Critical: Không batch, bypass rate limit, gửi ngay (payment, purchase)
        /// - High: Không batch, không bypass rate limit (important user actions)
        /// - Normal: Có thể batch với size/delay vừa phải (default)
        /// - Low: Có thể batch với size/delay cao hơn (position updates)
        /// - Batch: Batch aggressive với size/delay cao nhất (analytics, telemetry)
        /// </remarks>
        public RequestPriority Priority { get; }

        /// <summary>
        /// Khởi tạo EndpointAttribute với route và các options
        /// </summary>
        /// <param name="route">Đường dẫn endpoint API, ví dụ: "/api/v1/auth/login"</param>
        /// <param name="name">Tên mô tả của API, ví dụ: "User Login"</param>
        /// <param name="priority">Priority của request (Critical, High, Normal, Low, Batch)</param>
        /// <param name="timeoutMilliseconds">Timeout của API tính bằng milliseconds, ví dụ: 3000ms</param>
        /// <param name="allowRetry">Cho phép API được retry hay không</param>
        /// <param name="maxRetries">Số lần retry tối đa được quy định</param>
        public EndpointAttribute(
            string route, 
            string name = "", 
            RequestPriority priority = RequestPriority.Critical,
            int timeoutMilliseconds = 3000, 
            bool allowRetry = true,
            int maxRetries = 3)
        {
            this.Route = route;
            this.Name = name;
            this.Priority = priority;
            this.TimeoutMilliseconds = timeoutMilliseconds;
            this.AllowRetry = allowRetry;
            this.MaxRetries = maxRetries;
        }
    }
}

