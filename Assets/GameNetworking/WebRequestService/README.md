# Web Request Service

Hệ thống Web Request hoàn chỉnh sử dụng **Best HTTP** package với hiệu suất cao, tuân thủ nguyên tắc SOLID và sử dụng object pooling.

## 🎉 What's New in v2.0.0

**Major Architecture Refactoring** với simplified API và better type safety:

- 🆕 **Generic Base Response Classes**: `BaseGetResponse<T>`, `BasePostResponse<T>`, `BasePutResponse<T>`
- 🆕 **Abstract Callbacks**: `OnResponseSuccess(T)` và `OnResponseFailed(int, string)` 
- 🆕 **Auto Endpoint Resolution**: Tự động lấy URL từ `EndpointAttribute`
- 🆕 **Simplified API**: Không cần truyền `url` và `headers` nữa
- 🆕 **Better Type Safety**: Generic type parameters cho response data
- 📚 **Complete Documentation**: Migration guide, architecture guide, quick reference

👉 **[Quick Start](#-quick-start-v20)** | **[Migration Guide](MIGRATION_GUIDE.md)** | **[New Architecture](NEW_ARCHITECTURE.md)**

---

## 🎯 Tính Năng

- ✅ **Best HTTP Integration**: Sử dụng 100% Best HTTP API, không dùng UnityWebRequest
- ✅ **SOLID Principles**: Tuân thủ nghiêm ngặt các nguyên tắc SOLID
- ✅ **Object Pooling**: Pool tự động cho request/response objects
- ✅ **Type Factory**: Sử dụng TypeFactory cho hiệu suất cao hơn 100x so với Activator.CreateInstance
- ✅ **UniTask Async**: Async/await pattern với UniTask
- ✅ **Error Handling**: Try-catch với logging chi tiết cho mọi request
- ✅ **Status Code Constants**: Static class chứa tất cả HTTP status codes
- ✅ **Endpoint Attributes**: Đánh dấu response class với endpoint info
- ✅ **Auto Retry**: Tự động retry với exponential backoff
- ✅ **Cancellation Support**: Hỗ trợ CancellationToken
- ✅ **Memory Safe**: Không có memory leaks, tự động cleanup

## 📁 Cấu Trúc Thư Mục

```
WebRequestService/
├── Attributes/
│   └── EndpointAttribute.cs          # Attribute để đánh dấu endpoint info
├── Constants/
│   └── HttpStatusCode.cs             # HTTP status code constants
├── Core/
│   ├── BestHttpWebRequest.cs         # Implementation với Best HTTP
│   └── WebRequestService.cs          # Main service facade
├── Examples/
│   ├── LoginRequest.cs               # Example request model
│   ├── LoginResponse.cs              # Example response model
│   └── WebRequestExample.cs          # Example usage
├── Interfaces/
│   └── IWebRequest.cs                # Interface cho web request
├── Models/
│   ├── BaseResponse.cs               # Base class cho response
│   ├── IPoolable.cs                  # Interface cho poolable objects
│   └── WebRequestConfig.cs           # Configuration model
├── Pooling/
│   ├── ObjectPool.cs                 # Generic object pool
│   └── ResponsePoolManager.cs        # Manager cho response pools
└── README.md                          # Documentation
```

## 🚀 Quick Start (v2.0)

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

// Create response với EndpointAttribute
[Endpoint("/api/v1/users", "Get User")]
public class UserGetResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result)
    {
        Debug.Log($"Success! User: {result.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"Failed! Code: {errorCode}, Message: {errorMessage}");
    }
}
```

### 2. Make Request

```csharp
// Initialize service
var config = new WebRequestConfig { baseUrl = "https://api.example.com" };
var webRequestService = new WebRequestService(config);

// Make GET request - tự động lấy endpoint từ attribute
var response = await webRequestService.GetAsync<UserGetResponse>();
response?.ProcessResponse(); // Automatically calls OnResponseSuccess or OnResponseFailed
```

**That's it!** 🎉 No need to specify URL or headers - they're handled automatically.

👉 See **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** for more examples.

---

## 🚀 Cài Đặt

### 1. Import Best HTTP Package

Đảm bảo bạn đã import **Best HTTP** package vào Unity project.

### 2. Import Dependencies

- **UniTask**: Đã có sẵn trong project
- **TypeFactory**: Đã có sẵn trong `Assets/GameNetworking/TypeCreator/`

### 3. Copy WebRequestService Folder

Copy toàn bộ folder `WebRequestService` vào project của bạn.

## 📖 Hướng Dẫn Sử Dụng

### 1. Khởi Tạo Service

```csharp
using PracticalModules.WebRequestService.Core;
using PracticalModules.WebRequestService.Models;

// Tạo configuration
var config = new WebRequestConfig
{
    baseUrl = "https://api.example.com",
    defaultTimeoutMs = 30000,
    maxRetries = 3,
    retryDelayMs = 1000,
    useExponentialBackoff = true,
    enableLogging = true,
    logRequestBody = false,
    logResponseBody = true
};

// Khởi tạo service
var webRequestService = new WebRequestService(config);
```

### 2. Tạo Request và Response Models

#### Request Model

```csharp
using System;

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
    public string deviceId;
}
```

#### Response Model

```csharp
using System;
using PracticalModules.WebRequestService.Attributes;
using PracticalModules.WebRequestService.Models;

[Endpoint("/api/v1/auth/login", "User Login")]
[Serializable]
public class LoginResponse : BaseResponse
{
    public string token;
    public string refreshToken;
    public UserData userData;
    
    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        this.token = null;
        this.refreshToken = null;
        this.userData = null;
    }
}

[Serializable]
public class UserData
{
    public string userId;
    public string username;
    public string email;
}
```

### 3. Thực Hiện Requests

#### GET Request

```csharp
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public async UniTaskVoid DoGetRequest()
{
    try
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer YOUR_TOKEN" }
        };
        
        var response = await webRequestService.GetAsync<LoginResponse>(
            url: "/api/v1/user/profile",
            headers: headers
        );
        
        if (response != null && response.IsSuccess)
        {
            Debug.Log($"Success: {response.userData.username}");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error: {ex.Message}");
    }
}
```

#### POST Request

```csharp
public async UniTaskVoid DoPostRequest()
{
    try
    {
        var requestBody = new LoginRequest
        {
            username = "testuser",
            password = "testpass",
            deviceId = SystemInfo.deviceUniqueIdentifier
        };
        
        var response = await webRequestService.PostAsync<LoginRequest, LoginResponse>(
            url: "/api/v1/auth/login",
            requestBody: requestBody
        );
        
        if (response != null && response.IsSuccess)
        {
            Debug.Log($"Login success: {response.token}");
            PlayerPrefs.SetString("auth_token", response.token);
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Login failed: {ex.Message}");
    }
}
```

#### PUT Request

```csharp
public async UniTaskVoid DoPutRequest()
{
    try
    {
        var requestBody = new UpdateProfileRequest
        {
            username = "newusername",
            email = "newemail@example.com"
        };
        
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}" }
        };
        
        var response = await webRequestService.PutAsync<UpdateProfileRequest, LoginResponse>(
            url: "/api/v1/user/profile",
            requestBody: requestBody,
            headers: headers
        );
        
        if (response != null && response.IsSuccess)
        {
            Debug.Log("Profile updated successfully");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Update failed: {ex.Message}");
    }
}
```

### 4. Sử Dụng Cancellation Token

```csharp
private CancellationTokenSource cancellationTokenSource;

public async UniTaskVoid DoRequestWithCancellation()
{
    this.cancellationTokenSource = new CancellationTokenSource();
    
    try
    {
        var response = await webRequestService.GetAsync<LoginResponse>(
            url: "/api/v1/data",
            cancellationToken: this.cancellationTokenSource.Token
        );
        
        // Process response...
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Request cancelled by user");
    }
    finally
    {
        this.cancellationTokenSource?.Dispose();
    }
}

public void CancelRequest()
{
    this.cancellationTokenSource?.Cancel();
}
```

### 5. Object Pooling

Object pooling được tự động xử lý bởi service, nhưng bạn có thể manually manage nếu cần:

```csharp
// Lấy response từ pool
var response = webRequestService.GetResponseFromPool<LoginResponse>();

// Sử dụng response...

// Trả về pool khi không dùng nữa
webRequestService.ReturnResponseToPool(response);

// Xem thông tin pool
Debug.Log(webRequestService.GetPoolInfo<LoginResponse>());

// Clear pool
webRequestService.ClearResponsePool<LoginResponse>();
```

## 📊 HTTP Status Codes

Service cung cấp static class `HttpStatusCode` chứa tất cả status codes:

```csharp
using PracticalModules.WebRequestService.Constants;

// Success codes
HttpStatusCode.Success                 // 200
HttpStatusCode.Created                 // 201
HttpStatusCode.NoContent              // 204

// Client error codes
HttpStatusCode.BadRequest             // 400
HttpStatusCode.Unauthorized           // 401
HttpStatusCode.Forbidden              // 403
HttpStatusCode.NotFound               // 404
HttpStatusCode.TooManyRequests        // 429

// Server error codes
HttpStatusCode.InternalServerError    // 500
HttpStatusCode.BadGateway            // 502
HttpStatusCode.ServiceUnavailable    // 503

// Custom codes
HttpStatusCode.NetworkError          // -1
HttpStatusCode.Cancelled             // -2
HttpStatusCode.ParseError            // -3

// Utility methods
bool isSuccess = HttpStatusCode.IsSuccess(statusCode);
bool isClientError = HttpStatusCode.IsClientError(statusCode);
bool isServerError = HttpStatusCode.IsServerError(statusCode);
string description = HttpStatusCode.GetDescription(statusCode);
```

## 🎨 EndpointAttribute

Sử dụng `EndpointAttribute` để configure endpoint cho response class:

```csharp
[Endpoint("/api/v1/user/login", "User Login")]
[Endpoint("/api/v1/user/profile", "User Profile", 
    Method = "GET",
    TimeoutMilliseconds = 15000,
    AllowRetry = true,
    MaxRetries = 5
)]
public class LoginResponse : BaseResponse
{
    // Response fields...
}
```

## ⚙️ Configuration

### WebRequestConfig Properties

```csharp
public class WebRequestConfig
{
    public string baseUrl;                  // Base URL cho API
    public int defaultTimeoutMs;            // Timeout mặc định (ms)
    public int maxRetries;                  // Số lần retry tối đa
    public int retryDelayMs;                // Delay giữa các retry (ms)
    public bool useExponentialBackoff;      // Sử dụng exponential backoff
    public bool enableLogging;              // Enable logging
    public bool logRequestBody;             // Log request body (security risk)
    public bool logResponseBody;            // Log response body
}
```

## 🔧 Advanced Usage

### Custom Error Handling

```csharp
public async UniTaskVoid DoRequestWithCustomErrorHandling()
{
    try
    {
        var response = await webRequestService.GetAsync<LoginResponse>(url: "/api/data");
        
        if (response == null)
        {
            Debug.LogError("Response is null");
            return;
        }
        
        if (!response.IsSuccess)
        {
            HandleError(response.statusCode, response.message);
            return;
        }
        
        // Process success response...
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Request cancelled");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Unexpected error: {ex.Message}");
    }
}

private void HandleError(int statusCode, string message)
{
    if (HttpStatusCode.IsClientError(statusCode))
    {
        Debug.LogWarning($"Client error: {HttpStatusCode.GetDescription(statusCode)}");
        
        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized:
                // Redirect to login
                break;
            case HttpStatusCode.Forbidden:
                // Show permission denied message
                break;
            default:
                // Show generic error
                break;
        }
    }
    else if (HttpStatusCode.IsServerError(statusCode))
    {
        Debug.LogError($"Server error: {HttpStatusCode.GetDescription(statusCode)}");
        // Show server error message
    }
}
```

### Dependency Injection (Testing)

```csharp
// Create mock implementation
public class MockWebRequest : IWebRequest
{
    public async UniTask<TResponse> GetAsync<TResponse>(...)
    {
        // Mock implementation
    }
    
    // Implement other methods...
}

// Use in tests
var mockRequest = new MockWebRequest();
var service = new WebRequestService(config, mockRequest);
```

## 🎯 Best Practices

1. **Luôn sử dụng try-catch** cho async calls
2. **Dispose CancellationTokenSource** sau khi sử dụng
3. **Không log sensitive data** (passwords, tokens) trong request body
4. **Sử dụng BaseResponse** cho tất cả response models
5. **Override OnReturnToPool()** để reset state đúng cách
6. **Clear pools** khi không cần nữa để free memory
7. **Sử dụng EndpointAttribute** để document API endpoints

## 🐛 Troubleshooting

### Request không hoạt động

- Kiểm tra `baseUrl` có đúng không
- Kiểm tra network connection
- Enable logging để xem chi tiết: `config.enableLogging = true`

### Parse error

- Kiểm tra JSON format từ server có đúng không
- Đảm bảo response model fields match với JSON keys
- Sử dụng `[SerializeField]` attribute nếu cần

### Memory leaks

- Đảm bảo call `OnReturnToPool()` override đúng cách
- Clear pools khi không dùng nữa
- Dispose CancellationTokenSource

## 📝 Changelog

### Version 1.0.0

- Initial release
- Support GET, POST, PUT methods
- Object pooling implementation
- TypeFactory integration
- Best HTTP integration
- Comprehensive error handling
- Status code constants

## 📄 License

MIT License - Free to use in any project.

## 🤝 Support

Nếu có vấn đề hoặc câu hỏi, vui lòng tạo issue hoặc liên hệ team.

