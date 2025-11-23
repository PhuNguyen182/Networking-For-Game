# New Architecture Documentation

## Tổng Quan Kiến Trúc Mới

Kiến trúc mới của Web Request Service được thiết kế với các nguyên tắc:

1. **Separation by HTTP Method**: Mỗi HTTP method có base response class riêng
2. **Generic Type Safety**: Response data được type-safe với generics
3. **Auto Endpoint Resolution**: Tự động lấy endpoint từ metadata attributes
4. **Abstract Callback Pattern**: Force implementation của success/failed handlers
5. **Simplified Public API**: Ít parameters hơn, rõ ràng hơn

---

## Core Components

### 1. Base Response Classes

#### IBaseResponse Interface

```csharp
public interface IBaseResponse
{
    int statusCode { get; set; }
    string message { get; set; }
    long timestamp { get; set; }
    bool IsSuccess { get; }
}
```

**Mục đích**:
- Cung cấp contract chung cho tất cả response classes
- Cho phép xử lý generic mà không cần biết TResponseData
- Hỗ trợ polymorphism trong BestHttpWebRequest

#### BaseGetResponse<TResponseData>

```csharp
public abstract class BaseGetResponse<TResponseData> : IBaseResponse, IPoolable
    where TResponseData : class
{
    public int statusCode { get; set; }
    public string message { get; set; }
    public long timestamp { get; set; }
    public TResponseData data;
    
    public bool IsSuccess => statusCode >= 200 && statusCode < 300;
    
    public abstract void OnResponseSuccess(TResponseData result);
    public abstract void OnResponseFailed(int errorCode, string errorMessage);
    
    public void ProcessResponse()
    {
        if (IsSuccess && data != null)
            OnResponseSuccess(data);
        else
            OnResponseFailed(statusCode, message ?? "Unknown error");
    }
}
```

**Đặc điểm**:
- Generic type `TResponseData` cho response data structure
- Abstract methods force implementation của success/failed handlers
- `ProcessResponse()` tự động dispatch đúng callback
- Implement `IPoolable` cho object pooling
- Implement `IBaseResponse` cho generic handling

#### BasePostResponse<TResponseData>

Tương tự `BaseGetResponse` nhưng dành riêng cho POST requests.

**Lợi ích của separation**:
- Có thể customize behavior riêng cho POST
- Dễ dàng add POST-specific logic về sau
- Clear separation of concerns

#### BasePutResponse<TResponseData>

Tương tự `BaseGetResponse` nhưng dành riêng cho PUT requests.

---

### 2. EndpointHelper Utility

```csharp
public static class EndpointHelper
{
    public static string GetEndpointPath<TResponse>() where TResponse : class;
    public static string GetEndpointName<TResponse>() where TResponse : class;
    public static EndpointAttribute GetEndpointAttribute<TResponse>() where TResponse : class;
    public static bool HasEndpointAttribute<TResponse>() where TResponse : class;
    public static void ValidateEndpointAttribute<TResponse>() where TResponse : class;
}
```

**Chức năng**:
- Extract endpoint information từ `EndpointAttribute`
- Validate endpoint attribute existence và validity
- Provide clean API để làm việc với attributes

**Design Pattern**: Utility/Helper Pattern

---

### 3. Updated WebRequestService

#### New Public API

```csharp
// GET - chỉ cần cancellationToken
public async UniTask<TResponse> GetAsync<TResponse>(
    CancellationToken cancellationToken = default
) where TResponse : class

// POST - chỉ cần requestBody và cancellationToken
public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
    TRequest requestBody,
    CancellationToken cancellationToken = default
) where TRequest : class
  where TResponse : class

// PUT - chỉ cần requestBody và cancellationToken
public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
    TRequest requestBody,
    CancellationToken cancellationToken = default
) where TRequest : class
  where TResponse : class
```

**Thay đổi chính**:
1. ❌ Removed: `url` parameter (auto-resolved từ attribute)
2. ❌ Removed: `headers` parameter (có thể config global)
3. ✅ Simplified: Chỉ cần requestBody và optional cancellationToken

**Internal Flow**:
```
1. Client gọi GetAsync<ProfileGetResponse>()
2. WebRequestService.GetAsync:
   - Validate ProfileGetResponse có EndpointAttribute
   - Extract endpoint path từ attribute
   - Gọi IWebRequest.GetAsync với URL đã extract
3. BestHttpWebRequest thực hiện request
4. Parse response và populate IBaseResponse fields
5. Return response về client
6. Client gọi response.ProcessResponse()
7. Tự động dispatch OnResponseSuccess/OnResponseFailed
```

---

## Architecture Diagrams

### Class Hierarchy

```
IBaseResponse (interface)
├── BaseGetResponse<TResponseData> (abstract)
│   ├── ProfileGetResponse : BaseGetResponse<ProfileData>
│   ├── InventoryGetResponse : BaseGetResponse<InventoryData>
│   └── ... (custom GET responses)
│
├── BasePostResponse<TResponseData> (abstract)
│   ├── LoginResponse : BasePostResponse<LoginResponseData>
│   ├── RegisterResponse : BasePostResponse<RegisterData>
│   └── ... (custom POST responses)
│
└── BasePutResponse<TResponseData> (abstract)
    ├── ProfileUpdateResponse : BasePutResponse<ProfileUpdateData>
    ├── SettingsUpdateResponse : BasePutResponse<SettingsData>
    └── ... (custom PUT responses)

IPoolable (interface)
└── All base response classes implement this
```

### Request Flow Diagram

```
Client Code
    │
    ├─→ webRequestService.GetAsync<ProfileGetResponse>()
    │       │
    │       ├─→ EndpointHelper.ValidateEndpointAttribute<ProfileGetResponse>()
    │       │       └─→ Check [Endpoint] attribute exists
    │       │
    │       ├─→ EndpointHelper.GetEndpointPath<ProfileGetResponse>()
    │       │       └─→ Extract "/api/v1/user/profile" from attribute
    │       │
    │       └─→ webRequest.GetAsync<ProfileGetResponse>(url, null, token)
    │               │
    │               └─→ BestHttpWebRequest.GetAsync()
    │                       ├─→ Create HTTPRequest
    │                       ├─→ Send request to server
    │                       ├─→ Await response
    │                       ├─→ ParseResponse<ProfileGetResponse>(json, statusCode)
    │                       │       ├─→ JsonUtility.FromJson<ProfileGetResponse>(json)
    │                       │       └─→ Populate IBaseResponse fields (statusCode, timestamp)
    │                       └─→ Return ProfileGetResponse
    │
    └─→ response.ProcessResponse()
            ├─→ Check IsSuccess && data != null
            ├─→ If true: call OnResponseSuccess(data)
            └─→ If false: call OnResponseFailed(statusCode, message)
```

### Data Flow

```
Server Response JSON
    │
    ├─→ {"statusCode": 200, "message": "OK", "data": {"userId": "123", "username": "test"}}
    │
    └─→ BestHttpWebRequest.ParseResponse<ProfileGetResponse>()
            │
            ├─→ JsonUtility.FromJson<ProfileGetResponse>(json)
            │       └─→ Deserialize vào ProfileGetResponse object
            │
            ├─→ Cast to IBaseResponse
            │       └─→ response.statusCode = 200
            │       └─→ response.timestamp = CurrentUnixTime
            │
            └─→ Return populated ProfileGetResponse
                    │
                    ├─→ statusCode = 200
                    ├─→ message = "OK"
                    ├─→ timestamp = 1234567890
                    └─→ data = ProfileData { userId="123", username="test" }
```

---

## Design Patterns Used

### 1. Template Method Pattern

**Base Response Classes** sử dụng Template Method:

```csharp
public void ProcessResponse() // Template method
{
    if (IsSuccess && data != null)
        OnResponseSuccess(data); // Hook 1 - subclass implement
    else
        OnResponseFailed(statusCode, message); // Hook 2 - subclass implement
}
```

**Lợi ích**:
- Define skeleton của algorithm trong base class
- Subclasses implement specific steps
- Đảm bảo consistent flow across all responses

### 2. Strategy Pattern

**Abstract callbacks** là Strategy Pattern:

```csharp
public abstract void OnResponseSuccess(TResponseData result);
public abstract void OnResponseFailed(int errorCode, string errorMessage);
```

**Lợi ích**:
- Each concrete response class có strategy riêng để handle success/failure
- Easy to swap strategies bằng cách thay đổi implementation

### 3. Facade Pattern

**WebRequestService** là Facade:

```csharp
public class WebRequestService
{
    private readonly IWebRequest webRequest;
    private readonly ResponsePoolManager poolManager;
    
    public async UniTask<TResponse> GetAsync<TResponse>() { }
    public async UniTask<TResponse> PostAsync<TRequest, TResponse>() { }
    public async UniTask<TResponse> PutAsync<TRequest, TResponse>() { }
}
```

**Lợi ích**:
- Hide complexity của BestHttpWebRequest, EndpointHelper, ResponsePoolManager
- Provide simple interface cho clients
- Single entry point cho tất cả web requests

### 4. Dependency Injection

**Constructor injection** trong WebRequestService:

```csharp
public WebRequestService(WebRequestConfig config)
{
    this.webRequest = new BestHttpWebRequest(config, poolManager);
}

public WebRequestService(WebRequestConfig config, IWebRequest webRequest)
{
    this.webRequest = webRequest; // Inject custom implementation
}
```

**Lợi ích**:
- Testability: Có thể inject mock IWebRequest
- Flexibility: Support nhiều implementations
- Decoupling: WebRequestService không depend vào BestHttpWebRequest directly

### 5. Object Pool Pattern

**ResponsePoolManager** implement Object Pooling:

```csharp
public TResponse Get<TResponse>() where TResponse : class, IPoolable
{
    var pool = GetOrCreatePool<TResponse>();
    var response = pool.Get();
    response.OnGetFromPool();
    return response;
}

public void Return<TResponse>(TResponse response) where TResponse : class, IPoolable
{
    response.OnReturnToPool();
    var pool = GetOrCreatePool<TResponse>();
    pool.Return(response);
}
```

**Lợi ích**:
- Reduce GC pressure
- Reuse objects thay vì create mới
- Better performance

---

## SOLID Principles Compliance

### Single Responsibility Principle (SRP)

✅ **Mỗi class có 1 responsibility duy nhất**:

- `BaseGetResponse`: Handle GET response logic
- `BasePostResponse`: Handle POST response logic
- `BasePutResponse`: Handle PUT response logic
- `EndpointHelper`: Extract endpoint information từ attributes
- `WebRequestService`: Orchestrate web requests
- `BestHttpWebRequest`: Execute HTTP requests với Best HTTP

### Open/Closed Principle (OCP)

✅ **Open for extension, closed for modification**:

```csharp
// Extend bằng cách create new response class
public class CustomResponse : BaseGetResponse<CustomData>
{
    // Extend behavior
    public override void OnResponseSuccess(CustomData result)
    {
        // Custom implementation
    }
}

// Không cần modify base classes
```

### Liskov Substitution Principle (LSP)

✅ **Subclasses có thể thay thế base classes**:

```csharp
// Tất cả các response classes đều implement IBaseResponse
IBaseResponse response1 = new ProfileGetResponse();
IBaseResponse response2 = new LoginResponse();
IBaseResponse response3 = new ProfileUpdateResponse();

// Có thể sử dụng polymorphically
void ProcessAnyResponse(IBaseResponse response)
{
    Debug.Log($"Status: {response.statusCode}");
    Debug.Log($"Success: {response.IsSuccess}");
}
```

### Interface Segregation Principle (ISP)

✅ **Clients không depend vào methods họ không dùng**:

```csharp
// IBaseResponse chỉ có essential members
public interface IBaseResponse
{
    int statusCode { get; set; }
    string message { get; set; }
    long timestamp { get; set; }
    bool IsSuccess { get; }
}

// IPoolable separated từ IBaseResponse
public interface IPoolable
{
    void OnReturnToPool();
    void OnGetFromPool();
}

// Classes implement chỉ interfaces họ cần
```

### Dependency Inversion Principle (DIP)

✅ **Depend on abstractions, not concretions**:

```csharp
// WebRequestService depends on IWebRequest interface
private readonly IWebRequest webRequest;

// Không depend directly vào BestHttpWebRequest
// public WebRequestService(BestHttpWebRequest webRequest) // ❌ Bad

// Depend on interface
public WebRequestService(WebRequestConfig config, IWebRequest webRequest) // ✅ Good
{
    this.webRequest = webRequest;
}
```

---

## Performance Considerations

### 1. Zero Allocation với UniTask

```csharp
public async UniTask<TResponse> GetAsync<TResponse>()
{
    // UniTask không allocate như Task
    return await webRequest.GetAsync<TResponse>(url, null, token);
}
```

**Benefit**: 
- No heap allocations
- Better performance hơn System.Threading.Tasks.Task

### 2. Object Pooling

```csharp
// Reuse response objects
var response = poolManager.Get<ProfileGetResponse>();
// ... use response
poolManager.Return(response);
```

**Benefit**:
- Reduce GC pressure
- Faster object creation

### 3. Reflection Optimization

```csharp
// EndpointHelper cache reflection results
private static Dictionary<Type, EndpointAttribute> attributeCache = new();

public static EndpointAttribute GetEndpointAttribute(Type type)
{
    if (attributeCache.TryGetValue(type, out var cached))
        return cached;
        
    var attribute = type.GetCustomAttribute<EndpointAttribute>();
    attributeCache[type] = attribute;
    return attribute;
}
```

**Note**: Current implementation không cache, có thể optimize về sau nếu cần.

### 4. JSON Parsing

```csharp
// Unity's JsonUtility - fast nhất cho Unity
var response = JsonUtility.FromJson<TResponse>(json);
```

**Benefit**:
- Native Unity serialization
- Faster than third-party libraries

---

## Error Handling Strategy

### 1. Validation at API Boundary

```csharp
public async UniTask<TResponse> GetAsync<TResponse>()
{
    // Validate BEFORE making request
    EndpointHelper.ValidateEndpointAttribute<TResponse>();
    
    // Extract endpoint
    var url = EndpointHelper.GetEndpointPath<TResponse>();
    
    // Make request
    return await webRequest.GetAsync<TResponse>(url, null, token);
}
```

### 2. Try-Catch in BestHttpWebRequest

```csharp
try
{
    // Send request
    await request.Send();
    
    // Process response
    return ParseResponse<TResponse>(json, statusCode);
}
catch (Exception ex)
{
    Debug.LogError($"Request failed: {ex.Message}");
    return CreateErrorResponse<TResponse>(ex);
}
```

### 3. Abstract Callbacks for Business Logic

```csharp
public override void OnResponseFailed(int errorCode, string errorMessage)
{
    // Business-specific error handling
    switch (errorCode)
    {
        case 401: HandleUnauthorized(); break;
        case 403: HandleForbidden(); break;
        case 500: HandleServerError(); break;
    }
}
```

---

## Testing Strategy

### 1. Unit Testing với Mock

```csharp
var mockRequest = new MockWebRequest();
var service = new WebRequestService(config, mockRequest);

// Test without real network calls
var response = await service.GetAsync<ProfileGetResponse>();
```

### 2. Integration Testing

```csharp
var realService = new WebRequestService(realConfig);

// Test with real Best HTTP
var response = await realService.GetAsync<ProfileGetResponse>();
```

### 3. Response Callback Testing

```csharp
var response = new ProfileGetResponse
{
    statusCode = 200,
    data = new ProfileData { username = "test" }
};

// Test success callback
response.ProcessResponse(); // Should call OnResponseSuccess

response.statusCode = 401;
response.ProcessResponse(); // Should call OnResponseFailed
```

---

## Summary

### Key Improvements

1. ✅ **Type Safety**: Generic response data types
2. ✅ **Cleaner API**: Fewer parameters, auto endpoint resolution
3. ✅ **Better Separation**: GET/POST/PUT có base classes riêng
4. ✅ **Forced Error Handling**: Abstract callbacks đảm bảo implement
5. ✅ **SOLID Compliance**: Tuân thủ tất cả SOLID principles
6. ✅ **Testability**: Easy to mock và test
7. ✅ **Performance**: Object pooling, zero allocation async

### Architecture Benefits

- **Maintainability**: Clear structure, easy to understand
- **Extensibility**: Easy to add new response types
- **Testability**: Mockable interfaces
- **Performance**: Optimized với pooling và UniTask
- **Type Safety**: Compile-time checking với generics
- **Error Handling**: Consistent error handling pattern

---

## Next Steps

Recommended enhancements for future:

1. **Attribute Caching**: Cache reflection results trong EndpointHelper
2. **Request Retry**: Automatic retry với exponential backoff
3. **Response Caching**: Cache GET responses cho offline mode
4. **Rate Limiting**: Client-side rate limiting
5. **Metrics**: Track request timing và success rates
6. **Logging**: Structured logging với correlation IDs

---

**Version**: 2.0.0  
**Last Updated**: 2024  
**Author**: Networking-For-Game Team

