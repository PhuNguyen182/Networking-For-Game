using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface định nghĩa chiến lược batching cho các loại request khác nhau.
    /// Cho phép tùy chỉnh cách thức gom nhóm và gửi requests.
    /// </summary>
    public interface IBatchingStrategy
    {
        /// <summary>
        /// Kiểm tra xem request có thể được thêm vào batch hiện tại không
        /// </summary>
        /// <param name="request">Request cần kiểm tra</param>
        /// <param name="currentBatch">Batch hiện tại</param>
        /// <returns>True nếu có thể thêm vào batch</returns>
        public bool CanAddToBatch(QueuedRequest request, IReadOnlyList<QueuedRequest> currentBatch);
        
        /// <summary>
        /// Kiểm tra xem batch có sẵn sàng để gửi không
        /// </summary>
        /// <param name="batch">Batch cần kiểm tra</param>
        /// <param name="firstBatchTime">Thời điểm tạo batch đầu tiên</param>
        /// <returns>True nếu batch sẵn sàng gửi</returns>
        public bool ShouldSendBatch(IReadOnlyList<QueuedRequest> batch, float firstBatchTime);
        
        /// <summary>
        /// Tạo batch request từ danh sách requests
        /// </summary>
        /// <param name="requests">Danh sách requests cần gộp</param>
        /// <returns>Batch request đã được tối ưu</returns>
        public UniTask<QueuedRequest> CreateBatchRequestAsync(IReadOnlyList<QueuedRequest> requests);
        
        /// <summary>
        /// Xử lý response từ batch request và phân phối cho các callback tương ứng
        /// </summary>
        /// <param name="requests">Danh sách requests gốc</param>
        /// <param name="success">Trạng thái thành công</param>
        /// <param name="response">Response từ server</param>
        public UniTask ProcessBatchResponseAsync(IReadOnlyList<QueuedRequest> requests, bool success, string response);
        
        /// <summary>
        /// Lấy kích thước batch tối đa
        /// </summary>
        public int maxBatchingSize { get; }
        
        /// <summary>
        /// Lấy thời gian chờ tối đa trước khi force gửi batch (seconds)
        /// </summary>
        public float MaxBatchDelay { get; }
    }
}

