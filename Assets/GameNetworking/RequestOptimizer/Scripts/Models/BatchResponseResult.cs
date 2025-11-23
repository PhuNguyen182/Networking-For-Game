using System;
using System.Collections.Generic;

namespace GameNetworking.RequestOptimizer.Scripts.Models
{
    /// <summary>
    /// Kết quả của batch response với hỗ trợ partial success
    /// </summary>
    [Serializable]
    public class BatchResponseResult
    {
        public bool isSuccess;
        public int totalRequests;
        public int successfulRequests;
        public int failedRequests;
        public List<IndividualRequestResult> results;
        
        public BatchResponseResult()
        {
            this.results = new List<IndividualRequestResult>();
        }
        
        /// <summary>
        /// Kiểm tra xem có phải tất cả requests đều thành công không
        /// </summary>
        public bool IsFullSuccess => this.successfulRequests == this.totalRequests;
        
        /// <summary>
        /// Kiểm tra xem có phải partial success không (một số thành công, một số fail)
        /// </summary>
        public bool IsPartialSuccess => this.successfulRequests > 0 && this.failedRequests > 0;
        
        /// <summary>
        /// Kiểm tra xem có phải tất cả requests đều thất bại không
        /// </summary>
        public bool IsFullFailure => this.successfulRequests == 0;
    }
    
    /// <summary>
    /// Kết quả của từng request riêng lẻ trong batch
    /// </summary>
    [Serializable]
    public class IndividualRequestResult
    {
        public int index;
        public bool isSuccess;
        public string response;
        public string errorMessage;
        public int statusCode;
        
        public IndividualRequestResult(int index, bool isSuccess, string response, 
            string errorMessage = null, int statusCode = 200)
        {
            this.index = index;
            this.isSuccess = isSuccess;
            this.response = response;
            this.errorMessage = errorMessage;
            this.statusCode = statusCode;
        }
    }
}

