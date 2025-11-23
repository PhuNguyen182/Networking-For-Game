using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.WebRequestService.Constants;
using GameNetworking.WebRequestService.Interfaces;
using GameNetworking.WebRequestService.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace PracticalModules.WebRequestService.Tests
{
    /// <summary>
    /// Mock implementation của IWebRequest cho unit testing
    /// </summary>
    /// <remarks>
    /// Class này cho phép test logic mà không cần gọi API thật.
    /// Hữu ích cho unit tests, integration tests và development.
    /// </remarks>
    public class MockWebRequest : IWebRequest
    {
        private readonly bool simulateSuccess;
        private readonly int simulatedStatusCode;
        private readonly int simulatedDelayMs;
        
        /// <summary>
        /// Khởi tạo MockWebRequest
        /// </summary>
        /// <param name="simulateSuccess">Simulate success response</param>
        /// <param name="simulatedStatusCode">Status code để return</param>
        /// <param name="simulatedDelayMs">Delay để simulate network latency</param>
        public MockWebRequest(
            bool simulateSuccess = true,
            int simulatedStatusCode = HttpStatusCode.Success,
            int simulatedDelayMs = 100
        )
        {
            this.simulateSuccess = simulateSuccess;
            this.simulatedStatusCode = simulatedStatusCode;
            this.simulatedDelayMs = simulatedDelayMs;
        }
        
        /// <summary>
        /// Mock GET request
        /// </summary>
        public async UniTask<TResponse> GetAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
          where TResponse : class
        {
            Debug.Log($"[MockWebRequest] GET {url}");
            if (requestBody != null)
            {
                Debug.Log($"[MockWebRequest] GET Request Body: {JsonConvert.SerializeObject(requestBody)}");
            }
            
            await UniTask.Delay(this.simulatedDelayMs, cancellationToken: cancellationToken);
            
            return this.CreateMockResponse<TResponse>();
        }
        
        /// <summary>
        /// Mock POST request
        /// </summary>
        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
          where TResponse : class
        {
            Debug.Log($"[MockWebRequest] POST {url}");
            Debug.Log($"[MockWebRequest] POST Request Body: {JsonConvert.SerializeObject(requestBody)}");
            
            await UniTask.Delay(this.simulatedDelayMs, cancellationToken: cancellationToken);
            
            return this.CreateMockResponse<TResponse>();
        }
        
        /// <summary>
        /// Mock PUT request
        /// </summary>
        public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
          where TResponse : class
        {
            Debug.Log($"[MockWebRequest] PUT {url}");
            Debug.Log($"[MockWebRequest] PUT Request Body: {JsonConvert.SerializeObject(requestBody)}");
            
            await UniTask.Delay(this.simulatedDelayMs, cancellationToken: cancellationToken);
            
            return this.CreateMockResponse<TResponse>();
        }
        
        /// <summary>
        /// Tạo mock response object
        /// </summary>
        private TResponse CreateMockResponse<TResponse>() where TResponse : class
        {
            if (!this.simulateSuccess)
            {
                Debug.LogWarning($"[MockWebRequest] Simulating failure with status {this.simulatedStatusCode}");
                return null;
            }
            
            try
            {
                var response = Activator.CreateInstance<TResponse>();
                
                if (response is BasePlainResponse baseResponse)
                {
                    baseResponse.statusCode = this.simulatedStatusCode;
                    baseResponse.message = "Mock response";
                    baseResponse.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
                
                Debug.Log($"[MockWebRequest] Created mock response: {typeof(TResponse).Name}");
                
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MockWebRequest] Failed to create mock response: {ex.Message}");
                return null;
            }
        }
    }
}

