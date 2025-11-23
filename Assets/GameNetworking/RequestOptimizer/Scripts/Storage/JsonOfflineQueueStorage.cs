using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using GameNetworking.RequestOptimizer.Scripts.Utilities;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Storage
{
    /// <summary>
    /// Offline queue storage sử dụng JSON serialization với compression
    /// </summary>
    public class JsonOfflineQueueStorage : IOfflineQueueStorage
    {
        private readonly string _storageKey;
        private readonly int _maxOfflineQueueSize;
        private readonly RequestConfigCollection _requestConfigCollection;
        
        public JsonOfflineQueueStorage(string storageKey, int maxOfflineQueueSize, 
            RequestConfigCollection requestConfigCollection)
        {
            this._storageKey = storageKey;
            this._maxOfflineQueueSize = maxOfflineQueueSize;
            this._requestConfigCollection = requestConfigCollection;
        }
        
        public async UniTask SaveRequestAsync(QueuedRequest request)
        {
            await UniTask.SwitchToThreadPool();
            
            try
            {
                var existingRequests = await this.LoadRequestsAsync();
                
                if (existingRequests.Count >= this._maxOfflineQueueSize)
                {
                    existingRequests.RemoveAt(0);
                }
                
                existingRequests.Add(request);
                
                await this.SaveRequestsInternalAsync(existingRequests);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }
        
        public async UniTask SaveRequestsAsync(IEnumerable<QueuedRequest> requests)
        {
            await UniTask.SwitchToThreadPool();
            
            try
            {
                var existingRequests = await this.LoadRequestsAsync();
                
                foreach (var request in requests)
                {
                    if (existingRequests.Count >= this._maxOfflineQueueSize)
                    {
                        break;
                    }
                    
                    existingRequests.Add(request);
                }
                
                await this.SaveRequestsInternalAsync(existingRequests);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }
        
        public async UniTask<List<QueuedRequest>> LoadRequestsAsync()
        {
            await UniTask.SwitchToThreadPool();
            
            try
            {
                var json = PlayerPrefs.GetString(this._storageKey, "");
                
                if (string.IsNullOrEmpty(json))
                {
                    return new List<QueuedRequest>();
                }
                
                var wrapper = JsonSerializer.Deserialize<SerializableRequestListWrapper>(json);
                
                if (wrapper == null || wrapper.Requests == null)
                {
                    return new List<QueuedRequest>();
                }
                
                var requests = new List<QueuedRequest>();
                
                foreach (var serializableRequest in wrapper.Requests)
                {
                    var config = this._requestConfigCollection.GetRequestConfigByPriority(serializableRequest.priority);
                    
                    if (config != null)
                    {
                        var request = new QueuedRequest(
                            serializableRequest.endpoint,
                            serializableRequest.jsonBody,
                            serializableRequest.httpMethod,
                            serializableRequest.priority,
                            config,
                            serializableRequest.requestId,
                            serializableRequest.queuedTime,
                            serializableRequest.retryCount
                        );
                        
                        requests.Add(request);
                    }
                }
                
                return requests;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load offline queue: {ex.Message}");
                return new List<QueuedRequest>();
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }
        
        public async UniTask ClearAsync()
        {
            await UniTask.SwitchToThreadPool();
            
            try
            {
                PlayerPrefs.DeleteKey(this._storageKey);
                PlayerPrefs.Save();
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }
        
        public async UniTask<int> GetCountAsync()
        {
            var requests = await this.LoadRequestsAsync();
            return requests.Count;
        }
        
        public bool IsFull
        {
            get
            {
                var json = PlayerPrefs.GetString(this._storageKey, "");
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }
                
                var wrapper = JsonSerializer.Deserialize<SerializableRequestListWrapper>(json);
                return wrapper?.Requests?.Length >= this._maxOfflineQueueSize;
            }
        }
        
        private async UniTask SaveRequestsInternalAsync(List<QueuedRequest> requests)
        {
            await UniTask.SwitchToThreadPool();
            
            try
            {
                var serializableRequests = requests
                    .Select(r => new SerializableRequest(r))
                    .ToArray();
                
                var wrapper = new SerializableRequestListWrapper
                {
                    Requests = serializableRequests
                };
                
                var json = JsonSerializer.SerializeCompact(wrapper);
                
                await UniTask.SwitchToMainThread();
                
                PlayerPrefs.SetString(this._storageKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save offline queue: {ex.Message}");
            }
        }
        
        private class SerializableRequestListWrapper
        {
            public SerializableRequest[] Requests;
        }
    }
}

