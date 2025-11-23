namespace GameNetworking.WebRequestService.Models
{
    /// <summary>
    /// Interface chung cho tất cả base response classes
    /// </summary>
    /// <remarks>
    /// Interface này cho phép xử lý các response class một cách generic
    /// mà không cần biết kiểu TResponseData cụ thể.
    /// </remarks>
    public interface IBaseResponse
    {
        /// <summary>
        /// HTTP status code của response
        /// </summary>
        public int StatusCode { get; set; }
        
        /// <summary>
        /// Message từ server
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Timestamp khi nhận response
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Request có thành công hay không
        /// </summary>
        public bool IsSuccess { get; }
    }
}
