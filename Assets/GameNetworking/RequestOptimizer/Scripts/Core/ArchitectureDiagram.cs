/*
 * ═══════════════════════════════════════════════════════════════════════════
 *                    REQUEST OPTIMIZER ARCHITECTURE DIAGRAM
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * LAYER 1: Unity Integration (MonoBehaviour Adapter)
 * ┌─────────────────────────────────────────────────────────────────────────┐
 * │                   RequestQueueManagerBehaviour                          │
 * │  - Singleton pattern cho easy access                                    │
 * │  - Khởi tạo tất cả dependencies                                         │
 * │  - Connect với Unity lifecycle (Awake, Start, OnDestroy)                │
 * │  - Event handling cho application pause/quit                            │
 * └──────────────────────────────┬──────────────────────────────────────────┘
 *                                │
 *                                │ Dependency Injection
 *                                ▼
 * ┌─────────────────────────────────────────────────────────────────────────┐
 * │                      RequestQueueManager (Core)                         │
 * │  ┌──────────────────────────────────────────────────────────────────┐  │
 * │  │  Main Responsibilities:                                           │  │
 * │  │  - Điều phối request processing                                   │  │
 * │  │  - Quản lý lifecycle của processing loops                         │  │
 * │  │  - Event-driven network status handling                           │  │
 * │  │  - Statistics aggregation và reporting                            │  │
 * │  └──────────────────────────────────────────────────────────────────┘  │
 * │                                                                          │
 * │  Processing Loops (UniTask):                                            │
 * │  ┌────────────────┐  ┌────────────────┐  ┌─────────────────┐          │
 * │  │ Queue Loop     │  │ Batch Loop     │  │ Rate Limiter    │          │
 * │  │ - Dequeue      │  │ - Check ready  │  │ - Update windows│          │
 * │  │ - Rate check   │  │ - Send batches │  │ - Clean old TS  │          │
 * │  │ - Send request │  │ - Distribute   │  │ - Emit stats    │          │
 * │  └────────────────┘  └────────────────┘  └─────────────────┘          │
 * └────┬──────┬──────┬──────┬──────┬──────┬─────────────────────────────┘
 *      │      │      │      │      │      │
 *      │      │      │      │      │      └─────────────────────┐
 *      ▼      ▼      ▼      ▼      ▼      ▼                     ▼
 * ┌────────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌─────────────────────────────┐
 * │IRequest│ │IRat│ │INet│ │IReq│ │IOffl│ │   IBatchingStrategy         │
 * │Queue   │ │eLim│ │work│ │uest│ │ineQ│ │   (Strategy Pattern)        │
 * │        │ │iter│ │Mon │ │Send│ │ueue│ │                             │
 * └───┬────┘ └─┬──┘ └─┬──┘ └─┬──┘ └─┬──┘ └──┬───────┬───────┬───────┬──┘
 *     │        │      │      │      │       │       │       │       │
 *     │        │      │      │      │       │       │       │       │
 * ┌───▼────────▼──────▼──────▼──────▼───────▼───────▼───────▼───────▼───┐
 * │                    CONCRETE IMPLEMENTATIONS                           │
 * │                                                                        │
 * │  ┌──────────────────┐  ┌──────────────┐  ┌─────────────────────┐   │
 * │  │PriorityRequest   │  │ RateLimiter  │  │ NetworkMonitor      │   │
 * │  │Queue             │  │              │  │                     │   │
 * │  │                  │  │ - Per-second │  │ - Health checks     │   │
 * │  │ - 5 Priority     │  │ - Per-minute │  │ - Auto reconnect    │   │
 * │  │   levels         │  │ - Sliding    │  │ - Event-driven      │   │
 * │  │ - Auto drop low  │  │   window     │  │                     │   │
 * │  │ - O(1) ops       │  │ - Cooldown   │  │                     │   │
 * │  └──────────────────┘  └──────────────┘  └─────────────────────┘   │
 * │                                                                        │
 * │  ┌──────────────────┐  ┌──────────────────────────────────────────┐ │
 * │  │HttpRequestSender │  │ JsonOfflineQueueStorage                  │ │
 * │  │                  │  │                                          │ │
 * │  │ - Semaphore      │  │ - Async serialization                   │ │
 * │  │   concurrency    │  │ - Thread-safe                           │ │
 * │  │ - Retry logic    │  │ - Max size enforcement                  │ │
 * │  │ - Error typing   │  │                                          │ │
 * │  └──────────────────┘  └──────────────────────────────────────────┘ │
 * │                                                                        │
 * │  ┌────────────────────────────────────────────────────────────────┐  │
 * │  │              Batching Strategies (4 Types)                      │  │
 * │  │                                                                 │  │
 * │  │  ┌─────────────┐  ┌─────────────┐  ┌──────────┐  ┌──────────┐│  │
 * │  │  │Time-Based   │  │Size-Based   │  │Adaptive  │  │Priority  ││  │
 * │  │  │             │  │             │  │          │  │Aware     ││  │
 * │  │  │- Min delay  │  │- Optimal    │  │- Auto    │  │- Per     ││  │
 * │  │  │- Max delay  │  │  size       │  │  adjust  │  │  priority││  │
 * │  │  │- Analytics  │  │- Game       │  │- Learning│  │- Mixed   ││  │
 * │  │  │  focused    │  │  events     │  │          │  │  workload││  │
 * │  │  └─────────────┘  └─────────────┘  └──────────┘  └──────────┘│  │
 * │  └────────────────────────────────────────────────────────────────┘  │
 * └────────────────────────────────────────────────────────────────────────┘
 * 
 * 
 * DATA FLOW DIAGRAM:
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 *   User Code
 *       │
 *       ▼
 *   EnqueueRequest(endpoint, data, config)
 *       │
 *       ├──► [Priority Check]
 *       │         │
 *       │         ├──► Critical + BypassRateLimit? ──YES──► SendImmediate()
 *       │         │                                               │
 *       │         NO                                              ▼
 *       │         │                                         [HTTP Request]
 *       │         │                                               │
 *       │         │                                               ▼
 *       │         │                                          [Response]
 *       │         ▼                                               │
 *       │    [Can Batch?]                                         ▼
 *       │         │                                           [Callback]
 *       │         ├──► YES ──► [Add to BatchBuffer]
 *       │         │                    │
 *       │         │                    ▼
 *       │         │            [Strategy.CanAddToBatch?]
 *       │         │                    │
 *       │         │                    ├──► YES ──► [Buffer.Add]
 *       │         │                    │
 *       │         │                    └──► NO ──► [Send Current Batch]
 *       │         │                                       │
 *       │         │                                       ▼
 *       │         │                              [Create New Buffer]
 *       │         │
 *       │         NO
 *       │         │
 *       │         ▼
 *       │    [RequestQueue.Enqueue]
 *       │         │
 *       │         ▼
 *       │    [Priority Queue]
 *       │         │
 *       │         ▼
 *       │    [ProcessQueueLoop]
 *       │         │
 *       │         ├──► [RateLimiter.CanSend?] ──NO──► [Wait]
 *       │         │                │
 *       │         │               YES
 *       │         │                │
 *       │         │                ▼
 *       │         │         [NetworkMonitor.IsOnline?] ──NO──► [Wait]
 *       │         │                │
 *       │         │               YES
 *       │         │                │
 *       │         │                ▼
 *       │         │         [Queue.Dequeue]
 *       │         │                │
 *       │         │                ▼
 *       │         │         [RateLimiter.Record]
 *       │         │                │
 *       │         │                ▼
 *       │         │         [HttpRequestSender.Send]
 *       │         │                │
 *       │         │                ▼
 *       │         │         [Handle Response]
 *       │         │                │
 *       │         │                ├──► 429? ──YES──► [RateLimiter.Cooldown]
 *       │         │                │                          │
 *       │         │                │                          ▼
 *       │         │                │                   [Re-enqueue]
 *       │         │                │
 *       │         │                ├──► Failed + Retry? ──YES──► [Exponential Backoff]
 *       │         │                │                                     │
 *       │         │                │                                     ▼
 *       │         │                │                              [Re-enqueue]
 *       │         │                │
 *       │         │                └──► Success ──► [ProcessedIds.Add]
 *       │         │                                        │
 *       │         │                                        ▼
 *       │         │                                   [Callback]
 *       │         │
 *       │         └──► [ProcessBatchLoop]
 *       │                      │
 *       │                      ▼
 *       │              [Check All Batches]
 *       │                      │
 *       │                      ▼
 *       │              [Strategy.ShouldSend?]
 *       │                      │
 *       │                      ├──► Size Full? ──YES──► [Send Batch]
 *       │                      │
 *       │                      └──► Time Exceeded? ──YES──► [Send Batch]
 *       │                                                         │
 *       │                                                         ▼
 *       │                                              [CreateBatchRequest]
 *       │                                                         │
 *       │                                                         ▼
 *       │                                              [Send as Single HTTP]
 *       │                                                         │
 *       │                                                         ▼
 *       │                                              [ProcessBatchResponse]
 *       │                                                         │
 *       │                                                         ▼
 *       │                                              [Distribute to All]
 *       │                                                         │
 *       │                                                         ▼
 *       └─────────────────────────────────────────────────► [All Callbacks]
 * 
 * 
 * STATISTICS FLOW:
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 *   RateLimiterUpdateLoop (every 100ms)
 *          │
 *          ▼
 *   RateLimiter.Update(currentTime)
 *          │
 *          ├──► Clean old timestamps
 *          │
 *          └──► Check cooldown expiry
 *                    │
 *                    ▼
 *             GetStatistics()
 *                    │
 *                    ├──► TotalQueued (from RequestQueue)
 *                    ├──► TotalBatched (from BatchBuffers)
 *                    ├──► ActiveRequests (from RequestSender)
 *                    ├──► IsRateLimited (from RateLimiter)
 *                    ├──► IsOnline (from NetworkMonitor)
 *                    └──► OfflineQueueCount (from OfflineStorage)
 *                                │
 *                                ▼
 *                         QueueStatistics
 *                                │
 *                                ▼
 *                    OnStatisticsUpdated.Invoke(stats)
 *                                │
 *                                ▼
 *                          [Subscribers]
 *                                │
 *                                ├──► UI Updates
 *                                ├──► Monitoring
 *                                └──► Alerting
 * 
 * 
 * OFFLINE SUPPORT FLOW:
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 *   NetworkMonitor.OnNetworkStatusChanged
 *                │
 *                ├──► Online → Offline
 *                │         │
 *                │         └──► SaveToOfflineQueue
 *                │                     │
 *                │                     └──► JsonSerialization
 *                │                                │
 *                │                                └──► PlayerPrefs.SetString
 *                │
 *                └──► Offline → Online
 *                          │
 *                          └──► LoadOfflineQueue
 *                                      │
 *                                      └──► JsonDeserialization
 *                                                  │
 *                                                  └──► Re-enqueue all
 *                                                          │
 *                                                          └──► Process normally
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

// This file is for documentation purposes only - no actual code
namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Architecture diagram và flow documentation.
    /// Xem file content để hiểu rõ architecture của hệ thống.
    /// </summary>
    public static class ArchitectureDiagram
    {
        // Documentation only - see file comments above
    }
}

