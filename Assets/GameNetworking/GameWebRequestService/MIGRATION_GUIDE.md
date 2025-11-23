# Migration Guide - New Response Architecture

## Tổng Quan

Hệ thống Web Request đã được refactor với kiến trúc mới linh động và minh bạch hơn:

- **BaseResponse classes**: Tách thành 3 loại riêng biệt cho GET, POST, PUT
- **Generic Response Data**: Mỗi response có generic type cho data structure
- **Abstract Callbacks**: OnResponseSuccess và OnResponseFailed methods
- **Auto Endpoint Resolution**: Tự động lấy endpoint từ EndpointAttribute
- **Simplified API**: Không cần truyền headers và URL nữa

---

## Kiến Trúc Mới

### 1. Base Response Classes

#### BaseGetResponse<TResponseData>
```csharp
[Endpoint("/api/v1/user/profile", "Get User Profile")]
public class ProfileGetResponse : BaseGetResponse<ProfileData>
{
    public override void OnResponseSuccess(ProfileData result)
    {
        // Xử lý khi GET thành công
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        // Xử lý khi GET thất bại
    }
}
```

#### BasePostResponse<TResponseData>
```csharp
[Endpoint("/api/v1/auth/login", "User Login")]
public class LoginResponse : BasePostResponse<LoginResponseData>
{
    public override void OnResponseSuccess(LoginResponseData result)
    {
        // Xử lý khi POST thành công
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        // Xử lý khi POST thất bại
    }
}
```

#### BasePutResponse<TResponseData>
```csharp
[Endpoint("/api/v1/user/profile", "Update User Profile")]
public class ProfileUpdateResponse : BasePutResponse<ProfileUpdateData>
{
    public override void OnResponseSuccess(ProfileUpdateData result)
    {
        // Xử lý khi PUT thành công
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        // Xử lý khi PUT thất bại
    }
}
```

---

## Migration Steps

### Old API (Legacy)

```csharp
// GET request
var response = await webRequestService.GetAsync<OldResponse>(
    url: "/api/v1/user/profile",
    headers: customHeaders,
    cancellationToken: token
);

// POST request
var response = await webRequestService.PostAsync<RequestModel, OldResponse>(
    url: "/api/v1/auth/login",
    requestBody: requestBody,
    headers: customHeaders,
    cancellationToken: token
);

// PUT request
var response = await webRequestService.PutAsync<RequestModel, OldResponse>(
    url: "/api/v1/user/profile",
    requestBody: requestBody,
    headers: customHeaders,
    cancellationToken: token
);
```

### New API (Recommended)

```csharp
// GET request - endpoint tự động từ attribute
var response = await webRequestService.GetAsync<ProfileGetResponse>(
    cancellationToken: token
);

// POST request - chỉ cần requestBody
var response = await webRequestService.PostAsync<LoginRequest, LoginResponse>(
    requestBody: requestBody,
    cancellationToken: token
);

// PUT request - chỉ cần requestBody
var response = await webRequestService.PutAsync<UpdateRequest, ProfileUpdateResponse>(
    requestBody: requestBody,
    cancellationToken: token
);

// Process response với callbacks
if (response != null)
{
    response.ProcessResponse(); // Tự động gọi OnResponseSuccess hoặc OnResponseFailed
}
```

---

## Lợi Ích Của Kiến Trúc Mới

### 1. Separation of Concerns
- Mỗi HTTP method có base response class riêng
- Dễ dàng customize behavior cho từng loại request

### 2. Type Safety
- Generic type constraint đảm bảo type safety
- Compile-time checking cho response data structure

### 3. Cleaner API
- Không cần truyền URL thủ công
- Endpoint được quản lý centralized qua attribute
- Không cần truyền headers (có thể config global)

### 4. Better Error Handling
- Abstract methods force implementation của error handling
- Consistent error handling pattern across all responses

### 5. Extensibility
- Dễ dàng extend base classes với custom logic
- Override OnReturnToPool() cho custom cleanup

---

## Example: Complete Migration

### Old Code

```csharp
public class OldLoginResponse
{
    public int statusCode;
    public string message;
    public string token;
    public UserData userData;
}

public async UniTask Login()
{
    var response = await webRequestService.PostAsync<LoginRequest, OldLoginResponse>(
        url: "https://api.example.com/api/v1/auth/login",
        requestBody: new LoginRequest { username = "test", password = "test123" },
        headers: new Dictionary<string, string> { { "Content-Type", "application/json" } }
    );
    
    if (response != null && response.statusCode == 200)
    {
        // Manual success handling
        PlayerPrefs.SetString("token", response.token);
    }
    else
    {
        // Manual error handling
        Debug.LogError($"Login failed: {response?.message}");
    }
}
```

### New Code

```csharp
// 1. Define response data structure
[Serializable]
public class LoginResponseData
{
    public string token;
    public string refreshToken;
    public UserData userData;
    public long expiresAt;
}

// 2. Create response class với EndpointAttribute
[Endpoint("/api/v1/auth/login", "User Login")]
[Serializable]
public class LoginResponse : BasePostResponse<LoginResponseData>
{
    public override void OnResponseSuccess(LoginResponseData result)
    {
        // Automatic success handling
        Debug.Log($"Login successful! Token: {result.token}");
        PlayerPrefs.SetString("token", result.token);
        PlayerPrefs.SetString("refresh_token", result.refreshToken);
        PlayerPrefs.Save();
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        // Automatic error handling
        Debug.LogError($"Login failed! Code: {errorCode}, Message: {errorMessage}");
        
        switch (errorCode)
        {
            case 401:
                Debug.LogError("Invalid credentials");
                break;
            case 403:
                Debug.LogError("Account locked");
                break;
            case 429:
                Debug.LogError("Too many attempts");
                break;
        }
    }
}

// 3. Use simplified API
public async UniTask Login()
{
    var response = await webRequestService.PostAsync<LoginRequest, LoginResponse>(
        requestBody: new LoginRequest { username = "test", password = "test123" }
    );
    
    // Single line để process response
    response?.ProcessResponse();
}
```

---

## Best Practices

### 1. Always Use EndpointAttribute

```csharp
// ✅ Good
[Endpoint("/api/v1/users", "User Management")]
public class UserResponse : BaseGetResponse<UserData> { }

// ❌ Bad - sẽ throw exception
public class UserResponse : BaseGetResponse<UserData> { }
```

### 2. Implement Both Callbacks

```csharp
// ✅ Good - implement cả success và failed
public override void OnResponseSuccess(UserData result)
{
    Debug.Log("Success!");
}

public override void OnResponseFailed(int errorCode, string errorMessage)
{
    Debug.LogError($"Failed: {errorCode}");
}

// ❌ Bad - không implement callbacks
// Compiler sẽ báo lỗi vì abstract methods
```

### 3. Use ProcessResponse()

```csharp
// ✅ Good - tự động gọi đúng callback
var response = await webRequestService.GetAsync<UserResponse>();
response?.ProcessResponse();

// ⚠️ OK - manual handling nếu cần logic phức tạp
if (response != null)
{
    if (response.IsSuccess && response.data != null)
    {
        // Custom logic
        response.OnResponseSuccess(response.data);
    }
    else
    {
        response.OnResponseFailed(response.statusCode, response.message);
    }
}
```

### 4. Clean Up Resources

```csharp
public override void OnReturnToPool()
{
    base.OnReturnToPool(); // Call base first
    
    // Custom cleanup
    this.customData = null;
    this.cachedValue = default;
}
```

---

## Backward Compatibility

**Legacy BaseResponse vẫn được support** cho compatibility:

```csharp
// Old code vẫn hoạt động
public class LegacyResponse : BaseResponse
{
    public string data;
}

var response = await webRequestService.GetAsync<LegacyResponse>(
    url: "/api/v1/legacy",
    headers: customHeaders
);
```

Nhưng **khuyến khích migrate sang new architecture** để có:
- Better type safety
- Cleaner code
- Automatic error handling
- Better maintainability

---

## Summary

| Feature | Old API | New API |
|---------|---------|---------|
| **Endpoint** | Truyền URL string | Tự động từ attribute |
| **Headers** | Truyền Dictionary | Config global (optional) |
| **Error Handling** | Manual check statusCode | Abstract callbacks |
| **Type Safety** | Loose typing | Strong generic typing |
| **Separation** | Single BaseResponse | 3 base classes (GET/POST/PUT) |
| **Callbacks** | None | OnResponseSuccess/OnResponseFailed |

---

## Hỗ Trợ

Nếu gặp vấn đề trong quá trình migration:

1. Check ARCHITECTURE.md để hiểu rõ design
2. Xem examples trong Examples/ folder
3. Đọc XML documentation trong source code
4. Test với mock implementation trước khi deploy

Happy Coding! 🚀

