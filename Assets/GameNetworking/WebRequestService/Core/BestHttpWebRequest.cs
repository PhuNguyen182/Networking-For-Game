using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Best.HTTP;
using Best.HTTP.Request.Settings;
using Cysharp.Threading.Tasks;
using GameNetworking.WebRequestService.Attributes;
using GameNetworking.WebRequestService.Constants;
using GameNetworking.WebRequestService.Interfaces;
using GameNetworking.WebRequestService.Models;
using GameNetworking.WebRequestService.Pooling;
using Newtonsoft.Json;
using UnityEngine;

namespace GameNetworking.WebRequestService.Core
{
    /// <summary>
    /// Implementation của IWebRequest sử dụng Best HTTP package
    /// </summary>
    /// <remarks>
    /// Class này implement tất cả các phương thức HTTP cơ bản (GET, POST, PUT)
    /// với Best HTTP API, không sử dụng UnityWebRequest. Tuân thủ SOLID principles
    /// và sử dụng object pooling cho hiệu suất tối ưu.
    /// </remarks>
    public class BestHttpWebRequest : IWebRequest
    {
        private readonly WebRequestConfig _webRequestConfig;
        private readonly ResponsePoolManager _poolManager;

        /// <summary>
        /// Khởi tạo BestHttpWebRequest với config
        /// </summary>
        /// <param name="config">Configuration cho web request</param>
        /// <param name="poolManager">Pool manager cho response objects</param>
        public BestHttpWebRequest(WebRequestConfig config, ResponsePoolManager poolManager = null)
        {
            this._webRequestConfig = config ?? throw new ArgumentNullException(nameof(config));
            this._poolManager = poolManager ?? new ResponsePoolManager();
        }

        /// <summary>
        /// Thực hiện GET request bất đồng bộ
        /// </summary>
        public async UniTask<TResponse> GetAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class
        {
            var fullUrl = this.BuildFullUrl(url);

            if (this._webRequestConfig.enableLogging)
            {
                Debug.Log($"[BestHttpWebRequest] GET {fullUrl}");
                if (requestBody != null && this._webRequestConfig.logRequestBody)
                {
                    Debug.Log($"[BestHttpWebRequest] GET Request Body: {JsonConvert.SerializeObject(requestBody)}");
                }
            }

            try
            {
                var request = new HTTPRequest(new Uri(fullUrl), HTTPMethods.Get);

                // Add request body nếu có
                if (requestBody != null)
                {
                    var jsonBody = this.SerializeRequestBody(requestBody);
                    request.UploadSettings.UploadStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
                    request.SetHeader("Content-Type", "application/json");
                }

                this.SetupRequest(request, headers);

                var response = await this.SendRequestAsync<TResponse>(
                    request,
                    nameof(HTTPMethods.Get),
                    fullUrl,
                    null,
                    cancellationToken
                );

                return response;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[BestHttpWebRequest] GET request cancelled: {fullUrl}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestHttpWebRequest] GET request failed: {fullUrl}\nError: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Thực hiện POST request bất đồng bộ
        /// </summary>
        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class
        {
            var fullUrl = this.BuildFullUrl(url);

            if (this._webRequestConfig.enableLogging)
            {
                Debug.Log($"[BestHttpWebRequest] POST {fullUrl}");
            }

            try
            {
                var request = new HTTPRequest(new Uri(fullUrl), HTTPMethods.Post);

                this.SetupRequest(request, headers);

                var jsonBody = this.SerializeRequestBody(requestBody);

                if (this._webRequestConfig.enableLogging && this._webRequestConfig.logRequestBody)
                {
                    Debug.Log($"[BestHttpWebRequest] POST Body: {jsonBody}");
                }

                request.UploadSettings.UploadStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
                request.SetHeader("Content-Type", "application/json");

                var response = await this.SendRequestAsync<TResponse>(
                    request,
                    nameof(HTTPMethods.Post),
                    fullUrl,
                    jsonBody,
                    cancellationToken
                );

                return response;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[BestHttpWebRequest] POST request cancelled: {fullUrl}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestHttpWebRequest] POST request failed: {fullUrl}\nError: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Thực hiện PUT request bất đồng bộ
        /// </summary>
        public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class
        {
            var fullUrl = this.BuildFullUrl(url);

            if (this._webRequestConfig.enableLogging)
            {
                Debug.Log($"[BestHttpWebRequest] PUT {fullUrl}");
            }

            try
            {
                var request = new HTTPRequest(new Uri(fullUrl), HTTPMethods.Put);

                this.SetupRequest(request, headers);

                var jsonBody = this.SerializeRequestBody(requestBody);

                if (this._webRequestConfig.enableLogging && this._webRequestConfig.logRequestBody)
                {
                    Debug.Log($"[BestHttpWebRequest] PUT Body: {jsonBody}");
                }

                request.UploadSettings.UploadStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
                request.SetHeader("Content-Type", "application/json");

                var response = await this.SendRequestAsync<TResponse>(
                    request,
                    nameof(HTTPMethods.Put),
                    fullUrl,
                    jsonBody,
                    cancellationToken
                );

                return response;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[BestHttpWebRequest] PUT request cancelled: {fullUrl}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestHttpWebRequest] PUT request failed: {fullUrl}\nError: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gửi request và xử lý response
        /// </summary>
        private async UniTask<TResponse> SendRequestAsync<TResponse>(
            HTTPRequest request,
            string method,
            string url,
            string requestBody,
            CancellationToken cancellationToken
        ) where TResponse : class
        {
            var retryCount = 0;
            var maxRetries = this.GetMaxRetries<TResponse>();

            while (retryCount <= maxRetries)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var taskCompletionSource = new UniTaskCompletionSource<HTTPResponse>();

                    request.Callback = (req, resp) =>
                    {
                        if (resp != null)
                        {
                            taskCompletionSource.TrySetResult(resp);
                        }
                        else
                        {
                            taskCompletionSource.TrySetException(
                                new Exception("Response is null")
                            );
                        }
                    };

                    request.Send();

                    var response = await taskCompletionSource.Task;

                    return this.ProcessResponse<TResponse>(response, method, url, requestBody);
                }
                catch (OperationCanceledException)
                {
                    this.LogRequestError(method, url, HttpStatusCode.Cancelled, "Request cancelled by user",
                        requestBody, null);
                    throw;
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount > maxRetries)
                    {
                        this.LogRequestError(method, url, HttpStatusCode.UnknownError, ex.Message, requestBody, null);
                        throw;
                    }

                    var delay = this.CalculateRetryDelay(retryCount);

                    Debug.LogWarning(
                        $"[BestHttpWebRequest] Retry {retryCount}/{maxRetries} after {delay}ms: {method} {url}");

                    await UniTask.Delay(delay, cancellationToken: cancellationToken);
                }
            }

            return null;
        }

        /// <summary>
        /// Xử lý HTTP response và parse data
        /// </summary>
        private TResponse ProcessResponse<TResponse>(
            HTTPResponse response,
            string method,
            string url,
            string requestBody
        ) where TResponse : class
        {
            try
            {
                var statusCode = response.StatusCode;
                var responseText = response.DataAsText;

                if (this._webRequestConfig.enableLogging)
                {
                    Debug.Log($"[BestHttpWebRequest] Response {method} {url}: Status={statusCode}");

                    if (this._webRequestConfig.logResponseBody && !string.IsNullOrEmpty(responseText))
                    {
                        Debug.Log($"[BestHttpWebRequest] Response Body: {responseText}");
                    }
                }

                if (!response.IsSuccess)
                {
                    this.LogRequestError(method, url, statusCode, HttpStatusCode.GetDescription(statusCode),
                        requestBody, responseText);
                    return null;
                }

                if (string.IsNullOrEmpty(responseText))
                {
                    Debug.LogWarning($"[BestHttpWebRequest] Empty response body: {method} {url}");
                    return null;
                }

                var parsedResponse = this.ParseResponse<TResponse>(responseText, statusCode);

                return parsedResponse;
            }
            catch (Exception ex)
            {
                this.LogRequestError(method, url, HttpStatusCode.ParseError, $"Parse error: {ex.Message}", requestBody,
                    response.DataAsText);
                return null;
            }
        }

        /// <summary>
        /// Parse response JSON thành object sử dụng Newtonsoft.Json
        /// </summary>
        private TResponse ParseResponse<TResponse>(string json, int statusCode) where TResponse : class
        {
            try
            {
                var response = JsonConvert.DeserializeObject<TResponse>(json);

                // Support cho cả BaseResponse (legacy) và IBaseResponse (new)
                if (response is IBaseResponse baseResponse)
                {
                    baseResponse.StatusCode = statusCode;
                    baseResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
                else if (response is BasePlainResponse legacyBaseResponse)
                {
                    legacyBaseResponse.statusCode = statusCode;
                    legacyBaseResponse.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestHttpWebRequest] JSON parse error: {ex.Message}\nJSON: {json}");
                throw new Exception($"Failed to parse response: {ex.Message}");
            }
        }

        /// <summary>
        /// Serialize request body thành JSON sử dụng Newtonsoft.Json
        /// </summary>
        private string SerializeRequestBody<TRequest>(TRequest requestBody) where TRequest : class
        {
            if (requestBody == null)
            {
                return "{}";
            }

            try
            {
                return JsonConvert.SerializeObject(requestBody);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestHttpWebRequest] JSON serialize error: {ex.Message}");
                throw new Exception($"Failed to serialize request body: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup request với headers và timeout
        /// </summary>
        private void SetupRequest(HTTPRequest request, Dictionary<string, string> headers)
        {
            var timeout = this.GetTimeout<object>();
            request.TimeoutSettings = new TimeoutSettings(request)
            {
                Timeout = TimeSpan.FromMilliseconds(timeout)
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetHeader(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Build full URL từ base URL và relative path
        /// </summary>
        private string BuildFullUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            }

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return url;
            }

            if (string.IsNullOrEmpty(this._webRequestConfig.baseUrl))
            {
                throw new InvalidOperationException("Base URL is not configured and relative URL provided");
            }

            var baseUrl = this._webRequestConfig.baseUrl.TrimEnd('/');
            var relativePath = url.TrimStart('/');

            return $"{baseUrl}/{relativePath}";
        }

        /// <summary>
        /// Lấy max retries từ EndpointAttribute hoặc config mặc định
        /// </summary>
        private int GetMaxRetries<TResponse>() where TResponse : class
        {
            var endpointAttr = typeof(TResponse).GetCustomAttributes(typeof(EndpointAttribute), true);

            if (endpointAttr.Length > 0 && endpointAttr[0] is EndpointAttribute attr)
            {
                if (!attr.AllowRetry)
                {
                    return 0;
                }

                return attr.MaxRetries;
            }

            return this._webRequestConfig.maxRetries;
        }

        /// <summary>
        /// Lấy timeout từ EndpointAttribute hoặc config mặc định
        /// </summary>
        private int GetTimeout<TResponse>() where TResponse : class
        {
            var endpointAttr = typeof(TResponse).GetCustomAttributes(typeof(EndpointAttribute), true);

            if (endpointAttr.Length > 0 && endpointAttr[0] is EndpointAttribute attr)
            {
                return attr.TimeoutMilliseconds;
            }

            return this._webRequestConfig.defaultTimeoutMs;
        }

        /// <summary>
        /// Tính toán delay cho retry với exponential backoff
        /// </summary>
        private int CalculateRetryDelay(int retryCount)
        {
            if (!this._webRequestConfig.useExponentialBackoff)
            {
                return this._webRequestConfig.retryDelayMs;
            }

            var exponentialDelay = this._webRequestConfig.retryDelayMs * (int)Math.Pow(2, retryCount - 1);
            var maxDelay = this._webRequestConfig.retryDelayMs * 10;

            return Math.Min(exponentialDelay, maxDelay);
        }

        /// <summary>
        /// Log request error với đầy đủ thông tin
        /// </summary>
        private void LogRequestError(
            string method,
            string url,
            int statusCode,
            string errorMessage,
            string requestBody,
            string responseBody
        )
        {
            var errorLog = new StringBuilder();
            errorLog.AppendLine($"[BestHttpWebRequest] Request Failed");
            errorLog.AppendLine($"Method: {method}");
            errorLog.AppendLine($"URL: {url}");
            errorLog.AppendLine($"Status Code: {statusCode}");
            errorLog.AppendLine($"Error: {errorMessage}");
            errorLog.AppendLine($"Description: {HttpStatusCode.GetDescription(statusCode)}");

            if (this._webRequestConfig.logRequestBody && !string.IsNullOrEmpty(requestBody))
            {
                errorLog.AppendLine($"Request Body: {requestBody}");
            }

            if (this._webRequestConfig.logResponseBody && !string.IsNullOrEmpty(responseBody))
            {
                errorLog.AppendLine($"Response Body: {responseBody}");
            }

            Debug.LogError(errorLog.ToString());
        }
    }
}
