using System;
using Cysharp.Threading.Tasks;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface theo dõi trạng thái kết nối mạng
    /// </summary>
    public interface INetworkMonitor
    {
        /// <summary>
        /// Kiểm tra xem hiện tại có kết nối mạng không
        /// </summary>
        public bool IsOnline { get; }
        
        /// <summary>
        /// Event được trigger khi trạng thái mạng thay đổi
        /// </summary>
        public event Action<bool> OnNetworkStatusChanged;
        
        /// <summary>
        /// Bắt đầu theo dõi trạng thái mạng
        /// </summary>
        /// <param name="cancellationToken">Token để hủy monitoring</param>
        public UniTask StartMonitoringAsync(System.Threading.CancellationToken cancellationToken);
        
        /// <summary>
        /// Thực hiện health check thủ công
        /// </summary>
        /// <returns>True nếu có kết nối mạng</returns>
        public UniTask<bool> CheckConnectionAsync();
        
        /// <summary>
        /// Dừng monitoring
        /// </summary>
        public void StopMonitoring();
    }
}

