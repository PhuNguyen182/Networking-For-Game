using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Core;
using GameNetworking.GameWebRequestService.Interfaces;
using GameNetworking.GameWebRequestService.Models;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;

namespace GameNetworking.RequestOptimizer.Scripts.Adapters
{
    /// <summary>
    /// Adapter để tích hợp GameWebRequestService vào RequestOptimizer
    /// Implement IHttpClient interface để RequestOptimizer có thể sử dụng GameWebRequestService
    /// Tuân thủ Adapter Pattern và Dependency Inversion Principle
    /// </summary>
    public class GameWebRequestAdapter : IHttpClient
    {
        private readonly IWebRequest _webRequestService;
        private bool _isDisposed;
        
        public GameWebRequestAdapter(IWebRequest webRequestService)
        {
            this._webRequestService = webRequestService ?? throw new ArgumentNullException(nameof(webRequestService));
        }
        
        /// <summary>
        /// Factory method để tạo adapter với config mặc định
        /// </summary>
        public static GameWebRequestAdapter CreateDefault(WebRequestConfig config = null)
        {
            config ??= new WebRequestConfig();
            var webRequestService = new BestHttpWebRequest(config);
            return new GameWebRequestAdapter(webRequestService);
        }
        
        public async UniTask<HttpClientResponse> PostAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            try
            {
                // GameWebRequestService sử dụng generic types, nhưng RequestOptimizer chỉ cần string
                // Dùng BasePlainResponse để handle response as plain string
                var response = await this._webRequestService.PostAsync<StringRequest, BasePlainResponse>(
                    url,
                    new StringRequest { jsonBody = jsonBody },
                    headers,
                    CancellationToken.None
                );
                
                return this.ConvertToHttpClientResponse(response);
            }
            catch (OperationCanceledException)
            {
                return HttpClientResponse.Timeout("Request cancelled");
            }
            catch (Exception ex)
            {
                return HttpClientResponse.NetworkError(ex.Message);
            }
        }
        
        public async UniTask<HttpClientResponse> GetAsync(
            string url,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            try
            {
                var response = await this._webRequestService.GetAsync<object, BasePlainResponse>(
                    url,
                    null,
                    headers,
                    CancellationToken.None
                );
                
                return this.ConvertToHttpClientResponse(response);
            }
            catch (OperationCanceledException)
            {
                return HttpClientResponse.Timeout("Request cancelled");
            }
            catch (Exception ex)
            {
                return HttpClientResponse.NetworkError(ex.Message);
            }
        }
        
        public async UniTask<HttpClientResponse> PutAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            try
            {
                var response = await this._webRequestService.PutAsync<StringRequest, BasePlainResponse>(
                    url,
                    new StringRequest { jsonBody = jsonBody },
                    headers,
                    CancellationToken.None
                );
                
                return this.ConvertToHttpClientResponse(response);
            }
            catch (OperationCanceledException)
            {
                return HttpClientResponse.Timeout("Request cancelled");
            }
            catch (Exception ex)
            {
                return HttpClientResponse.NetworkError(ex.Message);
            }
        }
        
        public async UniTask<HttpClientResponse> DeleteAsync(
            string url,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            try
            {
                // GameWebRequestService chưa có DeleteAsync, fallback to custom implementation
                // Hoặc có thể extend IWebRequest để thêm DeleteAsync
                return HttpClientResponse.Failure("DELETE not implemented", 501, HttpClientErrorType.Unknown);
            }
            catch (Exception ex)
            {
                return HttpClientResponse.NetworkError(ex.Message);
            }
        }
        
        private HttpClientResponse ConvertToHttpClientResponse(BasePlainResponse response)
        {
            if (response == null)
            {
                return HttpClientResponse.NetworkError("Null response");
            }
            
            var statusCode = response.statusCode;
            var isSuccess = statusCode >= 200 && statusCode < 300;
            
            if (isSuccess)
            {
                return HttpClientResponse.Success(response.message, statusCode);
            }
            
            var errorType = this.DetermineErrorType(statusCode);
            
            if (statusCode == 429)
            {
                return HttpClientResponse.RateLimited(response.message);
            }
            
            return HttpClientResponse.Failure(
                response.message ?? "Unknown error",
                statusCode,
                errorType
            );
        }
        
        private HttpClientErrorType DetermineErrorType(long statusCode)
        {
            if (statusCode == 429)
            {
                return HttpClientErrorType.RateLimitExceeded;
            }
            
            if (statusCode >= 500)
            {
                return HttpClientErrorType.ServerError;
            }
            
            if (statusCode >= 400)
            {
                return HttpClientErrorType.ClientError;
            }
            
            return HttpClientErrorType.Unknown;
        }
        
        public void Dispose()
        {
            if (this._isDisposed)
            {
                return;
            }
            
            // IWebRequest không có Dispose, không cần cleanup
            this._isDisposed = true;
        }
        
        /// <summary>
        /// Simple request wrapper cho string JSON body
        /// </summary>
        private class StringRequest
        {
            public string jsonBody;
        }
    }
}

