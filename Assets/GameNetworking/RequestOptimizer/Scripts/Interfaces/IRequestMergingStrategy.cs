using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface cho Request Merging Strategy
    /// Khác với Batching (gộp nhiều requests thành array), Merging gộp nhiều requests thành 1 request duy nhất
    /// Ví dụ: 10 requests update player position → 1 request với position cuối cùng
    /// </summary>
    public interface IRequestMergingStrategy
    {
        /// <summary>
        /// Kiểm tra xem request có thể merge với các requests hiện tại không
        /// </summary>
        /// <param name="newRequest">Request mới cần kiểm tra</param>
        /// <param name="existingRequests">Các requests đã có trong merge buffer</param>
        /// <returns>True nếu có thể merge</returns>
        public bool CanMerge(QueuedRequest newRequest, IReadOnlyList<QueuedRequest> existingRequests);
        
        /// <summary>
        /// Merge nhiều requests thành 1 request duy nhất
        /// </summary>
        /// <param name="requests">Danh sách requests cần merge</param>
        /// <returns>Request đã được merge</returns>
        public UniTask<QueuedRequest> MergeRequestsAsync(IReadOnlyList<QueuedRequest> requests);
        
        /// <summary>
        /// Xử lý response từ merged request và phân phối callbacks
        /// </summary>
        /// <param name="originalRequests">Danh sách requests gốc</param>
        /// <param name="mergedResponse">Response từ merged request</param>
        /// <param name="success">Trạng thái thành công</param>
        public UniTask ProcessMergedResponseAsync(
            IReadOnlyList<QueuedRequest> originalRequests,
            string mergedResponse,
            bool success
        );
        
        /// <summary>
        /// Lấy merge key để nhóm các requests có thể merge với nhau
        /// Ví dụ: "player_update:123" cho player ID 123
        /// </summary>
        public string GetMergeKey(QueuedRequest request);
        
        /// <summary>
        /// Thời gian chờ tối đa trước khi force gửi merged request (seconds)
        /// </summary>
        public float MaxMergeDelay { get; }
    }
}

