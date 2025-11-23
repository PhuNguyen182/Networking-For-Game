# Best HTTP API Implementation Notes

## API Changes in Best HTTP v3.x

### Upload Data (POST/PUT Request Body)

Trong Best HTTP version mới (v3.x), cách set request body đã thay đổi:

#### ❌ Old API (v2.x) - KHÔNG HOẠT ĐỘNG
```csharp
request.RawData = Encoding.UTF8.GetBytes(jsonBody);
```

#### ✅ New API (v3.x) - ĐÚNG CÁCH
```csharp
request.UploadSettings.UploadStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
```

### Timeout Settings

Timeout cũng được set khác:

#### ❌ Old API (v2.x)
```csharp
request.Timeout = TimeSpan.FromMilliseconds(timeout);
```

#### ✅ New API (v3.x)
```csharp
request.TimeoutSettings = new TimeoutSettings(request)
{
    Timeout = TimeSpan.FromMilliseconds(timeout)
};
```

## Implementation Details

### POST Request Body
```csharp
// Serialize to JSON
var jsonBody = JsonUtility.ToJson(requestBody);

// Convert to bytes
var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

// Create MemoryStream
var stream = new System.IO.MemoryStream(bodyBytes);

// Set upload stream
request.UploadSettings.UploadStream = stream;

// Set content type
request.SetHeader("Content-Type", "application/json");
```

### PUT Request Body
```csharp
// Same as POST
request.UploadSettings.UploadStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
request.SetHeader("Content-Type", "application/json");
```

## Namespace Changes

Best HTTP v3.x cũng thay đổi namespaces:

### ❌ Old Namespaces (v2.x)
```csharp
using BestHTTP;
```

### ✅ New Namespaces (v3.x)
```csharp
using Best.HTTP;
using Best.HTTP.Request.Settings;
```

## Full Example

```csharp
using System;
using System.IO;
using System.Text;
using Best.HTTP;
using Best.HTTP.Request.Settings;

public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
    string url,
    TRequest requestBody
) where TRequest : class
  where TResponse : class
{
    // Create request
    var request = new HTTPRequest(new Uri(url), HTTPMethods.Post);
    
    // Set timeout
    request.TimeoutSettings = new TimeoutSettings(request)
    {
        Timeout = TimeSpan.FromMilliseconds(30000)
    };
    
    // Serialize body
    var jsonBody = JsonUtility.ToJson(requestBody);
    var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
    
    // Set upload stream
    request.UploadSettings.UploadStream = new MemoryStream(bodyBytes);
    
    // Set headers
    request.SetHeader("Content-Type", "application/json");
    
    // Send request
    request.Send();
    
    // Wait for response...
}
```

## Key Points

1. **UploadSettings.UploadStream**: Thay thế cho `RawData`
2. **TimeoutSettings**: Thay thế cho direct `Timeout` property
3. **Namespaces**: `Best.HTTP` thay vì `BestHTTP`
4. **MemoryStream**: Phải wrap bytes trong MemoryStream

## Migration Checklist

Nếu upgrade từ Best HTTP v2.x sang v3.x:

- [ ] Update namespaces (`BestHTTP` → `Best.HTTP`)
- [ ] Update timeout setting (`Timeout` → `TimeoutSettings`)
- [ ] Update upload data (`RawData` → `UploadSettings.UploadStream`)
- [ ] Test all POST/PUT requests
- [ ] Verify JSON serialization works
- [ ] Check error handling still works

## Reference

- **Best HTTP Documentation**: https://besthttp.documentation.help/
- **Version**: 3.x
- **Unity Version**: 2021.3+

---

**Last Updated**: November 23, 2024  
**Implementation**: BestHttpWebRequest.cs

