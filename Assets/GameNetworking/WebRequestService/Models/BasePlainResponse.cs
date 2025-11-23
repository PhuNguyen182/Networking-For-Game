using System;

namespace GameNetworking.WebRequestService.Models
{
    /// <summary>
    /// Base class cho tất cả response models
    /// </summary>
    /// <remarks>
    /// Class này cung cấp các thông tin cơ bản về response từ server,
    /// hỗ trợ object pooling và tự động reset state.
    /// </remarks>
    [Serializable]
    public abstract class BasePlainResponse : IPoolable
    {
        /// <summary>
        /// HTTP status code của response
        /// </summary>
        public int statusCode;

        /// <summary>
        /// Message từ server (nếu có)
        /// </summary>
        public string message;

        /// <summary>
        /// Timestamp khi nhận response (Unix timestamp)
        /// </summary>
        public long timestamp;

        /// <summary>
        /// Request có thành công hay không
        /// </summary>
        public bool IsSuccess => statusCode is >= 200 and < 300;

        /// <summary>
        /// Reset object về trạng thái mặc định để tái sử dụng trong pool
        /// </summary>
        public virtual void OnReturnToPool()
        {
            this.statusCode = 0;
            this.message = null;
            this.timestamp = 0;
        }

        /// <summary>
        /// Khởi tạo object khi lấy từ pool
        /// </summary>
        public virtual void OnGetFromPool()
        {
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
