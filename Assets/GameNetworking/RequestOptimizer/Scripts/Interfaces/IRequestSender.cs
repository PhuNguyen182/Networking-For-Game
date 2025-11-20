using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface gửi HTTP requests với error handling và retry logic
    /// </summary>
    public interface IRequestSender
    {
        /// <summary>
        /// Gửi request với retry logic và error handling
        /// </summary>
        /// <param name="request">Request cần gửi</param>
        /// <returns>RequestResult chứa kết quả và response</returns>
        public UniTask<RequestResult> SendRequestAsync(QueuedRequest request);
        
        /// <summary>
        /// Gửi request ngay lập tức mà không qua queue (cho critical requests)
        /// </summary>
        /// <param name="request">Request cần gửi</param>
        /// <returns>RequestResult chứa kết quả và response</returns>
        public UniTask<RequestResult> SendRequestImmediateAsync(QueuedRequest request);
        
        /// <summary>
        /// Lấy số lượng requests đang được gửi (concurrent requests)
        /// </summary>
        public int ActiveRequestsCount { get; }
    }
}

