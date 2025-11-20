namespace GameNetworking.RequestOptimizer.Scripts.Models
{
    /// <summary>
    /// Kết quả của một HTTP request với error handling
    /// </summary>
    public readonly struct RequestResult
    {
        public readonly bool IsSuccess;
        public readonly string Response;
        public readonly string ErrorMessage;
        public readonly long StatusCode;
        public readonly RequestErrorType ErrorType;
        
        private RequestResult(bool isSuccess, string response, string errorMessage, long statusCode, RequestErrorType errorType)
        {
            this.IsSuccess = isSuccess;
            this.Response = response;
            this.ErrorMessage = errorMessage;
            this.StatusCode = statusCode;
            this.ErrorType = errorType;
        }
        
        public static RequestResult Success(string response, long statusCode = 200)
        {
            return new RequestResult(true, response, null, statusCode, RequestErrorType.None);
        }
        
        public static RequestResult Failure(string errorMessage, long statusCode, RequestErrorType errorType)
        {
            return new RequestResult(false, null, errorMessage, statusCode, errorType);
        }
        
        public static RequestResult RateLimitExceeded(string errorMessage = "Rate limit exceeded")
        {
            return new RequestResult(false, null, errorMessage, 429, RequestErrorType.RateLimitExceeded);
        }
        
        public static RequestResult NetworkError(string errorMessage)
        {
            return new RequestResult(false, null, errorMessage, 0, RequestErrorType.NetworkError);
        }
        
        public static RequestResult Timeout(string errorMessage = "Request timeout")
        {
            return new RequestResult(false, null, errorMessage, 0, RequestErrorType.Timeout);
        }
    }
    
    /// <summary>
    /// Loại lỗi có thể xảy ra khi gửi request
    /// </summary>
    public enum RequestErrorType : byte
    {
        None = 0,
        NetworkError = 1,
        Timeout = 2,
        RateLimitExceeded = 3,
        ServerError = 4,
        ClientError = 5,
        Unknown = 255
    }
}

