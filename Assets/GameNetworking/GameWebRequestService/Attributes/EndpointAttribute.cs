using System;

namespace GameNetworking.GameWebRequestService.Attributes
{
    /// <summary>
    /// Attribute để đánh dấu thông tin endpoint cho các response class
    /// </summary>
    /// <remarks>
    /// Attribute này giúp tự động mapping giữa response class và API endpoint,
    /// hỗ trợ object pooling và caching hiệu quả hơn.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Endpoint("/api/v1/user/login", "User Login")]
    /// public class LoginResponse : BaseResponse
    /// {
    ///     public string token;
    ///     public UserData userData;
    /// }
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
        public int TimeoutMilliseconds { get; set; }

        /// <summary>
        /// Cho phép retry request khi thất bại
        /// </summary>
        public bool AllowRetry { get; set; }

        /// <summary>
        /// Số lần retry tối đa
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Khởi tạo EndpointAttribute với path và name
        /// </summary>
        /// <param name="route">Đường dẫn endpoint API, ví dụ: "/api/v1/auth/login"</param>
        /// <param name="name">Tên mô tả của API, ví dụ: "User Login"</param>
        /// <param name="timeoutMilliseconds">Timeout của API tính bằng milliseconds, ví dụ: 3000ms</param>
        /// <param name="allowRetry">Cho phép API được retry hay không</param>
        /// <param name="maxRetries">Số lần retry tối đa được quy định</param>
        public EndpointAttribute(string route, string name = "", int timeoutMilliseconds = 3000, bool allowRetry = true,
            int maxRetries = 3)
        {
            this.Route = route;
            this.Name = name;
            this.TimeoutMilliseconds = timeoutMilliseconds;
            this.AllowRetry = allowRetry;
            this.MaxRetries = maxRetries;
        }
    }
}

