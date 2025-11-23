using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameNetworking.RequestOptimizer.Scripts.Interfaces
{
    /// <summary>
    /// Interface cho HTTP client abstraction - tuân thủ Dependency Inversion Principle
    /// Cho phép swap giữa UnityWebRequest, Best HTTP, hoặc mock implementations
    /// </summary>
    public interface IHttpClient : IDisposable
    {
        /// <summary>
        /// Gửi HTTP POST request
        /// </summary>
        /// <param name="url">Endpoint URL</param>
        /// <param name="jsonBody">JSON body content</param>
        /// <param name="headers">Optional custom headers</param>
        /// <param name="timeoutSeconds">Request timeout in seconds</param>
        /// <returns>HTTP response result</returns>
        public UniTask<HttpClientResponse> PostAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30
        );
        
        /// <summary>
        /// Gửi HTTP GET request
        /// </summary>
        public UniTask<HttpClientResponse> GetAsync(
            string url,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30
        );
        
        /// <summary>
        /// Gửi HTTP PUT request
        /// </summary>
        public UniTask<HttpClientResponse> PutAsync(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30
        );
        
        /// <summary>
        /// Gửi HTTP DELETE request
        /// </summary>
        public UniTask<HttpClientResponse> DeleteAsync(
            string url,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30
        );
    }
    
    /// <summary>
    /// HTTP response wrapper - framework-agnostic
    /// </summary>
    public readonly struct HttpClientResponse
    {
        public readonly bool IsSuccess;
        public readonly long StatusCode;
        public readonly string ResponseBody;
        public readonly string ErrorMessage;
        public readonly HttpClientErrorType ErrorType;
        
        public HttpClientResponse(bool isSuccess, long statusCode, string responseBody, 
            string errorMessage = null, HttpClientErrorType errorType = HttpClientErrorType.None)
        {
            this.IsSuccess = isSuccess;
            this.StatusCode = statusCode;
            this.ResponseBody = responseBody;
            this.ErrorMessage = errorMessage;
            this.ErrorType = errorType;
        }
        
        public static HttpClientResponse Success(string responseBody, long statusCode) =>
            new HttpClientResponse(true, statusCode, responseBody);
        
        public static HttpClientResponse Failure(string errorMessage, long statusCode, HttpClientErrorType errorType) =>
            new HttpClientResponse(false, statusCode, null, errorMessage, errorType);
        
        public static HttpClientResponse RateLimited(string errorMessage = "Rate limit exceeded") =>
            new HttpClientResponse(false, 429, null, errorMessage, HttpClientErrorType.RateLimitExceeded);
        
        public static HttpClientResponse NetworkError(string errorMessage) =>
            new HttpClientResponse(false, 0, null, errorMessage, HttpClientErrorType.NetworkError);
        
        public static HttpClientResponse Timeout(string errorMessage = "Request timeout") =>
            new HttpClientResponse(false, 0, null, errorMessage, HttpClientErrorType.Timeout);
    }
    
    /// <summary>
    /// HTTP client error types
    /// </summary>
    public enum HttpClientErrorType
    {
        None,
        NetworkError,
        Timeout,
        RateLimitExceeded,
        ServerError,
        ClientError,
        Unknown
    }
}

