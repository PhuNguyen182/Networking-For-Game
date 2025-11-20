using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface lưu trữ offline queue để xử lý khi mất kết nối
    /// </summary>
    public interface IOfflineQueueStorage
    {
        /// <summary>
        /// Lưu request vào offline storage
        /// </summary>
        /// <param name="request">Request cần lưu</param>
        public UniTask SaveRequestAsync(QueuedRequest request);
        
        /// <summary>
        /// Lưu nhiều requests vào offline storage
        /// </summary>
        /// <param name="requests">Danh sách requests cần lưu</param>
        public UniTask SaveRequestsAsync(IEnumerable<QueuedRequest> requests);
        
        /// <summary>
        /// Load tất cả offline requests
        /// </summary>
        /// <returns>Danh sách requests đã lưu</returns>
        public UniTask<List<QueuedRequest>> LoadRequestsAsync();
        
        /// <summary>
        /// Xóa tất cả offline requests
        /// </summary>
        public UniTask ClearAsync();
        
        /// <summary>
        /// Lấy số lượng requests trong offline storage
        /// </summary>
        public UniTask<int> GetCountAsync();
        
        /// <summary>
        /// Kiểm tra xem offline storage có đầy không
        /// </summary>
        public bool IsFull { get; }
    }
}

