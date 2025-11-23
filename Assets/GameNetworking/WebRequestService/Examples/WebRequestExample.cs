using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.WebRequestService.Models;
using UnityEngine;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example usage của WebRequestService
    /// </summary>
    /// <remarks>
    /// Class này demonstrate cách sử dụng WebRequestService cho các request
    /// GET, POST, PUT với Best HTTP API.
    /// </remarks>
    public class WebRequestExample : MonoBehaviour
    {
        private GameNetworking.WebRequestService.Core.WebRequestService webRequestService;
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
            
            this.webRequestService = new GameNetworking.WebRequestService.Core.WebRequestService(config);
            
            Debug.Log("[WebRequestExample] WebRequestService initialized");
        }
        
        /// <summary>
        /// Example GET request
        /// </summary>
        public async UniTaskVoid ExampleGetRequest()
        {
            try
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer YOUR_TOKEN_HERE" }
                };
                
                var response = await this.webRequestService.GetAsync<LoginRequest, LoginPlainResponse>(
                    cancellationToken: this.cancellationTokenSource.Token
                );
                
                if (response != null && response.IsSuccess)
                {
                    Debug.Log($"[WebRequestExample] GET Success: Token={response.token}");
                    Debug.Log($"[WebRequestExample] User: {response.userData.username}");
                }
                else
                {
                    Debug.LogError("[WebRequestExample] GET Failed");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[WebRequestExample] GET Cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebRequestExample] GET Exception: {ex.Message}");
            }
            finally
            {
                this.cancellationTokenSource?.Dispose();
                this.cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Example POST request (Login)
        /// </summary>
        public async UniTaskVoid ExamplePostRequest()
        {
            try
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                
                var requestBody = new LoginRequest(
                    username: "testuser",
                    password: "testpassword",
                    deviceId: SystemInfo.deviceUniqueIdentifier
                );
                
                var response = await this.webRequestService.PostAsync<LoginRequest, LoginPlainResponse>(
                    requestBody: requestBody,
                    cancellationToken: this.cancellationTokenSource.Token
                );
                
                if (response != null && response.IsSuccess)
                {
                    Debug.Log($"[WebRequestExample] POST Success: Token={response.token}");
                    Debug.Log($"[WebRequestExample] User: {response.userData.username}");
                    
                    PlayerPrefs.SetString("auth_token", response.token);
                    PlayerPrefs.Save();
                }
                else
                {
                    Debug.LogError("[WebRequestExample] POST Failed");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[WebRequestExample] POST Cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebRequestExample] POST Exception: {ex.Message}");
            }
            finally
            {
                this.cancellationTokenSource?.Dispose();
                this.cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Example PUT request (Update profile)
        /// </summary>
        public async UniTaskVoid ExamplePutRequest()
        {
            try
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                
                var requestBody = new UpdateProfileRequest
                {
                    username = "newusername",
                    email = "newemail@example.com"
                };
                
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}" }
                };
                
                var response = await this.webRequestService.PutAsync<UpdateProfileRequest, LoginPlainResponse>(
                    requestBody: requestBody,
                    cancellationToken: this.cancellationTokenSource.Token
                );
                
                if (response != null && response.IsSuccess)
                {
                    Debug.Log("[WebRequestExample] PUT Success: Profile updated");
                    Debug.Log($"[WebRequestExample] New username: {response.userData.username}");
                }
                else
                {
                    Debug.LogError("[WebRequestExample] PUT Failed");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[WebRequestExample] PUT Cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebRequestExample] PUT Exception: {ex.Message}");
            }
            finally
            {
                this.cancellationTokenSource?.Dispose();
                this.cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Example sử dụng object pooling
        /// </summary>
        public async UniTaskVoid ExampleWithObjectPooling()
        {
            try
            {
                Debug.Log($"[WebRequestExample] Pool info before: {this.webRequestService.GetPoolInfo<LoginPlainResponse>()}");
                
                var response = await this.webRequestService.GetAsync<LoginRequest, LoginPlainResponse>();
                
                if (response != null)
                {
                    Debug.Log($"[WebRequestExample] Response received: {response.userData.username}");
                    
                    this.webRequestService.ReturnResponseToPool(response);
                    
                    Debug.Log($"[WebRequestExample] Pool info after: {this.webRequestService.GetPoolInfo<LoginPlainResponse>()}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebRequestExample] Pooling Exception: {ex.Message}");
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
                Debug.Log("[WebRequestExample] Request cancellation requested");
            }
        }
        
        private void OnDestroy()
        {
            this.CancelRequest();
            this.cancellationTokenSource?.Dispose();
            
            this.webRequestService?.ClearAllResponsePools();
        }
    }
    
    /// <summary>
    /// Example update profile request
    /// </summary>
    [System.Serializable]
    public class UpdateProfileRequest
    {
        public string username;
        public string email;
    }
}

