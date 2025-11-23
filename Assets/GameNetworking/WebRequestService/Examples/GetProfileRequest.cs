using System;

namespace PracticalModules.WebRequestService.Examples
{
    /// <summary>
    /// Request model cho GET profile (optional)
    /// </summary>
    /// <remarks>
    /// GET request có thể có requestBody hoặc null.
    /// Đây là example cho case GET có requestBody.
    /// </remarks>
    [Serializable]
    public class GetProfileRequest
    {
        /// <summary>
        /// User ID để get profile (optional)
        /// </summary>
        public string userId;
        
        /// <summary>
        /// Include additional info flag
        /// </summary>
        public bool includeDetails;
        
        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public GetProfileRequest()
        {
            this.includeDetails = false;
        }
        
        /// <summary>
        /// Constructor với parameters
        /// </summary>
        public GetProfileRequest(string userId, bool includeDetails = false)
        {
            this.userId = userId;
            this.includeDetails = includeDetails;
        }
    }
}

