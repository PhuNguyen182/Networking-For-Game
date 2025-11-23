using System;
using System.Collections.Generic;
using GameNetworking.WebRequestService.Models;
using PracticalModules.TypeCreator.Core;
using UnityEngine;

namespace GameNetworking.WebRequestService.Pooling
{
    /// <summary>
    /// Generic object pool sử dụng TypeFactory cho hiệu suất tối ưu
    /// </summary>
    /// <typeparam name="T">Kiểu object cần pool (phải implement IPoolable)</typeparam>
    /// <remarks>
    /// Class này sử dụng TypeFactory thay vì Activator.CreateInstance để đạt
    /// hiệu suất cao hơn 100x khi tạo object. Pool được thiết kế thread-safe
    /// và tự động mở rộng khi cần thiết.
    /// </remarks>
    public class ObjectPool<T> where T : class, IPoolable
    {
        private readonly Queue<T> _availableObjects;
        private readonly HashSet<T> _activeObjects;
        private readonly object _poolLock = new();
        private readonly int _initialCapacity;
        private readonly int _maxCapacity;

        /// <summary>
        /// Số lượng object đang available trong pool
        /// </summary>
        public int AvailableCount
        {
            get
            {
                lock (this._poolLock)
                {
                    return this._availableObjects.Count;
                }
            }
        }

        /// <summary>
        /// Số lượng object đang được sử dụng
        /// </summary>
        public int ActiveCount
        {
            get
            {
                lock (this._poolLock)
                {
                    return this._activeObjects.Count;
                }
            }
        }

        /// <summary>
        /// Tổng số object trong pool (available + active)
        /// </summary>
        public int TotalCount => this.AvailableCount + this.ActiveCount;

        /// <summary>
        /// Khởi tạo object pool với capacity được chỉ định
        /// </summary>
        /// <param name="initialCapacity">Số lượng object khởi tạo ban đầu</param>
        /// <param name="maxCapacity">Số lượng object tối đa trong pool (0 = unlimited)</param>
        public ObjectPool(int initialCapacity = 10, int maxCapacity = 100)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentException("Initial capacity cannot be negative", nameof(initialCapacity));
            }

            if (maxCapacity < 0)
            {
                throw new ArgumentException("Max capacity cannot be negative", nameof(maxCapacity));
            }

            if (maxCapacity > 0 && initialCapacity > maxCapacity)
            {
                throw new ArgumentException("Initial capacity cannot exceed max capacity");
            }

            this._initialCapacity = initialCapacity;
            this._maxCapacity = maxCapacity;
            this._availableObjects = new Queue<T>(initialCapacity);
            this._activeObjects = new HashSet<T>();

            this.PrewarmPool();
        }

        /// <summary>
        /// Prewarm pool bằng cách tạo sẵn các object ban đầu
        /// </summary>
        private void PrewarmPool()
        {
            try
            {
                for (int i = 0; i < this._initialCapacity; i++)
                {
                    var obj = this.CreateNewObject();
                    if (obj != null)
                    {
                        this._availableObjects.Enqueue(obj);
                    }
                }

                if (this._availableObjects.Count > 0)
                {
                    Debug.Log(
                        $"[ObjectPool<{typeof(T).Name}>] Prewarmed pool with {this._availableObjects.Count} objects");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ObjectPool<{typeof(T).Name}>] Failed to prewarm pool: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy object từ pool hoặc tạo mới nếu pool rỗng
        /// </summary>
        /// <returns>Object từ pool hoặc null nếu không thể tạo</returns>
        public T Get()
        {
            lock (this._poolLock)
            {
                T obj;

                if (this._availableObjects.Count > 0)
                {
                    obj = this._availableObjects.Dequeue();
                }
                else
                {
                    // Check max capacity
                    if (this._maxCapacity > 0 && this.TotalCount >= this._maxCapacity)
                    {
                        Debug.LogWarning(
                            $"[ObjectPool<{typeof(T).Name}>] Pool reached max capacity ({this._maxCapacity})");
                        return null;
                    }

                    obj = this.CreateNewObject();

                    if (obj == null)
                    {
                        Debug.LogError($"[ObjectPool<{typeof(T).Name}>] Failed to create new object");
                        return null;
                    }
                }

                this._activeObjects.Add(obj);
                obj.OnGetFromPool();

                return obj;
            }
        }

        /// <summary>
        /// Trả object về pool để tái sử dụng
        /// </summary>
        /// <param name="obj">Object cần trả về pool</param>
        /// <returns>True nếu return thành công, false nếu thất bại</returns>
        public bool Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Cannot return null object to pool");
                return false;
            }

            lock (this._poolLock)
            {
                if (!this._activeObjects.Contains(obj))
                {
                    Debug.LogWarning(
                        $"[ObjectPool<{typeof(T).Name}>] Object is not from this pool or already returned");
                    return false;
                }

                this._activeObjects.Remove(obj);
                obj.OnReturnToPool();
                this._availableObjects.Enqueue(obj);

                return true;
            }
        }

        /// <summary>
        /// Xóa tất cả object trong pool và reset về trạng thái ban đầu
        /// </summary>
        public void Clear()
        {
            lock (this._poolLock)
            {
                this._availableObjects.Clear();
                this._activeObjects.Clear();

                Debug.Log($"[ObjectPool<{typeof(T).Name}>] Pool cleared");
            }
        }

        /// <summary>
        /// Tạo object mới sử dụng TypeFactory để đạt hiệu suất tối ưu
        /// </summary>
        /// <returns>Object mới hoặc null nếu không thể tạo</returns>
        private T CreateNewObject()
        {
            try
            {
                // Sử dụng TypeFactory thay vì Activator.CreateInstance
                // để đạt hiệu suất cao hơn 100x+
                if (!TypeFactory.CanCreate<T>())
                {
                    Debug.LogError(
                        $"[ObjectPool<{typeof(T).Name}>] Type cannot be created - ensure it has a parameterless constructor");
                    return null;
                }

                return TypeFactory.Create<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ObjectPool<{typeof(T).Name}>] Failed to create object: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Log thông tin pool cho debugging
        /// </summary>
        public void LogPoolInfo()
        {
            lock (this._poolLock)
            {
                Debug.Log(
                    $"[ObjectPool<{typeof(T).Name}>] Available: {this._availableObjects.Count}, Active: {this._activeObjects.Count}, Total: {this.TotalCount}");
            }
        }
    }
}
