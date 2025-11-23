# Quick Start Guide

## 🎯 Installation

1. Import **Best HTTP** package
2. Import **Newtonsoft.Json** package
3. Import **UniTask** package
4. Copy `GameWebRequestService` folder vào project

---

## 🚀 Basic Usage

### Step 1: Define Response Data

```csharp
using System;

[Serializable]
public class UserData
{
    public string userId;
    public string username;
    public string email;
    public int level;
}
```

### Step 2: Create Response Class

```csharp
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Models;
using UnityEngine;

[Endpoint("/api/v1/user/profile", "Get User Profile")]
public class ProfileGetResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result)
    {
        Debug.Log($"✅ Success! Username: {result.username}");
        Debug.Log($"✅ Email: {result.email}");
        Debug.Log($"✅ Level: {result.level}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"❌ Failed! Code: {errorCode}");
        Debug.LogError($"❌ Message: {errorMessage}");
        
        switch (errorCode)
        {
            case 401:
                Debug.LogError("❌ Unauthorized - please login");
                break;
            case 404:
                Debug.LogError("❌ User not found");
                break;
            default:
                Debug.LogError($"❌ Unexpected error");
                break;
        }
    }
}
```

### Step 3: Initialize Service

```csharp
using GameNetworking.GameWebRequestService.Core;
using GameNetworking.GameWebRequestService.Models;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private WebRequestService webRequestService;
    
    void Start()
    {
        // Create config
        var config = new WebRequestConfig
        {
            baseUrl = "https://api.example.com",
            defaultTimeoutMs = 30000,
            enableLogging = true
        };
        
        // Initialize service
        webRequestService = new WebRequestService(config);
        
        Debug.Log("✅ Web Request Service initialized");
    }
}
```

### Step 4: Make Request

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    private WebRequestService webRequestService;
    private CancellationTokenSource cancellationTokenSource;
    
    public async UniTaskVoid GetUserProfile()
    {
        cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // Make GET request (requestBody = null)
            var response = await webRequestService.GetAsync<object, ProfileGetResponse>(
                requestBody: null,
                cancellationToken: cancellationTokenSource.Token
            );
            
            // Process response (automatically calls OnResponseSuccess or OnResponseFailed)
            response?.ProcessResponse();
        }
        catch (System.OperationCanceledException)
        {
            Debug.LogWarning("⚠️ Request cancelled");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception: {ex.Message}");
        }
        finally
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }
    
    void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
```

---

## 📝 POST Request Example

### Step 1: Create Request Model

```csharp
[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
    public string deviceId;
}
```

### Step 2: Create Response Data

```csharp
[Serializable]
public class LoginData
{
    public string token;
    public string refreshToken;
    public UserData userData;
    public long expiresAt;
}
```

### Step 3: Create Response Class

```csharp
[Endpoint("/api/v1/auth/login", "User Login")]
public class LoginResponse : BasePostResponse<LoginData>
{
    public override void OnResponseSuccess(LoginData result)
    {
        // Save tokens
        PlayerPrefs.SetString("auth_token", result.token);
        PlayerPrefs.SetString("refresh_token", result.refreshToken);
        PlayerPrefs.Save();
        
        Debug.Log($"✅ Login successful! Welcome {result.userData.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"❌ Login failed: {errorCode} - {errorMessage}");
    }
}
```

### Step 4: Make POST Request

```csharp
public async UniTaskVoid Login(string username, string password)
{
    var requestBody = new LoginRequest
    {
        username = username,
        password = password,
        deviceId = SystemInfo.deviceUniqueIdentifier
    };
    
    var response = await webRequestService.PostAsync<LoginRequest, LoginResponse>(
        requestBody: requestBody,
        cancellationToken: cancellationTokenSource.Token
    );
    
    response?.ProcessResponse();
}
```

---

## 🔧 Configuration Options

```csharp
var config = new WebRequestConfig
{
    // Required
    baseUrl = "https://api.example.com",
    
    // Optional
    defaultTimeoutMs = 30000,           // 30 seconds (default)
    maxRetries = 3,                     // Retry 3 times
    retryDelayMs = 1000,                // 1 second delay
    useExponentialBackoff = true,       // Enable backoff
    enableLogging = true,               // Debug logs
    logRequestBody = false,             // Don't log passwords!
    logResponseBody = true              // Log responses
};
```

---

## 💡 Tips

### Use ProcessResponse() for Automatic Handling

```csharp
// ✅ Recommended - automatic callback
var response = await service.GetAsync<object, UserResponse>(null);
response?.ProcessResponse(); // Calls OnResponseSuccess or OnResponseFailed

// ⚠️ Manual handling - only if you need custom logic
var response = await service.GetAsync<object, UserResponse>(null);
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

### Always Clean Up

```csharp
void OnDestroy()
{
    // Cancel pending requests
    cancellationTokenSource?.Cancel();
    cancellationTokenSource?.Dispose();
    
    // Clear pools (optional)
    webRequestService?.ClearAllResponsePools();
}
```

---

## 🐛 Common Issues

### Issue 1: "Type does not have EndpointAttribute"

**Fix**: Add `[Endpoint]` attribute to your response class

```csharp
[Endpoint("/api/v1/users", "Get User")] // Add this!
public class UserResponse : BaseGetResponse<UserData> { }
```

### Issue 2: "Cannot create instance of abstract class"

**Fix**: Implement `OnResponseSuccess` và `OnResponseFailed`

```csharp
public class UserResponse : BaseGetResponse<UserData>
{
    // Must implement these!
    public override void OnResponseSuccess(UserData result) { }
    public override void OnResponseFailed(int errorCode, string errorMessage) { }
}
```

---

## 📚 Next Steps

1. ✅ Check **[README.md](README.md)** for complete documentation
2. ✅ See **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** for API reference
3. ✅ Browse **Examples/** folder for more examples
4. ✅ Read **[NEW_ARCHITECTURE.md](NEW_ARCHITECTURE.md)** for architecture details

---

**Happy Coding!** 🚀
