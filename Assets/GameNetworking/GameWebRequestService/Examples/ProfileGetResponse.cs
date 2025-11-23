using System;
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Models;
using UnityEngine;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example GET response với BaseGetResponse generic
    /// </summary>
    [Endpoint("/api/v1/user/profile", "Get User Profile")]
    [Serializable]
    public class ProfileGetResponse : BaseGetResponse<ProfileData>
    {
        /// <summary>
        /// Xử lý khi get profile thành công
        /// </summary>
        /// <param name="result">Profile data từ server</param>
        public override void OnResponseSuccess(ProfileData result)
        {
            Debug.Log($"[ProfileGetResponse] Profile loaded successfully!");
            Debug.Log($"[ProfileGetResponse] User ID: {result.userId}");
            Debug.Log($"[ProfileGetResponse] Username: {result.username}");
            Debug.Log($"[ProfileGetResponse] Email: {result.email}");
            Debug.Log($"[ProfileGetResponse] Level: {result.level}");
            Debug.Log($"[ProfileGetResponse] Experience: {result.experience}");
            
            // Update UI hoặc game state
            // Example: GameManager.Instance.UpdatePlayerProfile(result);
        }
        
        /// <summary>
        /// Xử lý khi get profile thất bại
        /// </summary>
        /// <param name="errorCode">HTTP status code lỗi</param>
        /// <param name="errorMessage">Message mô tả lỗi</param>
        public override void OnResponseFailed(int errorCode, string errorMessage)
        {
            Debug.LogError($"[ProfileGetResponse] Failed to load profile!");
            Debug.LogError($"[ProfileGetResponse] Error Code: {errorCode}");
            Debug.LogError($"[ProfileGetResponse] Error Message: {errorMessage}");
            
            // Handle specific error codes
            switch (errorCode)
            {
                case 401:
                    Debug.LogError("[ProfileGetResponse] Unauthorized - please login again");
                    // Redirect to login
                    break;
                case 404:
                    Debug.LogError("[ProfileGetResponse] Profile not found");
                    break;
                default:
                    Debug.LogError($"[ProfileGetResponse] Unexpected error occurred");
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
    /// Profile data structure
    /// </summary>
    [Serializable]
    public class ProfileData
    {
        public string userId;
        public string username;
        public string email;
        public int level;
        public float experience;
        public long registeredAt;
        public long lastLoginAt;
    }
}

