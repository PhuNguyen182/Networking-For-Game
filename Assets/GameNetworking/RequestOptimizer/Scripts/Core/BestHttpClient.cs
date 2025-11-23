using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Best.HTTP;
using Best.HTTP.Request.Settings;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Best HTTP implementation của IHttpClient interface
    /// Tối ưu performance với Best HTTP v3.x API
    /// </summary>
    public class BestHttpClient : IHttpClient
    {
        private readonly Dictionary<string, string> _defaultHeaders;
        private bool _isDisposed;
        
        public BestHttpClient(Dictionary<string, string> defaultHeaders = null)
        {
            this._defaultHeaders = defaultHeaders ?? new Dictionary<string, string>();
        }
        
        public async UniTask<HttpClientResponse> PostAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            return await this.SendRequestAsync(HTTPMethods.Post, url, jsonBody, headers, timeoutSeconds);
        }
        
        public async UniTask<HttpClientResponse> GetAsync(
            string url,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            return await this.SendRequestAsync(HTTPMethods.Get, url, null, headers, timeoutSeconds);
        }
        
        public async UniTask<HttpClientResponse> PutAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            return await this.SendRequestAsync(HTTPMethods.Put, url, jsonBody, headers, timeoutSeconds);
        }
        
        public async UniTask<HttpClientResponse> DeleteAsync(
            string url,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            return await this.SendRequestAsync(HTTPMethods.Delete, url, null, headers, timeoutSeconds);
        }
        
        private async UniTask<HttpClientResponse> SendRequestAsync(
            HTTPMethods method,
            string url,
            string jsonBody,
            Dictionary<string, string> customHeaders,
            int timeoutSeconds)
        {
            try
            {
                var request = new HTTPRequest(new Uri(url), method);
                
                // Set timeout
                request.TimeoutSettings = new TimeoutSettings(request)
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                };
                
                // Apply default headers
                foreach (var header in this._defaultHeaders)
                {
                    request.SetHeader(header.Key, header.Value);
                }
                
                // Apply custom headers (override defaults)
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        request.SetHeader(header.Key, header.Value);
                    }
                }
                
                // Set content type for methods with body
                if (method == HTTPMethods.Post || method == HTTPMethods.Put)
                {
                    request.SetHeader("Content-Type", "application/json");
                    
                    if (!string.IsNullOrEmpty(jsonBody))
                    {
                        // Best HTTP v3.x API: Use UploadSettings.UploadStream
                        var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                        request.UploadSettings.UploadStream = new MemoryStream(bodyBytes);
                    }
                }
                
                // Send request and await response
                var response = await request.GetHTTPResponseAsync();
                
                // Check for null response (network error, timeout, etc.)
                if (response == null)
                {
                    return HttpClientResponse.NetworkError("No response received from server");
                }
                
                var statusCode = response.StatusCode;
                
                // Handle rate limiting
                if (statusCode == 429)
                {
                    var errorMessage = response.DataAsText ?? "Rate limit exceeded";
                    return HttpClientResponse.RateLimited(errorMessage);
                }
                
                // Handle success (2xx status codes)
                if (response.IsSuccess)
                {
                    var responseBody = response.DataAsText ?? string.Empty;
                    return HttpClientResponse.Success(responseBody, statusCode);
                }
                
                // Handle errors
                var errorType = this.DetermineErrorType(statusCode);
                var errorMsg = response.DataAsText ?? response.Message ?? $"HTTP {statusCode} error";
                
                return HttpClientResponse.Failure(errorMsg, statusCode, errorType);
            }
            catch (TimeoutException)
            {
                return HttpClientResponse.Timeout($"Request timeout after {timeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestHttpClient] Exception: {ex.Message}\n{ex.StackTrace}");
                return HttpClientResponse.NetworkError($"Request failed: {ex.Message}");
            }
        }
        
        private HttpClientErrorType DetermineErrorType(int statusCode)
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
            
            this._defaultHeaders?.Clear();
            this._isDisposed = true;
        }
    }
}

