# Quick Reference Guide - v2.1.0

## 🆕 What's New in v2.1.0

- ✅ **GET Request Body Support**: GET requests có thể truyền requestBody (optional)
- ✅ **Newtonsoft.Json**: Replaced JsonUtility với Json.NET cho better features
- ✅ **Complex Types**: Support Dictionary, custom converters, attributes
- ⚠️ **Breaking Change**: GET API signature changed (thêm TRequest parameter)

---

## 🚀 Fast Start

### 1. Create Response Class

```csharp
using PracticalModules.WebRequestService.Attributes;
using PracticalModules.WebRequestService.Models;

// Define response data structure
[Serializable]
public class UserData
{
    public string userId;
    public string username;
    public string email;
}

// Create response class với EndpointAttribute
[Endpoint("/api/v1/users", "Get User")]
public class UserGetResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result)
    {
        Debug.Log($"User: {result.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"Failed: {errorCode} - {errorMessage}");
    }
}
```

### 2. Make Request

```csharp
// Initialize service (once)
var config = new WebRequestConfig { baseUrl = "https://api.example.com" };
var webRequestService = new WebRequestService(config);

// Make GET request
var response = await webRequestService.GetAsync<UserGetResponse>();
response?.ProcessResponse(); // Tự động gọi OnResponseSuccess hoặc OnResponseFailed
```

---

## 📋 Cheat Sheet

### GET Request

```csharp
// Response class
[Endpoint("/api/v1/resource", "Get Resource")]
public class GetResponse : BaseGetResponse<DataType>
{
    public override void OnResponseSuccess(DataType result) { }
    public override void OnResponseFailed(int errorCode, string errorMessage) { }
}

// Usage - Without body (truyền null)
var response = await webRequestService.GetAsync<object, GetResponse>(
    requestBody: null
);
response?.ProcessResponse();

// Usage - With body (optional)
var response = await webRequestService.GetAsync<GetRequest, GetResponse>(
    requestBody: new GetRequest { /* data */ }
);
response?.ProcessResponse();
```

### POST Request

```csharp
// Response class
[Endpoint("/api/v1/resource", "Create Resource")]
public class PostResponse : BasePostResponse<DataType>
{
    public override void OnResponseSuccess(DataType result) { }
    public override void OnResponseFailed(int errorCode, string errorMessage) { }
}

// Request model
[Serializable]
public class CreateRequest
{
    public string name;
    public int value;
}

// Usage
var requestBody = new CreateRequest { name = "test", value = 123 };
var response = await webRequestService.PostAsync<CreateRequest, PostResponse>(requestBody);
response?.ProcessResponse();
```

### PUT Request

```csharp
// Response class
[Endpoint("/api/v1/resource", "Update Resource")]
public class PutResponse : BasePutResponse<DataType>
{
    public override void OnResponseSuccess(DataType result) { }
    public override void OnResponseFailed(int errorCode, string errorMessage) { }
}

// Request model
[Serializable]
public class UpdateRequest
{
    public string name;
    public int value;
}

// Usage
var requestBody = new UpdateRequest { name = "updated", value = 456 };
var response = await webRequestService.PutAsync<UpdateRequest, PutResponse>(requestBody);
response?.ProcessResponse();
```

---

## 🎯 Common Patterns

### Manual Error Handling

```csharp
var response = await webRequestService.GetAsync<UserGetResponse>();

if (response != null)
{
    if (response.IsSuccess && response.data != null)
    {
        // Custom success logic
        response.OnResponseSuccess(response.data);
    }
    else
    {
        // Custom error logic
        response.OnResponseFailed(response.statusCode, response.message);
    }
}
```

### Cancellation Support

```csharp
private CancellationTokenSource cts;

public async UniTaskVoid MakeRequest()
{
    cts = new CancellationTokenSource();
    
    try
    {
        var response = await webRequestService.GetAsync<UserGetResponse>(
            cancellationToken: cts.Token
        );
        response?.ProcessResponse();
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Request cancelled");
    }
    finally
    {
        cts?.Dispose();
    }
}

public void CancelRequest()
{
    cts?.Cancel();
}
```

### Response Data Mapping

```csharp
[Serializable]
public class LoginResponseData
{
    public string token;
    public string refreshToken;
    public UserInfo userInfo;
    public long expiresAt;
}

[Serializable]
public class UserInfo
{
    public string userId;
    public string username;
    public string email;
    public int level;
}

[Endpoint("/api/v1/auth/login", "Login")]
public class LoginResponse : BasePostResponse<LoginResponseData>
{
    public override void OnResponseSuccess(LoginResponseData result)
    {
        // Access nested data
        PlayerPrefs.SetString("token", result.token);
        PlayerPrefs.SetString("user_id", result.userInfo.userId);
        PlayerPrefs.SetInt("level", result.userInfo.level);
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        switch (errorCode)
        {
            case 401: Debug.LogError("Invalid credentials"); break;
            case 403: Debug.LogError("Account locked"); break;
            default: Debug.LogError($"Error: {errorMessage}"); break;
        }
    }
}
```

---

## 🔧 Configuration

### Basic Config

```csharp
var config = new WebRequestConfig
{
    baseUrl = "https://api.example.com",
    defaultTimeoutMs = 30000,
    enableLogging = true
};
```

### Advanced Config

```csharp
var config = new WebRequestConfig
{
    baseUrl = "https://api.example.com",
    defaultTimeoutMs = 30000,
    maxRetries = 3,
    retryDelayMs = 1000,
    useExponentialBackoff = true,
    enableLogging = true,
    logRequestBody = false, // Security: don't log passwords
    logResponseBody = true
};
```

---

## 📊 Response Properties

### Common Properties

```csharp
response.statusCode  // HTTP status code (int)
response.message     // Server message (string)
response.timestamp   // Response timestamp (long)
response.data        // Response data (TResponseData)
response.IsSuccess   // Success flag (bool)
```

### Usage

```csharp
public override void OnResponseSuccess(UserData result)
{
    Debug.Log($"Status: {this.statusCode}");
    Debug.Log($"Message: {this.message}");
    Debug.Log($"Timestamp: {this.timestamp}");
    Debug.Log($"User: {result.username}");
}
```

---

## ⚡ Best Practices

### ✅ Do This

```csharp
// Use EndpointAttribute
[Endpoint("/api/v1/users", "Get User")]
public class UserResponse : BaseGetResponse<UserData> { }

// Implement both callbacks
public override void OnResponseSuccess(UserData result) { }
public override void OnResponseFailed(int errorCode, string errorMessage) { }

// Use ProcessResponse
response?.ProcessResponse();

// Handle cancellation
var response = await webRequestService.GetAsync<UserResponse>(cancellationToken);

// Clean up resources
void OnDestroy()
{
    cts?.Cancel();
    cts?.Dispose();
}
```

### ❌ Don't Do This

```csharp
// Missing EndpointAttribute - sẽ throw exception
public class UserResponse : BaseGetResponse<UserData> { }

// Không implement callbacks - compiler error
public class UserResponse : BaseGetResponse<UserData>
{
    // Missing OnResponseSuccess and OnResponseFailed
}

// Hardcode URLs - không linh động
await webRequestService.GetAsync<UserResponse>("/api/hardcoded/url");

// Không cleanup - memory leak
// Missing OnDestroy with cancellation cleanup
```

---

## 🐛 Common Errors

### Error 1: Missing EndpointAttribute

**Error**: `InvalidOperationException: Type 'UserResponse' does not have EndpointAttribute`

**Solution**:
```csharp
[Endpoint("/api/v1/users", "Get User")] // Add this
public class UserResponse : BaseGetResponse<UserData> { }
```

### Error 2: Empty Endpoint Path

**Error**: `InvalidOperationException: EndpointAttribute but Path is null or empty`

**Solution**:
```csharp
[Endpoint("/api/v1/users", "Get User")] // Provide valid path
public class UserResponse : BaseGetResponse<UserData> { }
```

### Error 3: Not Implementing Abstract Methods

**Error**: `Cannot create an instance of the abstract class or interface`

**Solution**:
```csharp
public class UserResponse : BaseGetResponse<UserData>
{
    // Implement both methods
    public override void OnResponseSuccess(UserData result) { }
    public override void OnResponseFailed(int errorCode, string errorMessage) { }
}
```

---

## 📚 HTTP Status Codes

### Success (2xx)

```csharp
HttpStatusCode.OK                  // 200
HttpStatusCode.Created             // 201
HttpStatusCode.Accepted            // 202
HttpStatusCode.NoContent           // 204
```

### Client Errors (4xx)

```csharp
HttpStatusCode.BadRequest          // 400
HttpStatusCode.Unauthorized        // 401
HttpStatusCode.Forbidden           // 403
HttpStatusCode.NotFound            // 404
HttpStatusCode.Conflict            // 409
HttpStatusCode.TooManyRequests     // 429
```

### Server Errors (5xx)

```csharp
HttpStatusCode.InternalServerError // 500
HttpStatusCode.BadGateway          // 502
HttpStatusCode.ServiceUnavailable  // 503
HttpStatusCode.GatewayTimeout      // 504
```

### Usage

```csharp
public override void OnResponseFailed(int errorCode, string errorMessage)
{
    if (HttpStatusCode.IsClientError(errorCode))
    {
        Debug.LogError($"Client error: {HttpStatusCode.GetDescription(errorCode)}");
    }
    else if (HttpStatusCode.IsServerError(errorCode))
    {
        Debug.LogError($"Server error: {HttpStatusCode.GetDescription(errorCode)}");
    }
}
```

---

## 🎓 Next Steps

### Learn More
1. **MIGRATION_GUIDE.md** - Migrate từ v1.x
2. **NEW_ARCHITECTURE.md** - Deep dive vào architecture
3. **README.md** - Complete documentation
4. **Examples/** - Working code examples

### Advanced Topics
- Custom error handling strategies
- Request interceptors
- Response caching
- Offline mode support
- Performance optimization

---

## 💡 Tips & Tricks

### Tip 1: Reuse Response Classes

```csharp
// Có thể reuse cho multiple endpoints
[Endpoint("/api/v1/users/{id}", "Get User")]
public class UserResponse : BaseGetResponse<UserData> { }

// Sau đó có thể extend nếu cần
[Endpoint("/api/v1/users/me", "Get Current User")]
public class CurrentUserResponse : UserResponse { }
```

### Tip 2: Share Data Structures

```csharp
// Define once, reuse nhiều lần
[Serializable]
public class UserData
{
    public string userId;
    public string username;
}

// Use trong multiple responses
public class UserGetResponse : BaseGetResponse<UserData> { }
public class UserUpdateResponse : BasePutResponse<UserData> { }
```

### Tip 3: Logging Best Practices

```csharp
public override void OnResponseSuccess(UserData result)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"[{GetType().Name}] Success: {result.username}");
    #endif
    
    // Production code
    ProcessUserData(result);
}
```

---

## 🔗 Quick Links

- **GitHub**: [Repository URL]
- **Documentation**: `Assets/GameNetworking/WebRequestService/`
- **Examples**: `Assets/GameNetworking/WebRequestService/Examples/`
- **Issues**: [Issue Tracker URL]

---

**Version**: 2.0.0  
**Last Updated**: November 23, 2024  
**Status**: ✅ Production Ready

