# Game Web Request Service

Hệ thống Web Request service hoàn chỉnh sử dụng **Best HTTP** với **Newtonsoft.Json**, tuân thủ SOLID principles và hỗ trợ object pooling.

## ✨ Features

- ✅ **Best HTTP Integration** - 100% Best HTTP API, không dùng UnityWebRequest
- ✅ **Newtonsoft.Json** - Industry-standard JSON serialization
- ✅ **SOLID Principles** - Tuân thủ nghiêm ngặt các nguyên tắc SOLID
- ✅ **Object Pooling** - Pool tự động cho response objects
- ✅ **Generic Response Classes** - BaseGetResponse, BasePostResponse, BasePutResponse với TResponseData
- ✅ **Abstract Callbacks** - OnResponseSuccess và OnResponseFailed methods
- ✅ **Auto Endpoint Resolution** - Tự động lấy endpoint từ EndpointAttribute
- ✅ **UniTask Async** - Zero-allocation async/await
- ✅ **GET Request Body** - GET requests hỗ trợ optional requestBody
- ✅ **Cancellation Support** - Full CancellationToken support
- ✅ **Memory Safe** - Không có memory leaks

## 🚀 Quick Start

### 1. Define Response Class

```csharp
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Models;

[Serializable]
public class UserData
{
    public string userId;
    public string username;
    public string email;
}

[Endpoint("/api/v1/user/profile", "Get User Profile")]
public class ProfileGetResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result)
    {
        Debug.Log($"Success! User: {result.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"Failed: {errorCode} - {errorMessage}");
    }
}
```

### 2. Initialize Service

```csharp
using GameNetworking.GameWebRequestService.Core;
using GameNetworking.GameWebRequestService.Models;

var config = new WebRequestConfig
{
    baseUrl = "https://api.example.com",
    defaultTimeoutMs = 30000,
    enableLogging = true
};

var webRequestService = new WebRequestService(config);
```

### 3. Make Request

```csharp
// GET request (requestBody optional)
var response = await webRequestService.GetAsync<object, ProfileGetResponse>(
    requestBody: null,
    cancellationToken: cancellationToken
);
response?.ProcessResponse(); // Automatically calls OnResponseSuccess/OnResponseFailed

// POST request
var response = await webRequestService.PostAsync<LoginRequest, LoginResponse>(
    requestBody: new LoginRequest { username = "test", password = "test123" },
    cancellationToken: cancellationToken
);
response?.ProcessResponse();

// PUT request
var response = await webRequestService.PutAsync<UpdateRequest, UpdateResponse>(
    requestBody: new UpdateRequest { /* data */ },
    cancellationToken: cancellationToken
);
response?.ProcessResponse();
```

## 📁 Structure

```
GameWebRequestService/
├── Attributes/
│   └── EndpointAttribute.cs          # Attribute to mark endpoint info
├── Constants/
│   ├── HttpStatusCode.cs             # HTTP status code constants
│   └── PoolingConstants.cs           # Object pooling constants
├── Core/
│   ├── BestHttpWebRequest.cs         # Best HTTP implementation
│   └── WebRequestService.cs          # Main service facade
├── Examples/
│   ├── GetProfileRequest.cs          # Example GET request
│   ├── LoginRequest.cs               # Example POST request
│   ├── LoginPlainResponse.cs         # Example plain response
│   ├── NewLoginResponse.cs           # Example POST response
│   ├── ProfileGetResponse.cs         # Example GET response
│   ├── ProfileUpdateResponse.cs      # Example PUT response
│   └── ...
├── Interfaces/
│   └── IWebRequest.cs                # Web request interface
├── Models/
│   ├── BasePlainResponse.cs          # Legacy plain response base
│   ├── BaseGetResponse.cs            # Generic GET response base
│   ├── BasePostResponse.cs           # Generic POST response base
│   ├── BasePutResponse.cs            # Generic PUT response base
│   ├── IBaseResponse.cs              # Common response interface
│   ├── IPoolable.cs                  # Poolable object interface
│   └── WebRequestConfig.cs           # Configuration model
├── Pooling/
│   ├── ObjectPool.cs                 # Generic object pool
│   └── ResponsePoolManager.cs        # Response pool manager
├── Tests/
│   ├── MockWebRequest.cs             # Mock for testing
│   └── WebRequestServiceTests.cs     # Test suite
├── Utilities/
│   └── EndpointHelper.cs             # Endpoint attribute helper
└── README.md                          # This file
```

## 🎯 Response Types

### 1. Plain Response (Legacy)

```csharp
[Endpoint("/api/v1/auth/login", "Login")]
public class LoginPlainResponse : BasePlainResponse
{
    public string token;
    public UserData userData;
}

// Usage - manual error handling
var response = await webRequestService.PostAsync<LoginRequest, LoginPlainResponse>(requestBody);
if (response != null && response.IsSuccess)
{
    // Success
}
```

### 2. Generic Response (Recommended)

```csharp
[Serializable]
public class LoginResponseData
{
    public string token;
    public UserData userData;
}

[Endpoint("/api/v1/auth/login", "Login")]
public class LoginResponse : BasePostResponse<LoginResponseData>
{
    public override void OnResponseSuccess(LoginResponseData result)
    {
        // Auto success handling
        PlayerPrefs.SetString("token", result.token);
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        // Auto error handling
        Debug.LogError($"Login failed: {errorCode}");
    }
}

// Usage - automatic callbacks
var response = await webRequestService.PostAsync<LoginRequest, LoginResponse>(requestBody);
response?.ProcessResponse(); // Calls OnResponseSuccess or OnResponseFailed
```

## 🔧 Configuration

```csharp
var config = new WebRequestConfig
{
    baseUrl = "https://api.example.com",
    defaultTimeoutMs = 30000,          // 30 seconds
    maxRetries = 3,                    // Retry 3 times
    retryDelayMs = 1000,               // 1 second delay
    useExponentialBackoff = true,      // Exponential backoff
    enableLogging = true,              // Debug logs
    logRequestBody = false,            // Don't log passwords
    logResponseBody = true             // Log responses
};
```

## 📖 HTTP Status Codes

```csharp
using GameNetworking.GameWebRequestService.Constants;

// Success codes
HttpStatusCode.Success                  // 200
HttpStatusCode.Created                  // 201
HttpStatusCode.NoContent                // 204

// Client errors
HttpStatusCode.BadRequest               // 400
HttpStatusCode.Unauthorized             // 401
HttpStatusCode.Forbidden                // 403
HttpStatusCode.NotFound                 // 404

// Server errors
HttpStatusCode.InternalServerError      // 500
HttpStatusCode.ServiceUnavailable       // 503

// Utility methods
HttpStatusCode.IsSuccess(statusCode)
HttpStatusCode.IsClientError(statusCode)
HttpStatusCode.IsServerError(statusCode)
HttpStatusCode.GetDescription(statusCode)
```

## 🎨 Newtonsoft.Json Attributes

```csharp
using Newtonsoft.Json;

[Serializable]
public class UserData
{
    [JsonProperty("user_id")]           // Map from snake_case
    public string userId;
    
    [JsonIgnore]                        // Skip serialization
    public string internalCache;
    
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string optionalField;        // Ignore if null
}
```

## 🧪 Testing

```csharp
using GameNetworking.GameWebRequestService.Tests;

// Use MockWebRequest for testing
var mockRequest = new MockWebRequest(
    simulateSuccess: true,
    simulatedStatusCode: 200,
    simulatedDelayMs: 100
);

var service = new WebRequestService(config, mockRequest);

// Test without real network calls
var response = await service.GetAsync<object, TestResponse>(null);
```

## 🔗 Dependencies

- **Best HTTP** - High-performance HTTP client
- **Newtonsoft.Json** - JSON serialization
- **UniTask** - Zero-allocation async/await

## 📚 Documentation

- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Quick reference guide
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Migration from v1.x to v2.x
- **[NEW_ARCHITECTURE.md](NEW_ARCHITECTURE.md)** - Architecture deep dive
- **[V2_1_CHANGES.md](V2_1_CHANGES.md)** - Latest changes in v2.1.0
- **[CHANGELOG.md](CHANGELOG.md)** - Version history

## 💡 Best Practices

### ✅ Do This

```csharp
// Use EndpointAttribute
[Endpoint("/api/v1/users", "Get User")]
public class UserResponse : BaseGetResponse<UserData> { }

// Implement both callbacks
public override void OnResponseSuccess(UserData result) { }
public override void OnResponseFailed(int errorCode, string errorMessage) { }

// Use ProcessResponse for automatic handling
response?.ProcessResponse();

// Clean up resources
void OnDestroy()
{
    cancellationTokenSource?.Cancel();
    cancellationTokenSource?.Dispose();
}
```

### ❌ Don't Do This

```csharp
// Missing EndpointAttribute
public class UserResponse : BaseGetResponse<UserData> { } // Error!

// Not implementing callbacks
// Compiler will error if you don't implement abstract methods

// Not cleaning up
// Missing cancellation token cleanup causes memory leaks
```

## 🎯 Examples

Check **Examples/** folder for complete working examples:
- `LoginPlainResponse.cs` - Plain response example
- `NewLoginResponse.cs` - Generic POST response
- `ProfileGetResponse.cs` - Generic GET response
- `ProfileUpdateResponse.cs` - Generic PUT response
- `NewWebRequestExample.cs` - Complete usage examples

## 📊 Version

**Current Version**: 2.1.0  
**Status**: ✅ Production Ready  
**Last Updated**: November 23, 2024

## 🔄 Changelog

See **[CHANGELOG.md](CHANGELOG.md)** for version history and changes.

---

**Happy Coding!** 🚀
