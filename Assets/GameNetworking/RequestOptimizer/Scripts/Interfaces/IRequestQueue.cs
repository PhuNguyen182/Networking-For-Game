using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface quản lý queue requests với priority-based processing
    /// </summary>
    public interface IRequestQueue
    {
        /// <summary>
        /// Thêm request vào queue với priority tương ứng
        /// </summary>
        /// <param name="request">Request cần thêm vào queue</param>
        public void Enqueue(QueuedRequest request);
        
        /// <summary>
        /// Lấy request có priority cao nhất từ queue
        /// </summary>
        /// <returns>Request có priority cao nhất hoặc null nếu queue rỗng</returns>
        public QueuedRequest Dequeue();
        
        /// <summary>
        /// Kiểm tra xem có request nào trong queue không
        /// </summary>
        public bool HasRequests { get; }
        
        /// <summary>
        /// Lấy số lượng request trong queue theo priority
        /// </summary>
        public int GetQueueCount(RequestPriority priority);
        
        /// <summary>
        /// Lấy tổng số request trong tất cả queues
        /// </summary>
        public int TotalQueuedCount { get; }
        
        /// <summary>
        /// Xóa tất cả requests trong queue
        /// </summary>
        public void Clear();
        
        /// <summary>
        /// Xóa requests có priority thấp nhất khi queue đầy
        /// </summary>
        /// <param name="count">Số lượng requests cần xóa</param>
        public void DropLowPriorityRequests(int count);
    }
}

