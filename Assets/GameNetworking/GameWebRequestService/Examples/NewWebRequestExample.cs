using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Models;
using UnityEngine;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example usage mới của WebRequestService với generic response classes
    /// </summary>
    /// <remarks>
    /// Class này demonstrate API mới:
    /// - Tự động lấy endpoint từ EndpointAttribute
    /// - Không cần truyền headers
    /// - Chỉ cần requestBody và cancellationToken
    /// - Response tự động gọi OnResponseSuccess/OnResponseFailed
    /// </remarks>
    public class NewWebRequestExample : MonoBehaviour
    {
        private GameNetworking.GameWebRequestService.Core.WebRequestService webRequestService;
        private CancellationTokenSource cancellationTokenSource;
        
        private void Start()
        {
            this.InitializeWebRequestService();
        }
        
        /// <summary>
        /// Khởi tạo WebRequestService với config
        /// </summary>
        private void InitializeWebRequestService()
        {
            var config = new WebRequestConfig
            {
                baseUrl = "https://api.example.com",
                defaultTimeoutMs = 30000,
                maxRetries = 3,
                retryDelayMs = 1000,
                useExponentialBackoff = true,
                enableLogging = true,
                logRequestBody = false,
                logResponseBody = true
            };
            
            this.webRequestService = new GameNetworking.GameWebRequestService.Core.WebRequestService(config);
            
            Debug.Log("[NewWebRequestExample] WebRequestService initialized");
        }
        
        /// <summary>
        /// Example GET request - tự động lấy endpoint từ ProfileGetResponse
        /// </summary>
        public async UniTaskVoid ExampleGetProfile()
        {
            try
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                
                // GET có thể có requestBody (optional - truyền null nếu không cần)
                var response = await this.webRequestService.GetAsync<GetProfileRequest, ProfileGetResponse>(
                    requestBody: null, // Hoặc new GetProfileRequest() nếu cần
                    cancellationToken: this.cancellationTokenSource.Token
                );
                
                if (response != null)
                {
                    // Tự động gọi OnResponseSuccess hoặc OnResponseFailed
                    response.ProcessResponse();
                }
                else
                {
                    Debug.LogError("[NewWebRequestExample] GET Failed - response is null");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[NewWebRequestExample] GET Cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewWebRequestExample] GET Exception: {ex.Message}");
            }
            finally
            {
                this.cancellationTokenSource?.Dispose();
                this.cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Example POST request - tự động lấy endpoint từ NewLoginResponse
        /// </summary>
        public async UniTaskVoid ExampleLogin()
        {
            try
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                
                var requestBody = new LoginRequest(
                    username: "testuser",
                    password: "testpassword",
                    deviceId: SystemInfo.deviceUniqueIdentifier
                );
                
                // API mới - chỉ cần requestBody, endpoint tự động lấy từ attribute
                var response = await this.webRequestService.PostAsync<LoginRequest, NewLoginResponse>(
                    requestBody: requestBody,
                    cancellationToken: this.cancellationTokenSource.Token
                );
                
                if (response != null)
                {
                    // Tự động gọi OnResponseSuccess hoặc OnResponseFailed
                    response.ProcessResponse();
                }
                else
                {
                    Debug.LogError("[NewWebRequestExample] POST Failed - response is null");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[NewWebRequestExample] POST Cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewWebRequestExample] POST Exception: {ex.Message}");
            }
            finally
            {
                this.cancellationTokenSource?.Dispose();
                this.cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Example PUT request - tự động lấy endpoint từ ProfileUpdateResponse
        /// </summary>
        public async UniTaskVoid ExampleUpdateProfile()
        {
            try
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                
                var requestBody = new UpdateProfileRequest
                {
                    username = "newusername",
                    email = "newemail@example.com"
                };
                
                // API mới - chỉ cần requestBody, endpoint tự động lấy từ attribute
                var response = await this.webRequestService.PutAsync<UpdateProfileRequest, ProfileUpdateResponse>(
                    requestBody: requestBody,
                    cancellationToken: this.cancellationTokenSource.Token
                );
                
                if (response != null)
                {
                    // Tự động gọi OnResponseSuccess hoặc OnResponseFailed
                    response.ProcessResponse();
                }
                else
                {
                    Debug.LogError("[NewWebRequestExample] PUT Failed - response is null");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[NewWebRequestExample] PUT Cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewWebRequestExample] PUT Exception: {ex.Message}");
            }
            finally
            {
                this.cancellationTokenSource?.Dispose();
                this.cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Example với manual error handling
        /// </summary>
        public async UniTaskVoid ExampleWithManualHandling()
        {
            try
            {
                var response = await this.webRequestService.GetAsync<GetProfileRequest, ProfileGetResponse>(
                    requestBody: null
                );
                
                if (response != null)
                {
                    if (response.IsSuccess && response.data != null)
                    {
                        // Xử lý success manually
                        Debug.Log($"Success: {response.data.username}");
                        response.OnResponseSuccess(response.data);
                    }
                    else
                    {
                        // Xử lý failure manually
                        Debug.LogError($"Failed: {response.Message}");
                        response.OnResponseFailed(response.StatusCode, response.Message);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cancel request đang thực hiện
        /// </summary>
        public void CancelRequest()
        {
            if (this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
                Debug.Log("[NewWebRequestExample] Request cancellation requested");
            }
        }
        
        private void OnDestroy()
        {
            this.CancelRequest();
            this.cancellationTokenSource?.Dispose();
            
            this.webRequestService?.ClearAllResponsePools();
        }
    }
}

