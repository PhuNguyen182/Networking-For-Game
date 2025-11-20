namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface quản lý rate limiting cho requests
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Kiểm tra xem có thể gửi request không dựa trên rate limit
        /// </summary>
        /// <returns>True nếu có thể gửi request</returns>
        public bool CanSendRequest();
        
        /// <summary>
        /// Ghi nhận một request đã được gửi để tracking rate limit
        /// </summary>
        public void RecordRequest();
        
        /// <summary>
        /// Kích hoạt rate limit cooldown (thường do 429 response)
        /// </summary>
        /// <param name="cooldownDuration">Thời gian cooldown (seconds)</param>
        public void ActivateRateLimitCooldown(float cooldownDuration);
        
        /// <summary>
        /// Kiểm tra xem hiện tại có đang trong rate limit cooldown không
        /// </summary>
        public bool IsRateLimited { get; }
        
        /// <summary>
        /// Reset tất cả rate limit tracking
        /// </summary>
        public void Reset();
        
        /// <summary>
        /// Cập nhật rate limiter (được gọi định kỳ để clean up old timestamps)
        /// </summary>
        /// <param name="currentTime">Thời gian hiện tại</param>
        public void Update(float currentTime);
    }
}

