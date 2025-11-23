using System;
using GameNetworking.WebRequestService.Attributes;
using GameNetworking.WebRequestService.Models;
using UnityEngine;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example PUT response với BasePutResponse generic
    /// </summary>
    [Endpoint("/api/v1/user/profile", "Update User Profile")]
    [Serializable]
    public class ProfileUpdateResponse : BasePutResponse<ProfileUpdateData>
    {
        /// <summary>
        /// Xử lý khi update profile thành công
        /// </summary>
        /// <param name="result">Updated profile data từ server</param>
        public override void OnResponseSuccess(ProfileUpdateData result)
        {
            Debug.Log($"[ProfileUpdateResponse] Profile updated successfully!");
            Debug.Log($"[ProfileUpdateResponse] New Username: {result.username}");
            Debug.Log($"[ProfileUpdateResponse] New Email: {result.email}");
            Debug.Log($"[ProfileUpdateResponse] Updated At: {result.updatedAt}");
            
            // Update local cache hoặc UI
            // Example: GameManager.Instance.UpdateLocalProfile(result);
        }
        
        /// <summary>
        /// Xử lý khi update profile thất bại
        /// </summary>
        /// <param name="errorCode">HTTP status code lỗi</param>
        /// <param name="errorMessage">Message mô tả lỗi</param>
        public override void OnResponseFailed(int errorCode, string errorMessage)
        {
            Debug.LogError($"[ProfileUpdateResponse] Failed to update profile!");
            Debug.LogError($"[ProfileUpdateResponse] Error Code: {errorCode}");
            Debug.LogError($"[ProfileUpdateResponse] Error Message: {errorMessage}");
            
            // Handle specific error codes
            switch (errorCode)
            {
                case 400:
                    Debug.LogError("[ProfileUpdateResponse] Invalid data - check input fields");
                    break;
                case 401:
                    Debug.LogError("[ProfileUpdateResponse] Unauthorized - please login again");
                    break;
                case 409:
                    Debug.LogError("[ProfileUpdateResponse] Username/Email already taken");
                    break;
                case 422:
                    Debug.LogError("[ProfileUpdateResponse] Validation failed - check data format");
                    break;
                default:
                    Debug.LogError($"[ProfileUpdateResponse] Unexpected error occurred");
                    break;
            }
        }
        
        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            // Custom cleanup nếu cần
        }
    }
    
    /// <summary>
    /// Profile update data structure
    /// </summary>
    [Serializable]
    public class ProfileUpdateData
    {
        public string userId;
        public string username;
        public string email;
        public long updatedAt;
    }
}

