using System;

namespace GameNetworking.GameWebRequestService.Models
{
    /// <summary>
    /// Base class cho tất cả GET response models
    /// </summary>
    /// <typeparam name="TResponseData">Kiểu dữ liệu response từ server</typeparam>
    /// <remarks>
    /// Class này cung cấp abstract methods để xử lý response thành công và thất bại.
    /// Kế thừa class này và implement 2 abstract methods để xử lý custom logic.
    /// </remarks>
    [Serializable]
    public abstract class BaseGetResponse<TResponseData> : IBaseResponse, IPoolable where TResponseData : class
    {
        /// <summary>
        /// HTTP status code của response
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Message từ server (nếu có)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Timestamp khi nhận response (Unix timestamp)
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Response data từ server
        /// </summary>
        public TResponseData data;

        /// <summary>
        /// Request có thành công hay không
        /// </summary>
        public bool IsSuccess => this.StatusCode is >= 200 and < 300;

        /// <summary>
        /// Xử lý khi response thành công
        /// </summary>
        /// <param name="result">Dữ liệu response từ server</param>
        public abstract void OnResponseSuccess(TResponseData result);

        /// <summary>
        /// Xử lý khi response thất bại
        /// </summary>
        /// <param name="errorCode">HTTP status code lỗi</param>
        /// <param name="errorMessage">Message mô tả lỗi</param>
        public abstract void OnResponseFailed(int errorCode, string errorMessage);

        /// <summary>
        /// Reset object về trạng thái mặc định để tái sử dụng trong pool
        /// </summary>
        public virtual void OnReturnToPool()
        {
            this.StatusCode = 0;
            this.Message = null;
            this.Timestamp = 0;
            this.data = null;
        }

        /// <summary>
        /// Khởi tạo object khi lấy từ pool
        /// </summary>
        public virtual void OnGetFromPool()
        {
            this.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Process response và gọi callback tương ứng
        /// </summary>
        public void ProcessResponse()
        {
            if (this.IsSuccess && this.data != null)
            {
                this.OnResponseSuccess(this.data);
            }
            else
            {
                this.OnResponseFailed(this.StatusCode, this.Message ?? "Unknown error");
            }
        }
    }
}
