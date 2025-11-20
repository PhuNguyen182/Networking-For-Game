# Request Optimizer System - Tối Ưu Hóa Request

Hệ thống tối ưu hóa request với batching, rate limiting, offline support và retry logic.

## 🎯 Tính Năng Chính

### 1. **Priority-Based Queue System**
- 5 cấp độ priority: Critical, High, Normal, Low, Batch
- Xử lý requests theo thứ tự ưu tiên
- Tự động drop low-priority requests khi queue đầy

### 2. **Flexible Batching Strategies**
Hệ thống hỗ trợ nhiều chiến lược batching:
- **Time-Based**: Ưu tiên thời gian (analytics, telemetry)
- **Size-Based**: Ưu tiên đạt batch size tối đa (game events)
- **Adaptive**: Tự động điều chỉnh theo network conditions
- **Priority-Aware**: Chỉ batch requests cùng priority

### 3. **Rate Limiting**
- Sliding window algorithm
- Hỗ trợ per-second và per-minute limits
- Tự động cooldown khi hit rate limit (429)

### 4. **Network Monitoring**
- Tự động detect online/offline state
- Health check định kỳ
- Event-driven network status changes

### 5. **Offline Support**
- Lưu requests khi offline
- Tự động retry khi reconnect
- JSON serialization với compression

### 6. **Retry Logic**
- Exponential backoff
- Configurable retry count và delay
- Error type classification

## 🏗️ Architecture

Hệ thống tuân thủ SOLID principles:

```
RequestQueueManagerBehaviour (MonoBehaviour Adapter)
    ↓
RequestQueueManager (Plain Class - Core Logic)
    ├── IRequestQueue (PriorityRequestQueue)
    ├── IRateLimiter (RateLimiter)
    ├── INetworkMonitor (NetworkMonitor)
    ├── IRequestSender (HttpRequestSender)
    ├── IOfflineQueueStorage (JsonOfflineQueueStorage)
    └── IBatchingStrategy (Multiple Implementations)
```

## 📦 Sử Dụng

### Setup Cơ Bản

```csharp
// 1. Tạo config trong Unity Editor
// Assets → Create → Scriptable Objects → NetworkingForGame → RequestQueueManagerConfig

// 2. Truy cập singleton
var queueManager = RequestQueueManagerBehaviour.Instance;

// 3. Enqueue request
var config = configCollection.GetRequestConfigByPriority(RequestPriority.Normal);
queueManager.EnqueueRequest(
    endpoint: "https://api.example.com/events",
    data: new { eventName = "PlayerLevelUp", level = 10 },
    config: config,
    callback: (success, response) =>
    {
        if (success)
        {
            Debug.Log($"Request successful: {response}");
        }
        else
        {
            Debug.LogError($"Request failed: {response}");
        }
    }
);
```

### Custom Batching Strategy

```csharp
// Tạo custom batching strategy
public class CustomBatchingStrategy : BaseBatchingStrategy
{
    public CustomBatchingStrategy(int maxBatchSize, float maxBatchDelay) 
        : base(maxBatchSize, maxBatchDelay)
    {
    }
    
    public override bool ShouldSendBatch(IReadOnlyList<QueuedRequest> batch, float firstBatchTime)
    {
        // Custom logic here
        return base.ShouldSendBatch(batch, firstBatchTime);
    }
}

// Register strategy
var strategy = new CustomBatchingStrategy(50, 5f);
queueManager.RegisterBatchingStrategy("https://api.example.com/analytics", strategy);
```

### Monitoring Statistics

```csharp
// Subscribe to statistics updates
queueManager.OnStatisticsUpdated += stats =>
{
    Debug.Log($"Queue Stats: {stats}");
};

// Get current statistics
var currentStats = queueManager.GetStatistics();
Debug.Log($"Queued: {currentStats.TotalQueued}, Active: {currentStats.ActiveRequests}");
```

## ⚙️ Configuration

### RequestQueueManagerConfig

```
- maxRequestsPerSecond: Giới hạn requests/giây (default: 10)
- maxRequestsPerMinute: Giới hạn requests/phút (default: 300)
- rateLimitCooldown: Thời gian cooldown khi hit rate limit (default: 60s)
- maxQueueSize: Kích thước tối đa của queue (default: 1000)
- processInterval: Interval xử lý queue (default: 0.1s)
- maxConcurrentRequests: Số requests đồng thời tối đa (default: 5)
- enableOfflineQueue: Bật offline support (default: true)
- maxOfflineQueueSize: Kích thước offline queue (default: 500)
- networkCheckInterval: Interval check network (default: 5s)
- healthCheckUrl: URL để health check
```

### RequestConfig

```
- priority: Priority level (Critical/High/Normal/Low/Batch)
- canBatch: Có thể batch không
- maxBatchSize: Kích thước batch tối đa
- maxBatchDelay: Thời gian chờ tối đa trước khi gửi batch
- bypassRateLimit: Bypass rate limit (critical requests)
- maxRetries: Số lần retry tối đa
- retryDelay: Base delay cho retry (exponential backoff)
```

## 🎨 Design Patterns Sử Dụng

1. **Strategy Pattern**: Batching strategies có thể swap
2. **Dependency Injection**: All dependencies injected vào constructor
3. **Observer Pattern**: Event-driven network status và statistics
4. **Singleton Pattern**: MonoBehaviour adapter
5. **Factory Pattern**: Tạo batching strategies
6. **Adapter Pattern**: MonoBehaviour adapter cho Unity lifecycle

## 🚀 Performance Optimizations

1. **UniTask**: Thay thế Coroutines và Task cho performance tốt hơn
2. **Object Pooling**: Reuse collections và buffers
3. **Sliding Window**: Rate limiting algorithm hiệu quả
4. **Batch Processing**: Giảm số lượng requests thực tế
5. **Async/Await**: Non-blocking operations
6. **Thread Pool**: Background processing cho serialization

## 📝 Best Practices

1. **Sử dụng appropriate priority**: Critical cho purchases, Low cho analytics
2. **Configure batch settings**: Dựa trên server capacity và latency requirements
3. **Monitor statistics**: Track queue size và active requests
4. **Test offline scenarios**: Đảm bảo offline queue hoạt động đúng
5. **Handle callbacks**: Always check success status trước khi process response

## 🔧 Troubleshooting

### Queue luôn đầy?
- Tăng `maxQueueSize`
- Giảm `processInterval`
- Tăng `maxConcurrentRequests`
- Kiểm tra network latency

### Rate limit liên tục?
- Giảm `maxRequestsPerSecond/maxRequestsPerMinute`
- Tăng batch size để giảm số requests
- Sử dụng batching cho requests tương tự

### Offline queue không hoạt động?
- Kiểm tra `enableOfflineQueue` = true
- Kiểm tra `maxOfflineQueueSize`
- Verify `healthCheckUrl` accessible

## 📄 License

MIT License - Sử dụng tự do cho cả commercial và personal projects.

## 🤝 Contributing

Contributions are welcome! Đảm bảo tuân thủ:
- SOLID principles
- Unit tests cho new features
- Documentation cho public APIs
- Performance benchmarks

