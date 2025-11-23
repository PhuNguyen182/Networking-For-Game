using System;
using GameNetworking.WebRequestService.Attributes;
using GameNetworking.WebRequestService.Models;
using UnityEngine;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example POST response với BasePostResponse generic
    /// </summary>
    [Endpoint("/api/v1/auth/login", "User Login")]
    [Serializable]
    public class NewLoginResponse : BasePostResponse<LoginResponseData>
    {
        /// <summary>
        /// Xử lý khi login thành công
        /// </summary>
        /// <param name="result">Dữ liệu login từ server</param>
        public override void OnResponseSuccess(LoginResponseData result)
        {
            Debug.Log($"[NewLoginResponse] Login successful!");
            Debug.Log($"[NewLoginResponse] Token: {result.token}");
            Debug.Log($"[NewLoginResponse] User: {result.userData.username}");
            Debug.Log($"[NewLoginResponse] Email: {result.userData.email}");
            
            // Save token to PlayerPrefs
            PlayerPrefs.SetString("auth_token", result.token);
            PlayerPrefs.SetString("refresh_token", result.refreshToken);
            PlayerPrefs.Save();
            
            Debug.Log("[NewLoginResponse] Tokens saved to PlayerPrefs");
        }
        
        /// <summary>
        /// Xử lý khi login thất bại
        /// </summary>
        /// <param name="errorCode">HTTP status code lỗi</param>
        /// <param name="errorMessage">Message mô tả lỗi</param>
        public override void OnResponseFailed(int errorCode, string errorMessage)
        {
            Debug.LogError($"[NewLoginResponse] Login failed!");
            Debug.LogError($"[NewLoginResponse] Error Code: {errorCode}");
            Debug.LogError($"[NewLoginResponse] Error Message: {errorMessage}");
            
            // Handle specific error codes
            switch (errorCode)
            {
                case 401:
                    Debug.LogError("[NewLoginResponse] Invalid credentials - please check username/password");
                    break;
                case 403:
                    Debug.LogError("[NewLoginResponse] Account locked or banned");
                    break;
                case 429:
                    Debug.LogError("[NewLoginResponse] Too many login attempts - please try again later");
                    break;
                default:
                    Debug.LogError($"[NewLoginResponse] Unexpected error occurred");
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
    /// Login response data structure
    /// </summary>
    [Serializable]
    public class LoginResponseData
    {
        public string token;
        public string refreshToken;
        public UserData userData;
        public long expiresAt;
    }
}

