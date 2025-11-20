using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.OnlineChecking;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// INetworkMonitor implementation sử dụng OnlineCheckService cho việc kiểm tra kết nối internet.
    /// </summary>
    public class OnlineCheckNetworkMonitor : INetworkMonitor, IDisposable
    {
        private readonly OnlineCheckService _onlineCheckService;
        
        private CancellationTokenRegistration _cancellationTokenRegistration;
        private bool _isMonitoring;
        private bool _isDisposed;

        public event Action<bool> OnNetworkStatusChanged;

        public bool IsOnline => this._onlineCheckService.IsConnected;

        public OnlineCheckNetworkMonitor(OnlineCheckService onlineCheckService = null)
        {
            this._onlineCheckService = onlineCheckService ?? new OnlineCheckService();
        }

        public async UniTask StartMonitoringAsync(CancellationToken cancellationToken)
        {
            if (this._isMonitoring)
            {
                return;
            }

            this._isMonitoring = true;
            this._onlineCheckService.OnReconnected += this.HandleReconnected;
            this._onlineCheckService.OnDisconnected += this.HandleDisconnected;
            this._cancellationTokenRegistration = cancellationToken.Register(this.StopMonitoring);

            // Emit trạng thái ban đầu
            this.OnNetworkStatusChanged?.Invoke(this._onlineCheckService.IsConnected);

            await UniTask.CompletedTask;
        }

        public UniTask<bool> CheckConnectionAsync()
        {
            return UniTask.FromResult(this._onlineCheckService.IsConnected);
        }

        public void StopMonitoring()
        {
            if (!this._isMonitoring)
            {
                return;
            }

            this._isMonitoring = false;
            this._cancellationTokenRegistration.Dispose();
            this._onlineCheckService.OnReconnected -= this.HandleReconnected;
            this._onlineCheckService.OnDisconnected -= this.HandleDisconnected;
        }

        private void HandleReconnected()
        {
            this.OnNetworkStatusChanged?.Invoke(true);
        }

        private void HandleDisconnected()
        {
            this.OnNetworkStatusChanged?.Invoke(false);
        }

        public void Dispose()
        {
            if (this._isDisposed)
            {
                return;
            }

            this.StopMonitoring();
            this._onlineCheckService.Dispose();
            this._isDisposed = true;
        }
    }
}

