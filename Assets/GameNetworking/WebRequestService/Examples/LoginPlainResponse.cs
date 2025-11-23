using System;
using GameNetworking.WebRequestService.Attributes;
using GameNetworking.WebRequestService.Models;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example response model cho login API với EndpointAttribute
    /// </summary>
    [Endpoint("/api/v1/auth/login", "User Login")]
    [Serializable]
    public class LoginPlainResponse : BasePlainResponse
    {
        public string token;
        public string refreshToken;
        public UserData userData;
        public long expiresAt;
        
        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            
            this.token = null;
            this.refreshToken = null;
            this.userData = null;
            this.expiresAt = 0;
        }
    }
    
    /// <summary>
    /// User data model
    /// </summary>
    [Serializable]
    public class UserData
    {
        public string userId;
        public string username;
        public string email;
        public int level;
        public long registeredAt;
    }
}

