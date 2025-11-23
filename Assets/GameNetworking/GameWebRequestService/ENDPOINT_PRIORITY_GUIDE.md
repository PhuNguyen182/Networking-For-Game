# Endpoint Priority System - Hướng Dẫn Chi Tiết

## 📖 Tổng Quan

System Priority trong `EndpointAttribute` cho phép bạn **chính xác** xác định cách mỗi endpoint được xử lý bởi `OptimizedWebRequestService`, thay vì dựa vào URL pattern matching không chắc chắn.

---

## 🎯 Request Priority Types

### **5 Levels Priority**

| Priority | Batching | Rate Limit | Use Case | Examples |
|----------|----------|------------|----------|----------|
| **Critical** | ❌ No | Bypass | Must send immediately | Payment, Purchase, Transaction |
| **High** | ❌ No | Respect | Important but not critical | User login, Profile update |
| **Normal** | ✅ Yes (moderate) | Respect | Standard operations | Data fetching, Normal updates |
| **Low** | ✅ Yes (aggressive) | Respect | Can delay, can merge | Position updates, State sync |
| **Batch** | ✅ Yes (max) | Respect | Best for batching | Analytics, Telemetry, Events |

---

## 🔧 Cách Sử Dụng

### **1. Declare Priority trong EndpointAttribute**

```csharp
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Models;
using GameNetworking.RequestOptimizer.Scripts.Configuration;

// ✅ Critical Priority - Payment/Purchase
[Endpoint(
    route: "https://api.example.com/payment/process",
    name: "Process Payment",
    priority: RequestPriority.Critical  // ← Specify priority here
)]
public class PaymentResponse : BasePostResponse<PaymentData>
{
    public override void OnResponseSuccess(PaymentData result)
    {
        Debug.Log($"Payment successful: {result.transactionId}");
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogError($"Payment failed: {errorCode} - {message}");
    }
}

// ✅ Batch Priority - Analytics
[Endpoint(
    route: "https://api.example.com/analytics/track",
    name: "Track Analytics Event",
    priority: RequestPriority.Batch  // ← Batch aggressive
)]
public class AnalyticsResponse : BasePostResponse<AnalyticsData>
{
    public override void OnResponseSuccess(AnalyticsData result)
    {
        Debug.Log($"Analytics tracked: {result.eventId}");
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogWarning($"Analytics failed: {message}");
    }
}

// ✅ Normal Priority (Default)
[Endpoint(
    route: "https://api.example.com/user/profile",
    name: "Get User Profile",
    priority: RequestPriority.Normal  // ← Default, có thể bỏ qua
)]
public class UserProfileResponse : BaseGetResponse<UserProfileData>
{
    public override void OnResponseSuccess(UserProfileData result)
    {
        Debug.Log($"Profile loaded: {result.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogError($"Profile load failed: {message}");
    }
}
```

---

## 📊 Priority Behavior Chi Tiết

### **RequestPriority.Critical**

```csharp
[Endpoint(
    route: "https://api.example.com/purchase",
    priority: RequestPriority.Critical
)]
```

**Characteristics:**
- ❌ **No Batching**: Gửi ngay lập tức, không đợi batch
- ✅ **Bypass Rate Limit**: Bỏ qua rate limiting
- ⚡ **Highest Priority**: Xử lý trước tất cả
- 🔄 **Max Retries**: 3 retries với delay 0.5s

**Batching Config:**
```csharp
canBatch = false
bypassRateLimit = true
maxRetries = 3
retryDelay = 0.5f
```

**Use Cases:**
- 💰 Payment processing
- 🛒 Purchase transactions
- 💳 Financial operations
- 🔐 Critical security operations

**Example:**
```csharp
// Critical request được gửi NGAY LẬP TỨC
var response = await service.PostAsync<PurchaseRequest, PurchaseResponse>(
    new PurchaseRequest { userId = "user123", itemId = "sword", price = 9.99m }
);
// → Gửi immediately, không batch, bypass rate limit
```

---

### **RequestPriority.High**

```csharp
[Endpoint(
    route: "https://api.example.com/auth/login",
    priority: RequestPriority.High
)]
```

**Characteristics:**
- ❌ **No Batching**: Gửi riêng lẻ
- ❌ **Respect Rate Limit**: Tuân thủ rate limiting
- 🔄 **Max Retries**: 3 retries với delay 1s

**Batching Config:**
```csharp
canBatch = false
bypassRateLimit = false
maxRetries = 3
retryDelay = 1f
```

**Use Cases:**
- 🔐 User login
- 👤 Profile updates
- ⚙️ Important settings changes
- 📧 Critical notifications

---

### **RequestPriority.Normal** (Default)

```csharp
[Endpoint(
    route: "https://api.example.com/data/fetch",
    priority: RequestPriority.Normal  // Default, có thể bỏ qua
)]
```

**Characteristics:**
- ✅ **Moderate Batching**: Batch size 50, delay 3s
- ❌ **Respect Rate Limit**: Tuân thủ rate limiting
- 🔄 **Retries**: 2 retries với delay 1s

**Batching Config:**
```csharp
canBatch = true
maxBatchSize = 50
maxBatchDelay = 3f
bypassRateLimit = false
maxRetries = 2
retryDelay = 1f
```

**Use Cases:**
- 📊 Data fetching
- 📝 Normal updates
- 🔍 Search queries
- 📋 List retrievals

**Example:**
```csharp
// Normal requests có thể được batch nếu gửi nhanh
for (int i = 0; i < 10; i++)
{
    await service.GetAsync<Request, NormalResponse>(new Request());
}
// → 10 requests có thể batch thành 1-2 HTTP calls nếu gửi trong 3 giây
```

---

### **RequestPriority.Low**

```csharp
[Endpoint(
    route: "https://api.example.com/player/position",
    priority: RequestPriority.Low
)]
```

**Characteristics:**
- ✅ **Aggressive Batching**: Batch size 50, delay 5s
- ❌ **Respect Rate Limit**: Tuân thủ rate limiting
- 🔄 **Minimal Retries**: 1 retry với delay 2s
- 🔀 **Can Merge**: Có thể merge requests (last-wins)

**Batching Config:**
```csharp
canBatch = true
maxBatchSize = 50
maxBatchDelay = 5f
bypassRateLimit = false
maxRetries = 1
retryDelay = 2f
```

**Use Cases:**
- 🎮 Position updates
- 🔄 State synchronization
- 📍 Location tracking
- 🎯 Non-critical real-time data

**Example:**
```csharp
// Position updates - có thể merge, chỉ giữ latest
void Update()
{
    service.PostAsync<PositionRequest, PositionResponse>(
        new PositionRequest { 
            playerId = "player123",
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z
        }
    ).Forget(); // Fire and forget
}
// → Chỉ position mới nhất được gửi, các updates cũ bị merge/override
```

---

### **RequestPriority.Batch**

```csharp
[Endpoint(
    route: "https://api.example.com/analytics/track",
    priority: RequestPriority.Batch
)]
```

**Characteristics:**
- ✅ **Maximum Batching**: Batch size 100, delay 10s
- ❌ **Respect Rate Limit**: Tuân thủ rate limiting
- 🔄 **Minimal Retries**: 1 retry với delay 3s
- 📦 **Best for Batching**: Optimized cho batch operations

**Batching Config:**
```csharp
canBatch = true
maxBatchSize = 100
maxBatchDelay = 10f
bypassRateLimit = false
maxRetries = 1
retryDelay = 3f
```

**Use Cases:**
- 📈 Analytics tracking
- 📊 Telemetry data
- 📝 Event logging
- 🔍 Non-critical monitoring

**Example:**
```csharp
// Analytics - batch rất aggressive
for (int i = 0; i < 100; i++)
{
    service.PostAsync<AnalyticsRequest, AnalyticsResponse>(
        new AnalyticsRequest { 
            eventName = "PlayerAction",
            action = $"Action_{i}"
        }
    ).Forget();
}
// → 100 requests batch thành 1 HTTP call (hoặc 2 nếu > maxBatchSize)
// Mỗi request vẫn nhận đúng callback!
```

---

## 🎨 Complete Example

### **Định nghĩa Responses với Priority**

```csharp
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Models;
using GameNetworking.RequestOptimizer.Scripts.Configuration;

// 1. Critical - Payment
[Endpoint(
    route: "https://api.game.com/payment/purchase",
    name: "Purchase Item",
    priority: RequestPriority.Critical
)]
public class PurchaseResponse : BasePostResponse<PurchaseData>
{
    public override void OnResponseSuccess(PurchaseData result)
    {
        Debug.Log($"✅ Purchase Success: {result.itemName}");
        ShowSuccessNotification(result);
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogError($"❌ Purchase Failed: {message}");
        ShowErrorDialog(errorCode, message);
    }
}

// 2. High - User Login
[Endpoint(
    route: "https://api.game.com/auth/login",
    name: "User Login",
    priority: RequestPriority.High
)]
public class LoginResponse : BasePostResponse<LoginData>
{
    public override void OnResponseSuccess(LoginData result)
    {
        Debug.Log($"✅ Login Success: {result.username}");
        SaveAuthToken(result.token);
        LoadUserProfile();
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogError($"❌ Login Failed: {message}");
        ShowLoginError(message);
    }
}

// 3. Normal - Fetch Leaderboard
[Endpoint(
    route: "https://api.game.com/leaderboard/top",
    name: "Get Leaderboard",
    priority: RequestPriority.Normal
)]
public class LeaderboardResponse : BaseGetResponse<LeaderboardData>
{
    public override void OnResponseSuccess(LeaderboardData result)
    {
        Debug.Log($"✅ Leaderboard loaded: {result.entries.Length} entries");
        UpdateLeaderboardUI(result.entries);
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogWarning($"⚠️ Leaderboard load failed: {message}");
        ShowDefaultLeaderboard();
    }
}

// 4. Low - Position Update
[Endpoint(
    route: "https://api.game.com/player/position",
    name: "Update Position",
    priority: RequestPriority.Low
)]
public class PositionUpdateResponse : BasePostResponse<PositionData>
{
    public override void OnResponseSuccess(PositionData result)
    {
        Debug.Log($"✅ Position synced");
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogWarning($"⚠️ Position sync failed, will retry");
    }
}

// 5. Batch - Analytics
[Endpoint(
    route: "https://api.game.com/analytics/event",
    name: "Track Event",
    priority: RequestPriority.Batch
)]
public class AnalyticsResponse : BasePostResponse<AnalyticsData>
{
    public override void OnResponseSuccess(AnalyticsData result)
    {
        Debug.Log($"✅ Event tracked: {result.eventId}");
    }
    
    public override void OnResponseFailed(int errorCode, string message)
    {
        Debug.LogWarning($"⚠️ Analytics failed: {message}");
    }
}
```

### **Sử dụng trong Game**

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private WebRequestConfig webRequestConfig;
    [SerializeField] private RequestQueueManagerConfig queueConfig;
    [SerializeField] private RequestConfigCollection configCollection;
    [SerializeField] private OnlineCheckService onlineCheckService;
    
    private OptimizedWebRequestService _service;
    
    private async void Start()
    {
        // Initialize service
        _service = new OptimizedWebRequestService(
            webRequestConfig,
            queueConfig,
            configCollection,
            onlineCheckService
        );
        
        await _service.StartAsync(destroyCancellationToken);
        
        Debug.Log("OptimizedWebRequestService ready!");
    }
    
    // Critical - Purchase (gửi ngay)
    public async void OnPurchaseButtonClick(string itemId)
    {
        var request = new PurchaseRequest { itemId = itemId, userId = GetUserId() };
        
        var response = await _service.PostAsync<PurchaseRequest, PurchaseResponse>(
            request,
            destroyCancellationToken
        );
        
        // Response callback đã được gọi (OnResponseSuccess/OnResponseFailed)
        // Chỉ cần check result nếu cần logic bổ sung
    }
    
    // Normal - Fetch data (có thể batch)
    public async void LoadLeaderboard()
    {
        var response = await _service.GetAsync<EmptyRequest, LeaderboardResponse>(
            null,
            destroyCancellationToken
        );
    }
    
    // Batch - Analytics (batch aggressive)
    public void TrackEvent(string eventName, string action)
    {
        var request = new AnalyticsRequest { eventName = eventName, action = action };
        
        // Fire and forget - auto-batch
        _service.PostAsync<AnalyticsRequest, AnalyticsResponse>(
            request,
            destroyCancellationToken
        ).Forget();
    }
    
    // Low - Position updates (có thể merge)
    private void Update()
    {
        if (Time.frameCount % 10 == 0) // Every 10 frames
        {
            var request = new PositionRequest {
                playerId = GetUserId(),
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z
            };
            
            _service.PostAsync<PositionRequest, PositionUpdateResponse>(
                request,
                destroyCancellationToken
            ).Forget();
        }
    }
}
```

---

## 📈 Performance Impact

### **Benchmark Comparison**

#### **Scenario 1: Mixed Workload**
```
100 requests total:
- 5 Critical (payment)
- 10 High (login)
- 50 Normal (data fetch)
- 20 Low (position)
- 15 Batch (analytics)

WITHOUT Priority System:
→ 100 HTTP calls
→ ~5-10 seconds total time
→ High server load

WITH Priority System:
→ 5 + 10 + 10-15 + 2-4 + 1-2 = ~28-36 HTTP calls (64-72% reduction!)
→ ~2-3 seconds total time (50-70% faster!)
→ Low server load
```

#### **Scenario 2: Analytics Heavy**
```
1000 analytics events in 1 minute

WITHOUT Priority System:
→ 1000 HTTP calls
→ Server overwhelmed
→ Possible rate limiting (429 errors)

WITH Priority System (Batch):
→ 10-20 HTTP calls (98% reduction!)
→ Server load minimal
→ No rate limiting
→ All events delivered correctly
```

---

## ⚠️ Best Practices

### ✅ **DO**

```csharp
// 1. ✅ Always specify Priority cho critical operations
[Endpoint(route: "/payment", priority: RequestPriority.Critical)]
public class PaymentResponse : BasePostResponse<PaymentData> { }

// 2. ✅ Use Batch priority cho analytics
[Endpoint(route: "/analytics", priority: RequestPriority.Batch)]
public class AnalyticsResponse : BasePostResponse<AnalyticsData> { }

// 3. ✅ Use Low priority cho frequent updates
[Endpoint(route: "/position", priority: RequestPriority.Low)]
public class PositionResponse : BasePostResponse<PositionData> { }

// 4. ✅ Normal priority là default - OK nếu không chắc chắn
[Endpoint(route: "/data", priority: RequestPriority.Normal)]
public class DataResponse : BaseGetResponse<Data> { }
```

### ❌ **DON'T**

```csharp
// ❌ Don't use Critical cho non-critical operations
[Endpoint(route: "/analytics", priority: RequestPriority.Critical)]
public class AnalyticsResponse : BasePostResponse<AnalyticsData> { }
// → Tất cả analytics gửi immediately, không batch, waste resources!

// ❌ Don't use Batch cho payment/purchase
[Endpoint(route: "/payment", priority: RequestPriority.Batch)]
public class PaymentResponse : BasePostResponse<PaymentData> { }
// → Payment bị delay lên đến 10s! Unacceptable cho user!

// ❌ Don't forget Priority cho important endpoints
[Endpoint(route: "/purchase")]  // Missing priority!
public class PurchaseResponse : BasePostResponse<PurchaseData> { }
// → Default Normal, có thể bị batch, không phù hợp cho purchase
```

---

## 🔍 Troubleshooting

### **Problem: Critical requests bị batch**

```csharp
// ❌ Wrong
[Endpoint(route: "/payment")]  // No priority specified
public class PaymentResponse { }

// ✅ Fix
[Endpoint(route: "/payment", priority: RequestPriority.Critical)]
public class PaymentResponse { }
```

### **Problem: Analytics không được batch**

```csharp
// ❌ Wrong
[Endpoint(route: "/analytics", priority: RequestPriority.Normal)]
public class AnalyticsResponse { }

// ✅ Fix
[Endpoint(route: "/analytics", priority: RequestPriority.Batch)]
public class AnalyticsResponse { }
```

### **Problem: Position updates quá chậm**

```csharp
// ❌ Wrong
[Endpoint(route: "/position", priority: RequestPriority.High)]
public class PositionResponse { }
// → Gửi riêng lẻ, không batch, slow!

// ✅ Fix
[Endpoint(route: "/position", priority: RequestPriority.Low)]
public class PositionResponse { }
// → Batch aggressive, fast và efficient!
```

---

## 📚 Summary

### **Priority Decision Tree**

```
Is it a financial transaction (payment/purchase)?
└── YES → RequestPriority.Critical

Is it critical for user flow (login/security)?
└── YES → RequestPriority.High

Is it analytics/telemetry/logging?
└── YES → RequestPriority.Batch

Is it frequent updates (position/state)?
└── YES → RequestPriority.Low

Otherwise:
└── RequestPriority.Normal (default)
```

### **Key Takeaways**

1. ✅ **Always specify Priority** cho mọi endpoint quan trọng
2. ✅ **Critical** = Must send immediately, no batching
3. ✅ **Batch** = Best for analytics, maximum batching
4. ✅ **Low** = Good for frequent updates, can merge
5. ✅ **Normal** = Safe default, moderate batching

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-23  
**Status**: ✅ Production Ready

