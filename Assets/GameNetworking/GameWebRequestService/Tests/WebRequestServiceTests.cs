using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Constants;
using GameNetworking.GameWebRequestService.Models;
using PracticalModules.WebRequestService.Examples;
using UnityEngine;

namespace PracticalModules.WebRequestService.Tests
{
    /// <summary>
    /// Test suite cho WebRequestService
    /// </summary>
    /// <remarks>
    /// Class này demonstrate cách test WebRequestService với MockWebRequest.
    /// Có thể chạy trong Unity Editor hoặc như unit tests.
    /// </remarks>
    public class WebRequestServiceTests : MonoBehaviour
    {
        private GameNetworking.GameWebRequestService.Core.WebRequestService webRequestService;
        
        private void Start()
        {
            this.RunAllTests().Forget();
        }
        
        /// <summary>
        /// Chạy tất cả tests
        /// </summary>
        public async UniTaskVoid RunAllTests()
        {
            Debug.Log("=== Starting WebRequestService Tests ===");
            
            await this.TestSuccessfulGetRequest();
            await this.TestSuccessfulPostRequest();
            await this.TestSuccessfulPutRequest();
            await this.TestFailedRequest();
            await this.TestObjectPooling();
            
            Debug.Log("=== All Tests Completed ===");
        }
        
        /// <summary>
        /// Test GET request thành công
        /// </summary>
        private async UniTask TestSuccessfulGetRequest()
        {
            Debug.Log("[TEST] Starting TestSuccessfulGetRequest");
            
            try
            {
                var config = this.CreateTestConfig();
                var mockRequest = new MockWebRequest(
                    simulateSuccess: true,
                    simulatedStatusCode: HttpStatusCode.Success,
                    simulatedDelayMs: 100
                );
                
                this.webRequestService = new GameNetworking.GameWebRequestService.Core.WebRequestService(config, mockRequest);
                
                var response = await this.webRequestService.GetAsync<LoginRequest, LoginPlainResponse>();
                
                if (response != null && response.IsSuccess)
                {
                    Debug.Log("[TEST] ✅ TestSuccessfulGetRequest PASSED");
                }
                else
                {
                    Debug.LogError("[TEST] ❌ TestSuccessfulGetRequest FAILED: Response is null or failed");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TEST] ❌ TestSuccessfulGetRequest FAILED: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test POST request thành công
        /// </summary>
        private async UniTask TestSuccessfulPostRequest()
        {
            Debug.Log("[TEST] Starting TestSuccessfulPostRequest");
            
            try
            {
                var config = this.CreateTestConfig();
                var mockRequest = new MockWebRequest(simulateSuccess: true);
                
                this.webRequestService = new GameNetworking.GameWebRequestService.Core.WebRequestService(config, mockRequest);
                
                var requestBody = new LoginRequest(
                    username: "testuser",
                    password: "testpass",
                    deviceId: "test-device-123"
                );
                
                var response = await this.webRequestService.PostAsync<LoginRequest, LoginPlainResponse>(
                    requestBody: requestBody
                );
                
                if (response != null && response.IsSuccess)
                {
                    Debug.Log("[TEST] ✅ TestSuccessfulPostRequest PASSED");
                }
                else
                {
                    Debug.LogError("[TEST] ❌ TestSuccessfulPostRequest FAILED: Response is null or failed");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TEST] ❌ TestSuccessfulPostRequest FAILED: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test PUT request thành công
        /// </summary>
        private async UniTask TestSuccessfulPutRequest()
        {
            Debug.Log("[TEST] Starting TestSuccessfulPutRequest");
            
            try
            {
                var config = this.CreateTestConfig();
                var mockRequest = new MockWebRequest(simulateSuccess: true);
                
                this.webRequestService = new GameNetworking.GameWebRequestService.Core.WebRequestService(config, mockRequest);
                
                var requestBody = new LoginRequest(
                    username: "updateduser",
                    password: "newpass",
                    deviceId: "test-device-123"
                );
                
                var response = await this.webRequestService.PutAsync<LoginRequest, LoginPlainResponse>(
                    requestBody: requestBody
                );
                
                if (response != null && response.IsSuccess)
                {
                    Debug.Log("[TEST] ✅ TestSuccessfulPutRequest PASSED");
                }
                else
                {
                    Debug.LogError("[TEST] ❌ TestSuccessfulPutRequest FAILED: Response is null or failed");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TEST] ❌ TestSuccessfulPutRequest FAILED: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test request thất bại
        /// </summary>
        private async UniTask TestFailedRequest()
        {
            Debug.Log("[TEST] Starting TestFailedRequest");
            
            try
            {
                var config = this.CreateTestConfig();
                var mockRequest = new MockWebRequest(
                    simulateSuccess: false,
                    simulatedStatusCode: HttpStatusCode.InternalServerError
                );
                
                this.webRequestService = new GameNetworking.GameWebRequestService.Core.WebRequestService(config, mockRequest);
                
                var response = await this.webRequestService.GetAsync<LoginRequest, LoginPlainResponse>();
                
                if (response == null)
                {
                    Debug.Log("[TEST] ✅ TestFailedRequest PASSED - Correctly returned null for failed request");
                }
                else
                {
                    Debug.LogError("[TEST] ❌ TestFailedRequest FAILED: Should return null for failed request");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TEST] ❌ TestFailedRequest FAILED: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test object pooling functionality
        /// </summary>
        private async UniTask TestObjectPooling()
        {
            Debug.Log("[TEST] Starting TestObjectPooling");
            
            try
            {
                var config = this.CreateTestConfig();
                var mockRequest = new MockWebRequest(simulateSuccess: true);
                
                this.webRequestService = new GameNetworking.GameWebRequestService.Core.WebRequestService(config, mockRequest);
                
                Debug.Log($"[TEST] Pool info before: {this.webRequestService.GetPoolInfo<LoginPlainResponse>()}");
                
                var response1 = await this.webRequestService.GetAsync<LoginRequest, LoginPlainResponse>();
                var response2 = await this.webRequestService.GetAsync<LoginRequest, LoginPlainResponse>();
                
                Debug.Log($"[TEST] Pool info during: {this.webRequestService.GetPoolInfo<LoginPlainResponse>()}");
                
                if (response1 != null && response2 != null)
                {
                    Debug.Log("[TEST] ✅ TestObjectPooling PASSED - Pool created and working");
                }
                else
                {
                    Debug.LogError("[TEST] ❌ TestObjectPooling FAILED: Responses are null");
                }
                
                this.webRequestService.ClearAllResponsePools();
                Debug.Log($"[TEST] Pool info after clear: {this.webRequestService.GetPoolInfo<LoginPlainResponse>()}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TEST] ❌ TestObjectPooling FAILED: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tạo test configuration
        /// </summary>
        private WebRequestConfig CreateTestConfig()
        {
            return new WebRequestConfig
            {
                baseUrl = "https://test-api.example.com",
                defaultTimeoutMs = 5000,
                maxRetries = 2,
                retryDelayMs = 500,
                useExponentialBackoff = false,
                enableLogging = true,
                logRequestBody = true,
                logResponseBody = true
            };
        }
        
        private void OnDestroy()
        {
            this.webRequestService?.ClearAllResponsePools();
        }
    }
}

