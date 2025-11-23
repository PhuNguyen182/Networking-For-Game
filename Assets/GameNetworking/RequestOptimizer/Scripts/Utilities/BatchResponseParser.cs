using System.Collections.Generic;
using GameNetworking.RequestOptimizer.Scripts.Models;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Utilities
{
    /// <summary>
    /// Parser cho batch response với hỗ trợ partial success
    /// </summary>
    public static class BatchResponseParser
    {
        /// <summary>
        /// Parse batch response từ server
        /// Hỗ trợ nhiều format khác nhau:
        /// 1. Standard format: { "results": [...], "success": true }
        /// 2. Simple array: [ {...}, {...}, ... ]
        /// 3. Custom format với success/failure arrays
        /// </summary>
        public static BatchResponseResult ParseBatchResponse(string responseJson, int expectedCount)
        {
            var result = new BatchResponseResult
            {
                totalRequests = expectedCount
            };
            
            if (string.IsNullOrEmpty(responseJson))
            {
                result.isSuccess = false;
                result.failedRequests = expectedCount;
                return result;
            }
            
            try
            {
                var jsonObject = JObject.Parse(responseJson);
                
                // Format 1: { "results": [...], "success": true }
                if (jsonObject.TryGetValue("results", out var resultsToken) && resultsToken is JArray resultsArray)
                {
                    return ParseStandardFormat(resultsArray, expectedCount);
                }
                
                // Format 2: { "successes": [...], "failures": [...] }
                if (jsonObject.TryGetValue("successes", out var successToken) && 
                    jsonObject.TryGetValue("failures", out var failureToken))
                {
                    return ParseSuccessFailureFormat(successToken as JArray, failureToken as JArray, expectedCount);
                }
                
                // Format 3: Simple array [ {...}, {...}, ... ]
                if (responseJson.TrimStart().StartsWith("["))
                {
                    var array = JArray.Parse(responseJson);
                    return ParseSimpleArrayFormat(array, expectedCount);
                }
                
                // Fallback: treat as single success
                result.isSuccess = true;
                result.successfulRequests = expectedCount;
                result.failedRequests = 0;
                
                for (var i = 0; i < expectedCount; i++)
                {
                    result.results.Add(new IndividualRequestResult(i, true, responseJson));
                }
                
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BatchResponseParser] Failed to parse batch response: {ex.Message}");
                
                // Fallback: treat as all failed
                result.isSuccess = false;
                result.failedRequests = expectedCount;
                
                for (var i = 0; i < expectedCount; i++)
                {
                    result.results.Add(new IndividualRequestResult(
                        i, false, null, $"Parse error: {ex.Message}", 500
                    ));
                }
                
                return result;
            }
        }
        
        private static BatchResponseResult ParseStandardFormat(JArray resultsArray, int expectedCount)
        {
            var result = new BatchResponseResult
            {
                totalRequests = expectedCount
            };
            
            for (var i = 0; i < resultsArray.Count; i++)
            {
                var item = resultsArray[i] as JObject;
                if (item == null)
                {
                    continue;
                }
                
                var success = item.Value<bool?>("success") ?? item.Value<bool?>("isSuccess") ?? false;
                var response = item.Value<string>("response") ?? item.Value<string>("data");
                var errorMessage = item.Value<string>("error") ?? item.Value<string>("errorMessage");
                var statusCode = item.Value<int?>("statusCode") ?? (success ? 200 : 500);
                
                result.results.Add(new IndividualRequestResult(i, success, response, errorMessage, statusCode));
                
                if (success)
                {
                    result.successfulRequests++;
                }
                else
                {
                    result.failedRequests++;
                }
            }
            
            result.isSuccess = result.successfulRequests > 0;
            return result;
        }
        
        private static BatchResponseResult ParseSuccessFailureFormat(JArray successArray, JArray failureArray, int expectedCount)
        {
            var result = new BatchResponseResult
            {
                totalRequests = expectedCount,
                successfulRequests = successArray?.Count ?? 0,
                failedRequests = failureArray?.Count ?? 0,
                isSuccess = (successArray?.Count ?? 0) > 0
            };
            
            // Parse successes
            if (successArray != null)
            {
                for (var i = 0; i < successArray.Count; i++)
                {
                    var item = successArray[i];
                    result.results.Add(new IndividualRequestResult(
                        i, true, item.ToString(), null, 200
                    ));
                }
            }
            
            // Parse failures
            if (failureArray != null)
            {
                for (var i = 0; i < failureArray.Count; i++)
                {
                    var item = failureArray[i] as JObject;
                    var errorMessage = item?.Value<string>("error") ?? "Unknown error";
                    var statusCode = item?.Value<int?>("statusCode") ?? 500;
                    
                    result.results.Add(new IndividualRequestResult(
                        result.successfulRequests + i, false, null, errorMessage, statusCode
                    ));
                }
            }
            
            return result;
        }
        
        private static BatchResponseResult ParseSimpleArrayFormat(JArray array, int expectedCount)
        {
            var result = new BatchResponseResult
            {
                totalRequests = expectedCount
            };
            
            for (var i = 0; i < array.Count; i++)
            {
                var item = array[i];
                
                // Assume success if no explicit status
                result.results.Add(new IndividualRequestResult(i, true, item.ToString(), null, 200));
                result.successfulRequests++;
            }
            
            result.isSuccess = true;
            return result;
        }
    }
}

