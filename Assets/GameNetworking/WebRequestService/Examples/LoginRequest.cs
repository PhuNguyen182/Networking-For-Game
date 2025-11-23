using System;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Example request model cho login API
    /// </summary>
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
        public string deviceId;
        
        public LoginRequest(string username, string password, string deviceId)
        {
            this.username = username;
            this.password = password;
            this.deviceId = deviceId;
        }
    }
}

