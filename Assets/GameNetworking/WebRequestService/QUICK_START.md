# Quick Start Guide

Hướng dẫn nhanh để bắt đầu sử dụng Web Request Service trong 5 phút.

## 📦 Installation

### Bước 1: Kiểm tra Dependencies

Đảm bảo project có các packages sau:

- ✅ **Best HTTP** - HTTP client library
- ✅ **UniTask** - Zero-allocation async/await
- ✅ **TypeFactory** - High-performance object creation (đã có trong project)

### Bước 2: Import WebRequestService

Folder `WebRequestService` đã sẵn sàng tại:
```
Assets/GameNetworking/WebRequestService/
```

## 🚀 Basic Setup (30 giây)

### 1. Tạo Service Instance

```csharp
using PracticalModules.WebRequestService.Core;
using PracticalModules.WebRequestService.Models;

public class MyGameManager : MonoBehaviour
{
    private WebRequestService webRequestService;
    
    void Start()
    {
        // Tạo config
        var config = new WebRequestConfig
        {
            baseUrl = "https://your-api.com",  // ⚠️ Thay bằng API URL của bạn
            defaultTimeoutMs = 30000,
            enableLogging = true
        };
        
        // Khởi tạo service
        this.webRequestService = new WebRequestService(config);
        
        Debug.Log("WebRequestService ready!");
    }
}
```

## 📝 Create Models (2 phút)

### Request Model

```csharp
using System;

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}
```

### Response Model

```csharp
using System;
using PracticalModules.WebRequestService.Attributes;
using PracticalModules.WebRequestService.Models;

[Endpoint("/api/auth/login", "Login API")]
[Serializable]
public class LoginResponse : BaseResponse
{
    public string token;
    public string userId;
    
    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        this.token = null;
        this.userId = null;
    }
}
```

## 🎯 Make Your First Request (1 phút)

### GET Request

```csharp
using Cysharp.Threading.Tasks;

public async UniTaskVoid GetUserProfile()
{
    try
    {
        var response = await this.webRequestService.GetAsync<LoginResponse>(
            url: "/api/user/profile"
        );
        
        if (response != null && response.IsSuccess)
        {
            Debug.Log($"Success! Token: {response.token}");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error: {ex.Message}");
    }
}
```

### POST Request

```csharp
public async UniTaskVoid Login()
{
    try
    {
        var request = new LoginRequest
        {
            username = "testuser",
            password = "testpass"
        };
        
        var response = await this.webRequestService.PostAsync<LoginRequest, LoginResponse>(
            url: "/api/auth/login",
            requestBody: request
        );
        
        if (response != null && response.IsSuccess)
        {
            Debug.Log($"Login successful! Token: {response.token}");
            // Save token
            PlayerPrefs.SetString("auth_token", response.token);
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Login failed: {ex.Message}");
    }
}
```

### PUT Request

```csharp
public async UniTaskVoid UpdateProfile()
{
    try
    {
        var request = new UpdateProfileRequest
        {
            username = "newname",
            email = "new@email.com"
        };
        
        var response = await this.webRequestService.PutAsync<UpdateProfileRequest, LoginResponse>(
            url: "/api/user/profile",
            requestBody: request
        );
        
        if (response != null && response.IsSuccess)
        {
            Debug.Log("Profile updated!");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Update failed: {ex.Message}");
    }
}
```

## 🔐 Add Authentication (30 giây)

```csharp
using System.Collections.Generic;

public async UniTaskVoid AuthenticatedRequest()
{
    var headers = new Dictionary<string, string>
    {
        { "Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}" }
    };
    
    var response = await this.webRequestService.GetAsync<LoginResponse>(
        url: "/api/protected/resource",
        headers: headers
    );
    
    // Process response...
}
```

## 🎮 In Unity Editor

### 1. Attach Script to GameObject

```
1. Create empty GameObject: "GameManager"
2. Add Component → MyGameManager
3. Press Play
```

### 2. Test from Inspector

```csharp
// Add buttons in Inspector
[SerializeField] private Button loginButton;
[SerializeField] private Button profileButton;

void Start()
{
    this.loginButton.onClick.AddListener(() => this.Login().Forget());
    this.profileButton.onClick.AddListener(() => this.GetUserProfile().Forget());
}
```

## ✅ Success Checklist

Sau 5 phút, bạn đã có:

- [x] Service đã setup
- [x] Config đã tạo
- [x] Models đã define
- [x] First request thành công
- [x] Hiểu cách dùng GET, POST, PUT
- [x] Biết cách add authentication

## 🎯 Next Steps

### Học thêm tính năng advanced:

1. **Cancellation**
   ```csharp
   private CancellationTokenSource cts = new CancellationTokenSource();
   
   await service.GetAsync<Response>(url, cancellationToken: cts.Token);
   
   // Cancel
   cts.Cancel();
   ```

2. **Error Handling**
   ```csharp
   if (response == null || !response.IsSuccess)
   {
       Debug.LogError($"Failed: Status {response?.statusCode}");
       return;
   }
   ```

3. **Object Pooling**
   ```csharp
   // Automatic! Service handles pooling internally
   ```

## 📚 More Resources

- **README.md** - Complete user guide
- **ARCHITECTURE.md** - Design documentation
- **Examples/** - Working code samples
- **Tests/** - Test examples

## 💡 Tips

### ✅ DO's

- ✅ Use try-catch cho all requests
- ✅ Check `response.IsSuccess` before processing
- ✅ Dispose CancellationTokenSource
- ✅ Enable logging khi debug: `config.enableLogging = true`

### ❌ DON'Ts

- ❌ Không log sensitive data (passwords, tokens)
- ❌ Không block main thread
- ❌ Không forget to cleanup resources
- ❌ Không hardcode URLs (use config)

## 🐛 Troubleshooting

### Issue: Request không hoạt động

**Solution:**
```csharp
// 1. Check baseUrl
config.baseUrl = "https://your-api.com";  // No trailing slash

// 2. Enable logging
config.enableLogging = true;

// 3. Check Unity Console for errors
```

### Issue: Parse error

**Solution:**
```csharp
// Ensure response model matches JSON
[Serializable]
public class MyResponse : BaseResponse
{
    public string fieldName;  // Must match JSON key
}

// Enable response logging to see actual JSON
config.logResponseBody = true;
```

### Issue: Timeout

**Solution:**
```csharp
// Increase timeout
config.defaultTimeoutMs = 60000;  // 60 seconds

// Or use EndpointAttribute for specific endpoint
[Endpoint("/slow-api", "Slow API", TimeoutMilliseconds = 120000)]
public class SlowResponse : BaseResponse { }
```

## 🎉 You're Ready!

Bây giờ bạn đã sẵn sàng sử dụng Web Request Service! 

Nếu cần help:
1. Check **README.md** cho detailed guide
2. Check **Examples/** cho working code
3. Check **Tests/** cho test examples
4. Check **ARCHITECTURE.md** cho design details

Happy coding! 🚀

