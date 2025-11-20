using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// HTTP request sender với retry logic và error handling
    /// </summary>
    public class HttpRequestSender : IRequestSender
    {
        private int _activeRequestsCount;
        private readonly SemaphoreSlim _requestSemaphore;
        
        public HttpRequestSender(int maxConcurrentRequests = 5)
        {
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
                using (var webRequest = new UnityWebRequest(request.endpoint, "POST"))
                {
                    var bodyRaw = Encoding.UTF8.GetBytes(request.jsonBody);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.timeout = 30;
                    
                    await webRequest.SendWebRequest();
                    
                    var statusCode = webRequest.responseCode;
                    
                    if (statusCode == 429)
                    {
                        return RequestResult.RateLimitExceeded();
                    }
                    
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        return RequestResult.Success(webRequest.downloadHandler.text, statusCode);
                    }
                    
                    var errorType = this.DetermineErrorType(webRequest.result, statusCode);
                    return RequestResult.Failure(webRequest.error, statusCode, errorType);
                }
            }
            catch (Exception ex)
            {
                return RequestResult.NetworkError(ex.Message);
            }
        }
        
        private float CalculateRetryDelay(float baseDelay, int retryCount)
        {
            return baseDelay * Mathf.Pow(2, retryCount);
        }
        
        private RequestErrorType DetermineErrorType(UnityWebRequest.Result result, long statusCode)
        {
            if (result == UnityWebRequest.Result.ConnectionError)
            {
                return RequestErrorType.NetworkError;
            }
            
            if (result == UnityWebRequest.Result.DataProcessingError)
            {
                return RequestErrorType.Timeout;
            }
            
            if (statusCode >= 500)
            {
                return RequestErrorType.ServerError;
            }
            
            if (statusCode >= 400)
            {
                return RequestErrorType.ClientError;
            }
            
            return RequestErrorType.Unknown;
        }
    }
}

