using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// HTTP request sender với retry logic và error handling
    /// Sử dụng IHttpClient abstraction để decouple từ concrete HTTP implementation
    /// </summary>
    public class HttpRequestSender : IRequestSender
    {
        private readonly IHttpClient _httpClient;
        private readonly SemaphoreSlim _requestSemaphore;
        private int _activeRequestsCount;
        
        public HttpRequestSender(IHttpClient httpClient, int maxConcurrentRequests = 5)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._requestSemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            this._activeRequestsCount = 0;
        }
        
        public async UniTask<RequestResult> SendRequestAsync(QueuedRequest request)
        {
            await this._requestSemaphore.WaitAsync();
            
            try
            {
                this._activeRequestsCount++;
                
                var result = await this.SendHttpRequestAsync(request, request.config.maxRetries);
                
                return result;
            }
            finally
            {
                this._activeRequestsCount--;
                this._requestSemaphore.Release();
            }
        }
        
        public async UniTask<RequestResult> SendRequestImmediateAsync(QueuedRequest request)
        {
            this._activeRequestsCount++;
            
            try
            {
                var result = await this.SendHttpRequestAsync(request, 0);
                return result;
            }
            finally
            {
                this._activeRequestsCount--;
            }
        }
        
        public int ActiveRequestsCount => this._activeRequestsCount;
        
        private async UniTask<RequestResult> SendHttpRequestAsync(QueuedRequest request, int maxRetries)
        {
            var retryCount = 0;
            
            while (retryCount <= maxRetries)
            {
                var result = await this.ExecuteHttpRequestAsync(request);
                
                if (result.IsSuccess)
                {
                    return result;
                }
                
                if (result.ErrorType == RequestErrorType.RateLimitExceeded)
                {
                    return result;
                }
                
                if (retryCount < maxRetries)
                {
                    var delay = this.CalculateRetryDelay(request.config.retryDelay, retryCount);
                    await UniTask.Delay(TimeSpan.FromSeconds(delay));
                    retryCount++;
                }
                else
                {
                    return result;
                }
            }
            
            return RequestResult.Failure("Max retries exceeded", 0, RequestErrorType.Unknown);
        }
        
        private async UniTask<RequestResult> ExecuteHttpRequestAsync(QueuedRequest request)
        {
            try
            {
                // Sử dụng correct HTTP method từ QueuedRequest
                HttpClientResponse httpResponse;
                
                var httpMethod = request.httpMethod?.ToUpper() ?? "POST";
                
                switch (httpMethod)
                {
                    case "GET":
                    {
                        httpResponse = await this._httpClient.GetAsync(
                            url: request.endpoint,
                            headers: null,
                            timeoutSeconds: 30
                        );
                        break;
                    }
                    case "POST":
                    {
                        httpResponse = await this._httpClient.PostAsync(
                            url: request.endpoint,
                            jsonBody: request.jsonBody,
                            headers: null,
                            timeoutSeconds: 30
                        );
                        break;
                    }
                    case "PUT":
                    {
                        httpResponse = await this._httpClient.PutAsync(
                            url: request.endpoint,
                            jsonBody: request.jsonBody,
                            headers: null,
                            timeoutSeconds: 30
                        );
                        break;
                    }
                    case "DELETE":
                    {
                        httpResponse = await this._httpClient.DeleteAsync(
                            url: request.endpoint,
                            headers: null,
                            timeoutSeconds: 30
                        );
                        break;
                    }
                    default:
                    {
                        Debug.LogWarning($"[HttpRequestSender] Unknown HTTP method: {httpMethod}, defaulting to POST");
                        httpResponse = await this._httpClient.PostAsync(
                            url: request.endpoint,
                            jsonBody: request.jsonBody,
                            headers: null,
                            timeoutSeconds: 30
                        );
                        break;
                    }
                }
                
                // Convert HttpClientResponse sang RequestResult
                if (httpResponse.IsSuccess)
                {
                    return RequestResult.Success(httpResponse.ResponseBody, httpResponse.StatusCode);
                }
                
                // Handle specific error types
                var requestErrorType = this.MapHttpErrorToRequestError(httpResponse.ErrorType);
                
                if (httpResponse.StatusCode == 429 || requestErrorType == RequestErrorType.RateLimitExceeded)
                {
                    return RequestResult.RateLimitExceeded();
                }
                
                return RequestResult.Failure(
                    httpResponse.ErrorMessage ?? "Unknown error",
                    httpResponse.StatusCode,
                    requestErrorType
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpRequestSender] Exception: {ex.Message}");
                return RequestResult.NetworkError(ex.Message);
            }
        }
        
        private float CalculateRetryDelay(float baseDelay, int retryCount)
        {
            return baseDelay * Mathf.Pow(2, retryCount);
        }
        
        private RequestErrorType MapHttpErrorToRequestError(HttpClientErrorType httpErrorType)
        {
            return httpErrorType switch
            {
                HttpClientErrorType.NetworkError => RequestErrorType.NetworkError,
                HttpClientErrorType.Timeout => RequestErrorType.Timeout,
                HttpClientErrorType.RateLimitExceeded => RequestErrorType.RateLimitExceeded,
                HttpClientErrorType.ServerError => RequestErrorType.ServerError,
                HttpClientErrorType.ClientError => RequestErrorType.ClientError,
                _ => RequestErrorType.Unknown
            };
        }
    }
}

