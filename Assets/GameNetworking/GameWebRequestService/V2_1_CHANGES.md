# Version 2.1.0 Changes

## 🎉 New Features

### 1. GET Request Body Support

**Feature**: GET requests hiện có thể truyền requestBody (optional)

#### Before (v2.0.0)
```csharp
// GET không có requestBody
var response = await webRequestService.GetAsync<ProfileGetResponse>(
    cancellationToken: token
);
```

#### After (v2.1.0)
```csharp
// GET có thể có requestBody (optional - truyền null nếu không cần)
var response = await webRequestService.GetAsync<GetProfileRequest, ProfileGetResponse>(
    requestBody: new GetProfileRequest { userId = "123", includeDetails = true },
    cancellationToken: token
);

// Hoặc truyền null nếu không cần body
var response = await webRequestService.GetAsync<GetProfileRequest, ProfileGetResponse>(
    requestBody: null,
    cancellationToken: token
);
```

**Why?**
- Một số API servers yêu cầu complex query parameters
- RESTful APIs đôi khi sử dụng GET với body cho advanced filtering
- Flexible hơn cho các use cases đặc biệt

**Note**: GET với body không phải là REST standard nhưng được hỗ trợ bởi Best HTTP.

---

### 2. Newtonsoft.Json Integration

**Feature**: Thay thế Unity's JsonUtility bằng Newtonsoft.Json (Json.NET)

#### Benefits

1. **Better Serialization**
   - Support complex types (Dictionary, Hashtable, etc.)
   - Support private fields và properties
   - Custom converters và serialization settings

2. **More Features**
   - JsonProperty attributes cho field mapping
   - JsonIgnore cho fields cần skip
   - Custom date formatting
   - Circular reference handling

3. **Better Error Messages**
   - Clear error messages khi serialization fails
   - Line number và character position trong JSON

4. **Industry Standard**
   - Widely used trong .NET ecosystem
   - Better documentation và community support

#### API Changes

**Serialization**:
```csharp
// Before (JsonUtility)
string json = JsonUtility.ToJson(requestBody);

// After (Newtonsoft.Json)
string json = JsonConvert.SerializeObject(requestBody);
```

**Deserialization**:
```csharp
// Before (JsonUtility)
var response = JsonUtility.FromJson<TResponse>(json);

// After (Newtonsoft.Json)
var response = JsonConvert.DeserializeObject<TResponse>(json);
```

#### Example với Attributes

```csharp
using Newtonsoft.Json;

[Serializable]
public class UserData
{
    [JsonProperty("user_id")] // Map từ snake_case sang camelCase
    public string userId;
    
    [JsonProperty("user_name")]
    public string username;
    
    [JsonIgnore] // Skip khi serialize
    public string internalCache;
    
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string optionalField; // Không serialize nếu null
}
```

---

## 🔄 Migration Guide

### For GET Requests

#### Option 1: No Request Body (Simplest)

```csharp
// Nếu GET của bạn không cần body, truyền null
var response = await webRequestService.GetAsync<object, ProfileGetResponse>(
    requestBody: null,
    cancellationToken: token
);
```

#### Option 2: With Request Body

```csharp
// 1. Define request model
[Serializable]
public class GetProfileRequest
{
    public string userId;
    public bool includeDetails;
}

// 2. Use in GET request
var response = await webRequestService.GetAsync<GetProfileRequest, ProfileGetResponse>(
    requestBody: new GetProfileRequest { userId = "123" },
    cancellationToken: token
);
```

### For JsonUtility → Newtonsoft.Json

**No code changes needed!** 

Serialization và deserialization đều được handle internally. Nếu bạn có custom JSON handling code, consider using Newtonsoft.Json attributes:

```csharp
// Old approach
[Serializable]
public class UserData
{
    public string user_id; // Must match JSON field name exactly
}

// New approach với Newtonsoft.Json
[Serializable]
public class UserData
{
    [JsonProperty("user_id")] // Map JSON field to C# property
    public string userId;
    
    [JsonIgnore]
    public string computedField; // Won't be serialized
}
```

---

## 📚 Updated API Signatures

### IWebRequest Interface

```csharp
public interface IWebRequest
{
    // GET - Now with optional requestBody
    UniTask<TResponse> GetAsync<TRequest, TResponse>(
        string url,
        TRequest requestBody = null,
        Dictionary<string, string> headers = null,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;
    
    // POST - Unchanged
    UniTask<TResponse> PostAsync<TRequest, TResponse>(
        string url,
        TRequest requestBody,
        Dictionary<string, string> headers = null,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;
    
    // PUT - Unchanged
    UniTask<TResponse> PutAsync<TRequest, TResponse>(
        string url,
        TRequest requestBody,
        Dictionary<string, string> headers = null,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;
}
```

### WebRequestService

```csharp
public class WebRequestService
{
    // GET - Now requires TRequest type parameter
    public async UniTask<TResponse> GetAsync<TRequest, TResponse>(
        TRequest requestBody = null,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;
    
    // POST - Unchanged
    public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
        TRequest requestBody,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;
    
    // PUT - Unchanged
    public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
        TRequest requestBody,
        CancellationToken cancellationToken = default
    ) where TRequest : class
      where TResponse : class;
}
```

---

## 🎯 Examples

### Example 1: GET Without Body

```csharp
[Endpoint("/api/v1/users/{id}", "Get User")]
public class UserGetResponse : BaseGetResponse<UserData>
{
    public override void OnResponseSuccess(UserData result)
    {
        Debug.Log($"User: {result.username}");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"Failed: {errorCode}");
    }
}

// Usage - truyền null cho requestBody
var response = await webRequestService.GetAsync<object, UserGetResponse>(
    requestBody: null
);
response?.ProcessResponse();
```

### Example 2: GET With Body

```csharp
[Serializable]
public class SearchRequest
{
    public string query;
    public int maxResults;
    public string[] filters;
}

[Endpoint("/api/v1/search", "Search")]
public class SearchResponse : BaseGetResponse<SearchResults>
{
    public override void OnResponseSuccess(SearchResults result)
    {
        Debug.Log($"Found {result.totalCount} results");
    }
    
    public override void OnResponseFailed(int errorCode, string errorMessage)
    {
        Debug.LogError($"Search failed: {errorCode}");
    }
}

// Usage - với requestBody
var searchRequest = new SearchRequest
{
    query = "unity",
    maxResults = 10,
    filters = new[] { "tutorial", "documentation" }
};

var response = await webRequestService.GetAsync<SearchRequest, SearchResponse>(
    requestBody: searchRequest
);
response?.ProcessResponse();
```

### Example 3: Complex JSON với Newtonsoft.Json

```csharp
using Newtonsoft.Json;

[Serializable]
public class ComplexRequest
{
    [JsonProperty("user_id")]
    public string userId;
    
    [JsonProperty("settings")]
    public Dictionary<string, object> settings;
    
    [JsonProperty("created_at")]
    public DateTime createdAt;
    
    [JsonIgnore]
    public string internalData; // Won't be serialized
}

// Newtonsoft.Json tự động handle Dictionary, DateTime, etc.
var request = new ComplexRequest
{
    userId = "123",
    settings = new Dictionary<string, object>
    {
        { "theme", "dark" },
        { "notifications", true }
    },
    createdAt = DateTime.UtcNow
};

var response = await webRequestService.PostAsync<ComplexRequest, ComplexResponse>(
    requestBody: request
);
```

---

## ⚠️ Breaking Changes

### GET API Signature Changed

**Impact**: Tất cả GET requests cần update để include TRequest type parameter

**Before (v2.0.0)**:
```csharp
var response = await webRequestService.GetAsync<ProfileGetResponse>();
```

**After (v2.1.0)**:
```csharp
// Option 1: Explicit null
var response = await webRequestService.GetAsync<object, ProfileGetResponse>(
    requestBody: null
);

// Option 2: With request body
var response = await webRequestService.GetAsync<GetProfileRequest, ProfileGetResponse>(
    requestBody: new GetProfileRequest()
);
```

### Recommendation

Để minimize breaking changes, create extension method:

```csharp
public static class WebRequestServiceExtensions
{
    // Backward compatible extension
    public static async UniTask<TResponse> GetAsync<TResponse>(
        this WebRequestService service,
        CancellationToken cancellationToken = default
    ) where TResponse : class
    {
        return await service.GetAsync<object, TResponse>(
            requestBody: null,
            cancellationToken: cancellationToken
        );
    }
}

// Usage - giống v2.0.0
var response = await webRequestService.GetAsync<ProfileGetResponse>();
```

---

## 📊 Performance Impact

### Newtonsoft.Json vs JsonUtility

| Metric | JsonUtility | Newtonsoft.Json | Note |
|--------|-------------|-----------------|------|
| Serialization Speed | Faster (~1.5x) | Slightly slower | Acceptable tradeoff |
| Deserialization Speed | Faster (~1.5x) | Slightly slower | Acceptable tradeoff |
| Features | Limited | Comprehensive | More flexibility |
| Error Messages | Basic | Detailed | Better debugging |
| Complex Types | Not supported | Supported | Critical feature |

**Conclusion**: Slight performance cost (~33% slower) nhưng có nhiều features hơn rất nhiều. Tradeoff là worth it.

---

## 🐛 Bug Fixes

None - pure feature addition.

---

## 📝 Documentation Updates

- Updated README.md với GET requestBody examples
- Updated QUICK_REFERENCE.md
- Updated MIGRATION_GUIDE.md
- Created V2_1_CHANGES.md (this file)

---

## 🔮 Future Plans (v2.2.0)

- [ ] Support cho custom JsonSerializerSettings
- [ ] Attribute-based JSON configuration
- [ ] Response compression support
- [ ] Request/Response interceptors
- [ ] Global headers configuration

---

**Version**: 2.1.0  
**Release Date**: November 23, 2024  
**Breaking Changes**: Yes (GET API signature)  
**Migration Difficulty**: Low (simple type parameter addition)  
**Status**: ✅ Production Ready

