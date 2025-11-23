using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Interfaces;
using GameNetworking.GameWebRequestService.Models;
using GameNetworking.GameWebRequestService.Pooling;
using GameNetworking.GameWebRequestService.Utilities;
using UnityEngine;

namespace GameNetworking.GameWebRequestService.Core
{
    /// <summary>
    /// Main service class để thực hiện web requests
    /// </summary>
    /// <remarks>
    /// Class này là facade cho BestHttpWebRequest, cung cấp API đơn giản
    /// và dễ sử dụng. Tuân thủ Facade Pattern và Dependency Injection.
    /// API mới tự động lấy endpoint từ EndpointAttribute của response class.
    /// </remarks>
    public class WebRequestService
    {
        private readonly IWebRequest _webRequest;
        private readonly ResponsePoolManager _poolManager;
        private readonly WebRequestConfig _requestConfig;

        /// <summary>
        /// Khởi tạo WebRequestService với config
        /// </summary>
        /// <param name="requestConfig">Configuration cho web request</param>
        public WebRequestService(WebRequestConfig requestConfig)
        {
            this._requestConfig = requestConfig;
            this._poolManager = new ResponsePoolManager(initialCapacity: 10, maxCapacity: 100);
            this._webRequest = new BestHttpWebRequest(requestConfig, this._poolManager);

            Debug.Log("[WebRequestService] Initialized with Best HTTP");
        }

        /// <summary>
        /// Khởi tạo với custom IWebRequest implementation (for testing)
        /// </summary>
        /// <param name="requestConfig">Configuration</param>
        /// <param name="webRequest">Custom web request implementation</param>
        public WebRequestService(WebRequestConfig requestConfig, IWebRequest webRequest)
        {
            this._requestConfig = requestConfig;
            this._poolManager = new ResponsePoolManager();
            this._webRequest = webRequest;

            Debug.Log("[WebRequestService] Initialized with custom IWebRequest");
        }

        /// <summary>
        /// Thực hiện GET request
        /// </summary>
        /// <typeparam name="TRequest">Kiểu request body</typeparam>
        /// <typeparam name="TResponse">Kiểu response (phải có EndpointAttribute)</typeparam>
        /// <param name="requestBody">Request body data (optional, có thể null)</param>
        /// <param name="cancellationToken">Cancellation token (optional)</param>
        /// <returns>Response data hoặc null nếu thất bại</returns>
        /// <exception cref="InvalidOperationException">Nếu TResponse không có EndpointAttribute</exception>
        public async UniTask<TResponse> GetAsync<TRequest, TResponse>(
            TRequest requestBody = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class
        {
            EndpointHelper.ValidateEndpointAttribute<TResponse>();

            var url = EndpointHelper.GetEndpointPath<TResponse>();

            return await this._webRequest.GetAsync<TRequest, TResponse>(url, requestBody, null, cancellationToken);
        }

        /// <summary>
        /// Thực hiện POST request
        /// </summary>
        /// <typeparam name="TRequest">Kiểu request body</typeparam>
        /// <typeparam name="TResponse">Kiểu response (phải có EndpointAttribute)</typeparam>
        /// <param name="requestBody">Request body data</param>
        /// <param name="cancellationToken">Cancellation token (optional)</param>
        /// <returns>Response data hoặc null nếu thất bại</returns>
        /// <exception cref="InvalidOperationException">Nếu TResponse không có EndpointAttribute</exception>
        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            TRequest requestBody,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class
        {
            EndpointHelper.ValidateEndpointAttribute<TResponse>();

            var url = EndpointHelper.GetEndpointPath<TResponse>();

            return await this._webRequest.PostAsync<TRequest, TResponse>(url, requestBody, null, cancellationToken);
        }

        /// <summary>
        /// Thực hiện PUT request
        /// </summary>
        /// <typeparam name="TRequest">Kiểu request body</typeparam>
        /// <typeparam name="TResponse">Kiểu response (phải có EndpointAttribute)</typeparam>
        /// <param name="requestBody">Request body data</param>
        /// <param name="cancellationToken">Cancellation token (optional)</param>
        /// <returns>Response data hoặc null nếu thất bại</returns>
        /// <exception cref="InvalidOperationException">Nếu TResponse không có EndpointAttribute</exception>
        public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
            TRequest requestBody,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class
        {
            EndpointHelper.ValidateEndpointAttribute<TResponse>();

            var url = EndpointHelper.GetEndpointPath<TResponse>();

            return await this._webRequest.PutAsync<TRequest, TResponse>(url, requestBody, null, cancellationToken);
        }

        /// <summary>
        /// Lấy response object từ pool
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>Response object từ pool</returns>
        public TResponse GetResponseFromPool<TResponse>() where TResponse : class, IPoolable
        {
            return this._poolManager.Get<TResponse>();
        }

        /// <summary>
        /// Trả response object về pool
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <param name="response">Response object cần trả về pool</param>
        public void ReturnResponseToPool<TResponse>(TResponse response) where TResponse : class, IPoolable
        {
            this._poolManager.Return(response);
        }

        /// <summary>
        /// Clear pool cho một kiểu response cụ thể
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        public void ClearResponsePool<TResponse>() where TResponse : class, IPoolable
        {
            this._poolManager.ClearPool<TResponse>();
        }

        /// <summary>
        /// Clear tất cả response pools
        /// </summary>
        public void ClearAllResponsePools()
        {
            this._poolManager.ClearAllPools();
        }

        /// <summary>
        /// Lấy thông tin pool cho debugging
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>String chứa thông tin pool</returns>
        public string GetPoolInfo<TResponse>() where TResponse : class, IPoolable
        {
            return this._poolManager.GetPoolInfo<TResponse>();
        }

        /// <summary>
        /// Log thông tin tất cả pools
        /// </summary>
        public void LogAllPoolsInfo()
        {
            this._poolManager.LogAllPoolsInfo();
        }
    }
}
