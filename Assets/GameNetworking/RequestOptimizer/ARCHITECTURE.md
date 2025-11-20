# Request Optimizer - Architecture Documentation

## 📐 Kiến Trúc Tổng Quan

Hệ thống được thiết kế theo SOLID principles với các layer rõ ràng:

```
┌─────────────────────────────────────────────────────────┐
│         Unity MonoBehaviour Layer                       │
│  RequestQueueManagerBehaviour (Singleton Adapter)       │
└────────────────┬────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────┐
│              Core Business Logic Layer                  │
│          RequestQueueManager (Plain Class)              │
│  - Queue Processing Loop                                │
│  - Batch Processing Loop                                │
│  - Rate Limiter Update Loop                             │
└─┬──────┬──────┬──────┬──────┬──────┬────────────────────┘
  │      │      │      │      │      │
  ▼      ▼      ▼      ▼      ▼      ▼
┌───┐  ┌───┐  ┌───┐  ┌───┐  ┌───┐  ┌───────────────────┐
│ Q │  │ R │  │ N │  │ S │  │ O │  │    Batching       │
│ u │  │ a │  │ e │  │ e │  │ f │  │    Strategies     │
│ e │  │ t │  │ t │  │ n │  │ f │  │                   │
│ u │  │ e │  │ w │  │ d │  │ l │  │ - Time-Based      │
│ e │  │   │  │ o │  │ e │  │ i │  │ - Size-Based      │
│   │  │ L │  │ r │  │ r │  │ n │  │ - Adaptive        │
│   │  │ i │  │ k │  │   │  │ e │  │ - Priority-Aware  │
│   │  │ m │  │   │  │   │  │   │  │                   │
└───┘  └───┘  └───┘  └───┘  └───┘  └───────────────────┘
```

## 🎯 SOLID Principles Implementation

### 1. Single Responsibility Principle (SRP)

Mỗi class có một trách nhiệm duy nhất:

- **RequestQueueManager**: Điều phối tổng thể
- **PriorityRequestQueue**: Quản lý queue với priority
- **RateLimiter**: Xử lý rate limiting
- **NetworkMonitor**: Monitor network status
- **HttpRequestSender**: Gửi HTTP requests
- **JsonOfflineQueueStorage**: Lưu trữ offline
- **BatchingStrategy**: Xử lý batching logic

### 2. Open/Closed Principle (OCP)

Hệ thống mở cho extension, đóng cho modification:

```csharp
// Có thể extend batching strategy mà không modify code hiện có
public class CustomBatchingStrategy : BaseBatchingStrategy
{
    public override bool ShouldSendBatch(...)
    {
        // Custom implementation
    }
}
```

### 3. Liskov Substitution Principle (LSP)

Tất cả implementations của interface có thể thay thế cho nhau:

```csharp
// Có thể swap bất kỳ implementation nào
IRequestQueue queue = new PriorityRequestQueue();
// hoặc
IRequestQueue queue = new CircularBufferQueue();
```

### 4. Interface Segregation Principle (ISP)

Interfaces nhỏ, tập trung:

```csharp
// Mỗi interface có một purpose rõ ràng
IRequestQueue      - Chỉ queue operations
IRateLimiter      - Chỉ rate limiting
INetworkMonitor   - Chỉ network monitoring
```

### 5. Dependency Inversion Principle (DIP)

High-level modules depend on abstractions:

```csharp
public RequestQueueManager(
    IRequestQueue requestQueue,           // Depend on interface
    IRateLimiter rateLimiter,            // not concrete class
    INetworkMonitor networkMonitor,
    IRequestSender requestSender,
    IOfflineQueueStorage offlineStorage)
{
    // Constructor injection
}
```

## 🔄 Data Flow

### Normal Request Flow

```
User Code
    │
    ▼
EnqueueRequest()
    │
    ├─► Critical? ──YES─► SendImmediate()
    │                          │
    │                          ▼
    │                    [HTTP Request]
    │
    └─► Can Batch? ──YES─► BatchBuffer
              │                  │
              NO                 ▼
              │            [Wait for batch ready]
              │                  │
              ▼                  ▼
         RequestQueue ─────► ProcessQueue()
              │                  │
              ▼                  ▼
    [Rate Limit Check] ──► [HTTP Request]
              │                  │
              ▼                  ▼
         [Retry Logic] ──► [Callback]
```

### Batch Processing Flow

```
Multiple Requests
    │
    ├─► Request 1 ─┐
    ├─► Request 2 ─┼─► BatchBuffer
    ├─► Request 3 ─┘     │
    └─► Request N        │
                         ▼
            [Batching Strategy Check]
                    │
        ┌───────────┴───────────┐
        │                       │
    Size Full?            Time Exceeded?
        │                       │
        └───────────┬───────────┘
                    │ YES
                    ▼
        [Create Batch Request]
                    │
                    ▼
            [Send as Single Request]
                    │
                    ▼
        [Distribute Response to All]
```

## 🧩 Component Details

### RequestQueueManager

**Responsibilities:**
- Quản lý lifecycle của request processing
- Điều phối giữa các components
- Event handling cho network status changes

**Key Methods:**
- `StartAsync()`: Khởi động tất cả processing loops
- `EnqueueRequest()`: Thêm request vào queue
- `ProcessQueueLoopAsync()`: Loop xử lý queue
- `ProcessBatchLoopAsync()`: Loop xử lý batch
- `RateLimiterUpdateLoopAsync()`: Loop update rate limiter

### Batching Strategies

**Strategy Pattern Implementation:**

```
IBatchingStrategy (Interface)
    │
    ├─► BaseBatchingStrategy (Abstract Base)
    │       │
    │       ├─► TimeBasedBatchingStrategy
    │       │   - Ưu tiên thời gian
    │       │   - Min/max delay thresholds
    │       │
    │       ├─► SizeBasedBatchingStrategy
    │       │   - Ưu tiên kích thước
    │       │   - Optimal batch size
    │       │
    │       ├─► AdaptiveBatchingStrategy
    │       │   - Tự động điều chỉnh
    │       │   - Track success/failure
    │       │
    │       └─► PriorityAwareBatchingStrategy
    │           - Group by priority
    │           - Different delays per priority
    │
    └─► CustomBatchingStrategy (User-defined)
```

### Rate Limiting Algorithm

**Sliding Window Implementation:**

```
Time: ──────────────────────────►
      |    1 second    |
      ├────────────────┤
      [*]  [*]  [*]  [*]  ← Request timestamps
      
Window slides forward:
      ├────────────────┤
         [*]  [*]  [*]  [*]
      
Old timestamps removed:
            ├────────────────┤
               [*]  [*]  [*]  [*]
```

**Complexity:**
- Enqueue: O(1)
- Check limit: O(n) where n = requests in window
- Optimized with automatic cleanup

### Network Monitoring

**State Machine:**

```
     ┌─────────────┐
     │   Unknown   │
     └──────┬──────┘
            │ Initialize
            ▼
     ┌─────────────┐
  ┌─►│   Online    │◄─┐
  │  └──────┬──────┘  │
  │         │         │
  │ Reconnect│ Disconnect
  │         │         │
  │  ┌──────▼──────┐  │
  └──│  Offline    │──┘
     └─────────────┘
```

**Health Check Logic:**
1. Check Unity's `NetworkReachability`
2. If reachable, perform HTTP health check
3. Update online status
4. Trigger events if status changed
5. Retry offline requests if reconnected

## 📊 Performance Characteristics

### Time Complexity

| Operation | Best Case | Average | Worst Case |
|-----------|-----------|---------|------------|
| Enqueue | O(1) | O(1) | O(1) |
| Dequeue | O(1) | O(1) | O(p) * |
| Rate Limit Check | O(1) | O(w) ** | O(w) ** |
| Batch Check | O(1) | O(1) | O(1) |
| Offline Save | O(1) | O(n) *** | O(n) *** |

\* p = number of priorities  
\*\* w = requests in sliding window  
\*\*\* n = number of requests to serialize

### Space Complexity

| Component | Space |
|-----------|-------|
| Request Queue | O(n) |
| Batch Buffers | O(m) |
| Rate Limiter | O(w) |
| Offline Storage | O(k) |

n = queued requests  
m = batched requests  
w = window size  
k = offline requests

### Throughput

**Theoretical Maximum:**
```
Max Throughput = min(
    maxRequestsPerSecond,
    maxRequestsPerMinute / 60,
    maxConcurrentRequests / avgRequestDuration
)
```

**Actual Throughput:**
- Affected by network latency
- Reduced by batch delay
- Limited by rate limiting
- Improved by batching efficiency

## 🔐 Thread Safety

### Main Thread Operations
- All Unity API calls (Time, PlayerPrefs, Debug)
- Callback invocations
- MonoBehaviour lifecycle

### Background Thread Operations
- JSON serialization/deserialization
- Heavy computations
- File I/O (if implemented)

### Synchronization Strategy
```csharp
// Switch to thread pool for heavy work
await UniTask.SwitchToThreadPool();
var json = SerializeData(requests);

// Switch back to main thread for Unity APIs
await UniTask.SwitchToMainThread();
PlayerPrefs.SetString(key, json);
```

## 🎨 Design Patterns Used

1. **Singleton**: `RequestQueueManagerBehaviour`
2. **Strategy**: `IBatchingStrategy` implementations
3. **Dependency Injection**: Constructor injection throughout
4. **Observer**: Event-driven network status
5. **Factory**: Batching strategy creation
6. **Adapter**: MonoBehaviour → Plain class adapter
7. **Repository**: Offline storage abstraction
8. **Command**: Queued requests as commands

## 🔄 Extension Points

### Adding New Batching Strategy

```csharp
public class MyCustomStrategy : BaseBatchingStrategy
{
    public MyCustomStrategy(int maxSize, float maxDelay) 
        : base(maxSize, maxDelay) { }
    
    public override bool ShouldSendBatch(
        IReadOnlyList<QueuedRequest> batch, 
        float firstBatchTime)
    {
        // Custom logic here
        return base.ShouldSendBatch(batch, firstBatchTime);
    }
}

// Register
manager.RegisterBatchingStrategy(
    "https://api.example.com/custom", 
    new MyCustomStrategy(100, 10f)
);
```

### Custom Request Sender

```csharp
public class CustomRequestSender : IRequestSender
{
    public async UniTask<RequestResult> SendRequestAsync(
        QueuedRequest request)
    {
        // Custom HTTP client
        // Custom authentication
        // Custom encryption
        return RequestResult.Success(response);
    }
    
    // Implement other interface methods...
}

// Inject in constructor
var sender = new CustomRequestSender();
var manager = new RequestQueueManager(
    config, queue, rateLimiter, monitor, 
    sender, // Custom sender
    storage, strategies
);
```

### Custom Offline Storage

```csharp
public class SQLiteOfflineStorage : IOfflineQueueStorage
{
    public async UniTask SaveRequestAsync(QueuedRequest request)
    {
        // Save to SQLite database
        await database.InsertAsync(request);
    }
    
    // Implement other interface methods...
}
```

## 📈 Scalability Considerations

### Horizontal Scaling
- Multiple request queue instances per server region
- Distributed rate limiting with Redis
- Centralized offline storage with cloud sync

### Vertical Scaling
- Increase `maxConcurrentRequests`
- Increase `maxQueueSize`
- Optimize batch sizes based on server capacity

### Performance Tuning
1. **Batch Size**: Larger = fewer requests, higher latency
2. **Batch Delay**: Shorter = faster, more requests
3. **Process Interval**: Lower = responsive, more CPU
4. **Rate Limits**: Match server capacity

## 🧪 Testing Strategy

### Unit Tests
- Each component independently testable
- Mock all dependencies via interfaces
- Test edge cases and error conditions

### Integration Tests
- Test component interactions
- Test complete request flow
- Test network failure scenarios

### Performance Tests
- Load testing with varying request rates
- Memory profiling for leaks
- CPU profiling for bottlenecks
- Stress testing queue capacity

## 📚 Further Reading

- [UniTask Documentation](https://github.com/Cysharp/UniTask)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Rate Limiting Algorithms](https://en.wikipedia.org/wiki/Rate_limiting)
- [HTTP Status Codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)

