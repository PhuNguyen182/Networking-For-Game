# Quick Reference - Game Web Request Service v2.1.0

## 🚀 Quick Start

```csharp
// 1. Define response
[Endpoint("/api/v1/users", "Get User")]
public class UserResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result) 
    {
        Debug.Log($"User: {result.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage) 
    {
        Debug.LogError($"Error: {errorCode}");
    }
}

// 2. Initialize service
var config = new WebRequestConfig { baseUrl = "https://api.example.com" };
var service = new WebRequestService(config);

// 3. Make request
var response = await service.GetAsync<object, UserResponse>(requestBody: null);
response?.ProcessResponse();
```

---

## 📋 API Cheat Sheet

### GET Request

```csharp
// Without body
var response = await service.GetAsync<object, UserResponse>(
    requestBody: null,
    cancellationToken: token
);

// With body
var response = await service.GetAsync<GetRequest, UserResponse>(
    requestBody: new GetRequest { /* data */ },
    cancellationToken: token
);
```

### POST Request

```csharp
var response = await service.PostAsync<LoginRequest, LoginResponse>(
    requestBody: new LoginRequest { username = "test", password = "test123" },
    cancellationToken: token
);
```

### PUT Request

```csharp
var response = await service.PutAsync<UpdateRequest, UpdateResponse>(
    requestBody: new UpdateRequest { /* data */ },
    cancellationToken: token
);
```

---

## 🎯 Response Types

### Plain Response

```csharp
[Endpoint("/api/v1/login", "Login")]
public class LoginPlainResponse : BasePlainResponse
{
    public string token;
    public UserData userData;
}
```

### Generic Response (Recommended)

```csharp
// 1. Define data structure
[Serializable]
public class LoginData
{
    public string token;
    public UserData userData;
}

// 2. Create response class
[Endpoint("/api/v1/login", "Login")]
public class LoginResponse : BasePostResponse<LoginData>
{
    public override void OnResponseSuccess(LoginData result)
    {
        PlayerPrefs.SetString("token", result.token);
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"Login failed: {errorCode}");
    }
}
```

---

## 🔧 Configuration

```csharp
var config = new WebRequestConfig
{
    baseUrl = "https://api.example.com",
    defaultTimeoutMs = 30000,
    maxRetries = 3,
    retryDelayMs = 1000,
    useExponentialBackoff = true,
    enableLogging = true,
    logRequestBody = false,      // Security: don't log passwords
    logResponseBody = true
};
```

---

## 📊 HTTP Status Codes

```csharp
using GameNetworking.GameWebRequestService.Constants;

// Common codes
HttpStatusCode.Success              // 200
HttpStatusCode.Created              // 201
HttpStatusCode.BadRequest           // 400
HttpStatusCode.Unauthorized         // 401
HttpStatusCode.Forbidden            // 403
HttpStatusCode.NotFound             // 404
HttpStatusCode.InternalServerError  // 500

// Utility methods
HttpStatusCode.IsSuccess(code)
HttpStatusCode.IsClientError(code)
HttpStatusCode.IsServerError(code)
HttpStatusCode.GetDescription(code)
```

---

## 🎨 Newtonsoft.Json Attributes

```csharp
using Newtonsoft.Json;

[Serializable]
public class UserData
{
    [JsonProperty("user_id")]
    public string userId;
    
    [JsonIgnore]
    public string internalCache;
    
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string optionalField;
}
```

---

## ⚡ Common Patterns

### Cancellation

```csharp
private CancellationTokenSource cts;

public async UniTaskVoid MakeRequest()
{
    cts = new CancellationTokenSource();
    
    try
    {
        var response = await service.GetAsync<object, UserResponse>(
            requestBody: null,
            cancellationToken: cts.Token
        );
        response?.ProcessResponse();
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Cancelled");
    }
    finally
    {
        cts?.Dispose();
    }
}

public void Cancel()
{
    cts?.Cancel();
}
```

### Manual Error Handling

```csharp
var response = await service.GetAsync<object, UserResponse>(null);

if (response != null)
{
    if (response.IsSuccess && response.data != null)
    {
        response.OnResponseSuccess(response.data);
    }
    else
    {
        response.OnResponseFailed(response.statusCode, response.message);
    }
}
```

---

## 🐛 Common Errors

### Error 1: Missing EndpointAttribute

```csharp
// ❌ Error
public class UserResponse : BaseGetResponse<UserData> { }

// ✅ Fix
[Endpoint("/api/v1/users", "Get User")]
public class UserResponse : BaseGetResponse<UserData> { }
```

### Error 2: Not Implementing Callbacks

```csharp
// ❌ Error - Compiler will complain
public class UserResponse : BaseGetResponse<UserData>
{
    // Missing OnResponseSuccess and OnResponseFailed
}

// ✅ Fix
public class UserResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result) { }
    public override void OnResponseFailed(int errorCode, string errorMessage) { }
}
```

---

## 💡 Best Practices

### ✅ Do

- Use `EndpointAttribute` on all response classes
- Implement both `OnResponseSuccess` and `OnResponseFailed`
- Call `ProcessResponse()` for automatic handling
- Clean up `CancellationTokenSource` in `OnDestroy`
- Use generic responses (`BaseGetResponse<T>`) for type safety

### ❌ Don't

- Forget `EndpointAttribute` (will throw exception)
- Hardcode URLs (use attributes instead)
- Skip cancellation token cleanup (memory leak)
- Use plain responses for new code (use generic instead)

---

## 🧪 Testing

```csharp
using GameNetworking.GameWebRequestService.Tests;

var mockRequest = new MockWebRequest(
    simulateSuccess: true,
    simulatedStatusCode: 200,
    simulatedDelayMs: 100
);

var service = new WebRequestService(config, mockRequest);
var response = await service.GetAsync<object, TestResponse>(null);
```

---

## 📚 See Also

- **README.md** - Complete documentation
- **MIGRATION_GUIDE.md** - Migrate from v1.x to v2.x
- **NEW_ARCHITECTURE.md** - Architecture details
- **V2_1_CHANGES.md** - Latest changes
- **Examples/** - Working code examples

---

**Version**: 2.1.0  
**Last Updated**: November 23, 2024
