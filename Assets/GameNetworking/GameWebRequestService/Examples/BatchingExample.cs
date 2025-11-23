using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Core;
using GameNetworking.GameWebRequestService.Models;
using GameNetworking.OnlineChecking;
using GameNetworking.RequestOptimizer.Scripts;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using Newtonsoft.Json;
using UnityEngine;

namespace GameNetworking.GameWebRequestService.Examples
{
    /// <summary>
    /// Example demonstrating automatic batching cho GET, POST, PUT requests
    /// </summary>
    public class BatchingExample : MonoBehaviour
    {
        [Header("Service Configuration")] 
        [SerializeField] private WebRequestConfig webRequestConfig;
        [SerializeField] private RequestQueueManagerConfig queueManagerConfig;
        [SerializeField] private RequestConfigCollection requestConfigCollection;
        [SerializeField] private OnlineCheckService onlineCheckService;
        
        private OptimizedWebRequestService _optimizedService;
        
        private async void Start()
        {
            // Initialize OptimizedWebRequestService
            this._optimizedService = new OptimizedWebRequestService(
                this.webRequestConfig,
                this.queueManagerConfig,
                this.requestConfigCollection,
                this.onlineCheckService
            );
            
            await this._optimizedService.StartAsync();
            
            Debug.Log("[BatchingExample] Service started, ready for batching demonstration");
            
            // Wait 2 seconds để service ổn định
            await UniTask.Delay(2000);
            
            // Demonstrate batching
            await this.DemonstrateBatching();
        }
        
        /// <summary>
        /// Demonstrate batching với spam requests
        /// Tất cả requests sẽ được batch tự động và trả về đúng callback
        /// </summary>
        private async UniTask DemonstrateBatching()
        {
            Debug.Log("=== Starting Batching Demonstration ===");
            
            // Test 1: Spam GET requests (sẽ được batch nếu priority hỗ trợ)
            Debug.Log("\n--- Test 1: Batching GET Requests ---");
            await this.BatchGetRequestsTest();
            
            await UniTask.Delay(3000); // Wait for batch to complete
            
            // Test 2: Spam POST requests
            Debug.Log("\n--- Test 2: Batching POST Requests ---");
            await this.BatchPostRequestsTest();
            
            await UniTask.Delay(3000);
            
            // Test 3: Spam PUT requests
            Debug.Log("\n--- Test 3: Batching PUT Requests ---");
            await this.BatchPutRequestsTest();
            
            await UniTask.Delay(3000);
            
            // Test 4: Mixed priority requests
            Debug.Log("\n--- Test 4: Mixed Priority Batching ---");
            await this.MixedPriorityBatchingTest();
            
            Debug.Log("\n=== Batching Demonstration Complete ===");
        }
        
        private async UniTask BatchGetRequestsTest()
        {
            // Spam 10 GET requests cùng lúc
            var tasks = new UniTask[10];
            
            for (int i = 0; i < 10; i++)
            {
                var playerId = 1000 + i;
                tasks[i] = this.SendPlayerDataGetRequest(playerId);
            }
            
            await UniTask.WhenAll(tasks);
            
            Debug.Log("[BatchingExample] All GET requests completed!");
        }
        
        private async UniTask BatchPostRequestsTest()
        {
            // Spam 10 POST requests cùng lúc
            var tasks = new UniTask[10];
            
            for (int i = 0; i < 10; i++)
            {
                var playerId = 2000 + i;
                tasks[i] = this.SendAnalyticsPostRequest(playerId);
            }
            
            await UniTask.WhenAll(tasks);
            
            Debug.Log("[BatchingExample] All POST requests completed!");
        }
        
        private async UniTask BatchPutRequestsTest()
        {
            // Spam 10 PUT requests cùng lúc
            var tasks = new UniTask[10];
            
            for (int i = 0; i < 10; i++)
            {
                var playerId = 3000 + i;
                tasks[i] = this.SendProfileUpdateRequest(playerId);
            }
            
            await UniTask.WhenAll(tasks);
            
            Debug.Log("[BatchingExample] All PUT requests completed!");
        }
        
        private async UniTask MixedPriorityBatchingTest()
        {
            // Mix các requests với different priorities
            var tasks = new UniTask[]
            {
                this.SendPlayerDataGetRequest(4001), // Normal priority
                this.SendAnalyticsPostRequest(4002), // Batch priority
                this.SendProfileUpdateRequest(4003), // Normal priority
                this.SendPlayerDataGetRequest(4004), // Normal priority
                this.SendAnalyticsPostRequest(4005), // Batch priority
            };
            
            await UniTask.WhenAll(tasks);
            
            Debug.Log("[BatchingExample] Mixed priority requests completed!");
        }
        
        // === Individual Request Methods ===
        
        private async UniTask SendPlayerDataGetRequest(int playerId)
        {
            var requestBody = new PlayerDataRequest { playerId = playerId };
            
            try
            {
                var response = await this._optimizedService.GetAsync<PlayerDataRequest, PlayerDataGetResponse>(
                    requestBody
                );
                
                Debug.Log($"[GET] Player {playerId} request completed with status: {response.StatusCode}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GET] Player {playerId} request failed: {ex.Message}");
            }
        }
        
        private async UniTask SendAnalyticsPostRequest(int playerId)
        {
            var requestBody = new AnalyticsEventRequest
            {
                playerId = playerId,
                eventName = "test_event",
                eventData = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "timestamp", System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
                }
            };
            
            try
            {
                var response = await this._optimizedService.PostAsync<AnalyticsEventRequest, AnalyticsPostResponse>(
                    requestBody
                );
                
                Debug.Log($"[POST] Analytics for player {playerId} completed with status: {response.StatusCode}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[POST] Analytics for player {playerId} failed: {ex.Message}");
            }
        }
        
        private async UniTask SendProfileUpdateRequest(int playerId)
        {
            var requestBody = new ProfileUpdateRequest
            {
                playerId = playerId,
                displayName = $"Player_{playerId}",
                level = Random.Range(1, 100)
            };
            
            try
            {
                var response = await this._optimizedService.PutAsync<ProfileUpdateRequest, ProfileUpdatePutResponse>(
                    requestBody
                );
                
                Debug.Log($"[PUT] Profile update for player {playerId} completed with status: {response.StatusCode}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PUT] Profile update for player {playerId} failed: {ex.Message}");
            }
        }
        
        private void OnDestroy()
        {
            this._optimizedService?.Dispose();
        }
    }
    
    // === Request/Response Models ===
    
    [System.Serializable]
    public class PlayerDataRequest
    {
        public int playerId;
    }
    
    [System.Serializable]
    public class PlayerDataResponseData
    {
        public int playerId;
        public string playerName;
        public int level;
        public float experience;
    }
    
    [Endpoint("/api/players/data", "GetPlayerData", RequestPriority.Normal)]
    public class PlayerDataGetResponse : BaseGetResponse<PlayerDataResponseData>
    {
        public override void OnResponseSuccess(PlayerDataResponseData result)
        {
            Debug.Log($"[PlayerDataGetResponse] SUCCESS - Player: {result.playerName}, Level: {result.level}");
        }
        
        public override void OnResponseFailed(int errorCode, string message)
        {
            Debug.LogError($"[PlayerDataGetResponse] FAILED - Code: {errorCode}, Message: {message}");
        }
    }
    
    [System.Serializable]
    public class AnalyticsEventRequest
    {
        public int playerId;
        public string eventName;
        public System.Collections.Generic.Dictionary<string, object> eventData;
    }
    
    [System.Serializable]
    public class AnalyticsEventResponseData
    {
        public string eventId;
        public bool recorded;
    }
    
    [Endpoint("/api/analytics/event", "RecordAnalyticsEvent", RequestPriority.Batch)]
    public class AnalyticsPostResponse : BasePostResponse<AnalyticsEventResponseData>
    {
        public override void OnResponseSuccess(AnalyticsEventResponseData result)
        {
            Debug.Log($"[AnalyticsPostResponse] SUCCESS - Event ID: {result.eventId}, Recorded: {result.recorded}");
        }
        
        public override void OnResponseFailed(int errorCode, string message)
        {
            Debug.LogError($"[AnalyticsPostResponse] FAILED - Code: {errorCode}, Message: {message}");
        }
    }
    
    [System.Serializable]
    public class ProfileUpdateRequest
    {
        public int playerId;
        public string displayName;
        public int level;
    }
    
    [System.Serializable]
    public class ProfileUpdateResponseData
    {
        public int playerId;
        public string displayName;
        public int level;
        public bool updated;
    }
    
    [Endpoint("/api/players/profile", "UpdatePlayerProfile", RequestPriority.Normal)]
    public class ProfileUpdatePutResponse : BasePutResponse<ProfileUpdateResponseData>
    {
        public override void OnResponseSuccess(ProfileUpdateResponseData result)
        {
            Debug.Log($"[ProfileUpdatePutResponse] SUCCESS - Player: {result.displayName}, Updated: {result.updated}");
        }
        
        public override void OnResponseFailed(int errorCode, string message)
        {
            Debug.LogError($"[ProfileUpdatePutResponse] FAILED - Code: {errorCode}, Message: {message}");
        }
    }
}

