using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace GameNetworking.RequestOptimizer.Scripts.Utilities
{
    /// <summary>
    /// Centralized JSON serialization utilities với Newtonsoft.Json
    /// Cung cấp consistent settings và helper methods cho toàn bộ hệ thống
    /// </summary>
    public static class JsonSerializer
    {
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings;
        private static readonly JsonSerializerSettings CompactJsonSerializerSettings;
        private static readonly JsonSerializerSettings IndentedJsonSerializerSettings;
        
        static JsonSerializer()
        {
            DefaultJsonSerializerSettings = CreateDefaultSettings();
            CompactJsonSerializerSettings = CreateCompactSettings();
            IndentedJsonSerializerSettings = CreateIndentedSettings();
        }
        
        /// <summary>
        /// Default JSON serializer settings cho hệ thống
        /// </summary>
        public static JsonSerializerSettings defaultJsonSerializerSettings => DefaultJsonSerializerSettings;
        
        /// <summary>
        /// Compact settings (no formatting) cho production
        /// </summary>
        public static JsonSerializerSettings CompactSettings => CompactJsonSerializerSettings;
        
        /// <summary>
        /// Indented settings (với formatting) cho debugging
        /// </summary>
        public static JsonSerializerSettings indentedJsonSerializerSettings => IndentedJsonSerializerSettings;
        
        /// <summary>
        /// Serialize object thành JSON string với default settings
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, DefaultJsonSerializerSettings);
        }
        
        /// <summary>
        /// Serialize object thành JSON string với compact format
        /// </summary>
        public static string SerializeCompact(object obj)
        {
            return JsonConvert.SerializeObject(obj, CompactJsonSerializerSettings);
        }
        
        /// <summary>
        /// Serialize object thành JSON string với indented format (cho debugging)
        /// </summary>
        public static string SerializeIndented(object obj)
        {
            return JsonConvert.SerializeObject(obj, IndentedJsonSerializerSettings);
        }
        
        /// <summary>
        /// Deserialize JSON string thành object
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            
            try
            {
                return JsonConvert.DeserializeObject<T>(json, DefaultJsonSerializerSettings);
            }
            catch (JsonException ex)
            {
                UnityEngine.Debug.LogError($"[JsonSerializer] Failed to deserialize: {ex.Message}");
                return default;
            }
        }
        
        /// <summary>
        /// Deserialize JSON string thành object với error handling
        /// </summary>
        public static bool TryDeserialize<T>(string json, out T result)
        {
            result = default;
            
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }
            
            try
            {
                result = JsonConvert.DeserializeObject<T>(json, DefaultJsonSerializerSettings);
                return result != null;
            }
            catch (JsonException ex)
            {
                UnityEngine.Debug.LogError($"[JsonSerializer] Failed to deserialize: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Deserialize JSON string thành dynamic object
        /// </summary>
        public static dynamic DeserializeDynamic(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            
            try
            {
                return JsonConvert.DeserializeObject(json, DefaultJsonSerializerSettings);
            }
            catch (JsonException ex)
            {
                UnityEngine.Debug.LogError($"[JsonSerializer] Failed to deserialize dynamic: {ex.Message}");
                return null;
            }
        }
        
        private static JsonSerializerSettings CreateDefaultSettings()
        {
            return new JsonSerializerSettings
            {
                // Basic settings
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                
                // Contract resolver
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                
                // Type handling
                TypeNameHandling = TypeNameHandling.None,
                
                // Converters
                Converters =
                {
                    new StringEnumConverter()
                },
                
                // Date handling
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                
                // Error handling
                Error = (sender, args) =>
                {
                    UnityEngine.Debug.LogWarning($"[JsonSerializer] Serialization error: {args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                }
            };
        }
        
        private static JsonSerializerSettings CreateCompactSettings()
        {
            var settings = CreateDefaultSettings();
            settings.Formatting = Formatting.None;
            return settings;
        }
        
        private static JsonSerializerSettings CreateIndentedSettings()
        {
            var settings = CreateDefaultSettings();
            settings.Formatting = Formatting.Indented;
            return settings;
        }
    }
}

