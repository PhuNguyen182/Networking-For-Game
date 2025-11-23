using System;
using System.Collections.Generic;
using GameNetworking.WebRequestService.Constants;
using GameNetworking.WebRequestService.Models;
using UnityEngine;

namespace GameNetworking.WebRequestService.Pooling
{
    /// <summary>
    /// Manager quản lý các object pool cho response objects
    /// </summary>
    /// <remarks>
    /// Class này sử dụng Dictionary để quản lý nhiều pool khác nhau,
    /// mỗi pool tương ứng với một kiểu response cụ thể.
    /// Tuân thủ Single Responsibility Principle và Factory Pattern.
    /// </remarks>
    public class ResponsePoolManager
    {
        private readonly Dictionary<Type, object> _pools;
        private readonly object _managerLock = new();
        private readonly int _defaultInitialCapacity;
        private readonly int _defaultMaxCapacity;

        /// <summary>
        /// Khởi tạo ResponsePoolManager với capacity mặc định
        /// </summary>
        /// <param name="initialCapacity">Initial capacity cho mỗi pool</param>
        /// <param name="maxCapacity">Max capacity cho mỗi pool</param>
        public ResponsePoolManager(int initialCapacity = PoolingConstants.InitialCapacity,
            int maxCapacity = PoolingConstants.MaxCapacity)
        {
            this._pools = new Dictionary<Type, object>();
            this._defaultInitialCapacity = initialCapacity;
            this._defaultMaxCapacity = maxCapacity;
        }

        /// <summary>
        /// Lấy response object từ pool
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response cần lấy</typeparam>
        /// <returns>Response object hoặc null nếu thất bại</returns>
        public TResponse Get<TResponse>() where TResponse : class, IPoolable
        {
            var pool = this.GetOrCreatePool<TResponse>();

            if (pool == null)
            {
                Debug.LogError($"[ResponsePoolManager] Failed to get or create pool for type {typeof(TResponse).Name}");
                return null;
            }

            return pool.Get();
        }

        /// <summary>
        /// Trả response object về pool
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <param name="response">Response object cần trả về</param>
        /// <returns>True nếu return thành công</returns>
        public bool Return<TResponse>(TResponse response) where TResponse : class, IPoolable
        {
            if (response == null)
            {
                return false;
            }

            var pool = this.GetOrCreatePool<TResponse>();

            if (pool == null)
            {
                Debug.LogError($"[ResponsePoolManager] Failed to get or create pool for type {typeof(TResponse).Name}");
                return false;
            }

            return pool.Return(response);
        }

        /// <summary>
        /// Lấy hoặc tạo pool cho kiểu response cụ thể
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>Object pool tương ứng</returns>
        private ObjectPool<TResponse> GetOrCreatePool<TResponse>() where TResponse : class, IPoolable
        {
            var type = typeof(TResponse);

            lock (this._managerLock)
            {
                if (this._pools.TryGetValue(type, out var existingPool))
                {
                    return existingPool as ObjectPool<TResponse>;
                }

                try
                {
                    var newPool = new ObjectPool<TResponse>(
                        this._defaultInitialCapacity,
                        this._defaultMaxCapacity
                    );

                    this._pools[type] = newPool;

                    Debug.Log($"[ResponsePoolManager] Created new pool for type {type.Name}");

                    return newPool;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ResponsePoolManager] Failed to create pool for type {type.Name}: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Clear pool cho một kiểu response cụ thể
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        public void ClearPool<TResponse>() where TResponse : class, IPoolable
        {
            var type = typeof(TResponse);

            lock (this._managerLock)
            {
                if (this._pools.TryGetValue(type, out var pool))
                {
                    if (pool is ObjectPool<TResponse> typedPool)
                    {
                        typedPool.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Clear tất cả pools
        /// </summary>
        public void ClearAllPools()
        {
            lock (this._managerLock)
            {
                foreach (var pool in this._pools.Values)
                {
                    if (pool is IPoolable poolable)
                    {
                        poolable.OnReturnToPool();
                    }
                }

                this._pools.Clear();

                Debug.Log("[ResponsePoolManager] All pools cleared");
            }
        }

        /// <summary>
        /// Lấy thông tin về pool cho một kiểu cụ thể
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>String chứa thông tin pool</returns>
        public string GetPoolInfo<TResponse>() where TResponse : class, IPoolable
        {
            var type = typeof(TResponse);

            lock (this._managerLock)
            {
                if (this._pools.TryGetValue(type, out var pool))
                {
                    if (pool is ObjectPool<TResponse> typedPool)
                    {
                        return
                            $"Pool<{type.Name}>: Available={typedPool.AvailableCount}, Active={typedPool.ActiveCount}, Total={typedPool.TotalCount}";
                    }
                }

                return $"Pool<{type.Name}>: Not created yet";
            }
        }

        /// <summary>
        /// Log thông tin tất cả pools
        /// </summary>
        public void LogAllPoolsInfo()
        {
            lock (this._managerLock)
            {
                Debug.Log($"[ResponsePoolManager] Total pools: {this._pools.Count}");

                foreach (var kvp in this._pools)
                {
                    var type = kvp.Key;
                    Debug.Log($"Pool<{type.Name}>: Exists");
                }
            }
        }
    }
}
