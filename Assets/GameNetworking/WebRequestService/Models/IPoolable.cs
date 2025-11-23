namespace GameNetworking.WebRequestService.Models
{
    /// <summary>
    /// Interface cho các object có thể được pooling
    /// </summary>
    /// <remarks>
    /// Interface này định nghĩa các phương thức cần thiết để object
    /// có thể được quản lý bởi object pool hiệu quả.
    /// </remarks>
    public interface IPoolable
    {
        /// <summary>
        /// Được gọi khi object được trả về pool
        /// </summary>
        /// <remarks>
        /// Method này nên reset tất cả các field về trạng thái mặc định
        /// để tránh memory leak và đảm bảo object sạch khi tái sử dụng.
        /// </remarks>
        public void OnReturnToPool();
        
        /// <summary>
        /// Được gọi khi object được lấy từ pool
        /// </summary>
        /// <remarks>
        /// Method này nên khởi tạo các giá trị cần thiết cho object
        /// trước khi sử dụng.
        /// </remarks>
        public void OnGetFromPool();
    }
}
