namespace GameNetworking.WebRequestService.Constants
{
    /// <summary>
    /// Định nghĩa các HTTP status code chuẩn theo RFC 7231 và các mã mở rộng
    /// </summary>
    /// <remarks>
    /// Class này chứa tất cả các mã trạng thái HTTP thường gặp để sử dụng
    /// trong việc xử lý response từ server và logging chi tiết.
    /// </remarks>
    public static class HttpStatusCode
    {
        #region 2xx Success
        
        /// <summary>
        /// 200 OK - Request thành công
        /// </summary>
        public const int Success = 200;
        
        /// <summary>
        /// 201 Created - Resource được tạo thành công
        /// </summary>
        public const int Created = 201;
        
        /// <summary>
        /// 202 Accepted - Request được chấp nhận nhưng chưa hoàn thành
        /// </summary>
        public const int Accepted = 202;
        
        /// <summary>
        /// 204 No Content - Request thành công nhưng không có nội dung trả về
        /// </summary>
        public const int NoContent = 204;
        
        #endregion
        
        #region 3xx Redirection
        
        /// <summary>
        /// 301 Moved Permanently - Resource đã được di chuyển vĩnh viễn
        /// </summary>
        public const int MovedPermanently = 301;
        
        /// <summary>
        /// 302 Found - Resource tạm thời ở địa chỉ khác
        /// </summary>
        public const int Found = 302;
        
        /// <summary>
        /// 304 Not Modified - Resource không thay đổi (cache hit)
        /// </summary>
        public const int NotModified = 304;
        
        #endregion
        
        #region 4xx Client Errors
        
        /// <summary>
        /// 400 Bad Request - Request không hợp lệ (syntax error, invalid data)
        /// </summary>
        public const int BadRequest = 400;
        
        /// <summary>
        /// 401 Unauthorized - Cần xác thực (missing or invalid authentication token)
        /// </summary>
        public const int Unauthorized = 401;
        
        /// <summary>
        /// 403 Forbidden - Không có quyền truy cập (authenticated but no permission)
        /// </summary>
        public const int Forbidden = 403;
        
        /// <summary>
        /// 404 Not Found - Resource không tồn tại
        /// </summary>
        public const int NotFound = 404;
        
        /// <summary>
        /// 405 Method Not Allowed - HTTP method không được hỗ trợ cho endpoint này
        /// </summary>
        public const int MethodNotAllowed = 405;
        
        /// <summary>
        /// 408 Request Timeout - Request timeout từ phía client
        /// </summary>
        public const int RequestTimeout = 408;
        
        /// <summary>
        /// 409 Conflict - Xung đột với trạng thái hiện tại của resource
        /// </summary>
        public const int Conflict = 409;
        
        /// <summary>
        /// 410 Gone - Resource đã bị xóa vĩnh viễn
        /// </summary>
        public const int Gone = 410;
        
        /// <summary>
        /// 413 Payload Too Large - Request body quá lớn
        /// </summary>
        public const int PayloadTooLarge = 413;
        
        /// <summary>
        /// 415 Unsupported Media Type - Content-Type không được hỗ trợ
        /// </summary>
        public const int UnsupportedMediaType = 415;
        
        /// <summary>
        /// 422 Unprocessable Entity - Request đúng cú pháp nhưng dữ liệu không hợp lệ
        /// </summary>
        public const int UnprocessableEntity = 422;
        
        /// <summary>
        /// 429 Too Many Requests - Rate limit exceeded
        /// </summary>
        public const int TooManyRequests = 429;
        
        #endregion
        
        #region 5xx Server Errors
        
        /// <summary>
        /// 500 Internal Server Error - Lỗi không xác định từ server
        /// </summary>
        public const int InternalServerError = 500;
        
        /// <summary>
        /// 501 Not Implemented - Server không hỗ trợ chức năng này
        /// </summary>
        public const int NotImplemented = 501;
        
        /// <summary>
        /// 502 Bad Gateway - Gateway/proxy nhận response không hợp lệ
        /// </summary>
        public const int BadGateway = 502;
        
        /// <summary>
        /// 503 Service Unavailable - Server tạm thời không khả dụng (maintenance, overload)
        /// </summary>
        public const int ServiceUnavailable = 503;
        
        /// <summary>
        /// 504 Gateway Timeout - Gateway/proxy timeout
        /// </summary>
        public const int GatewayTimeout = 504;
        
        #endregion
        
        #region Custom Application Codes
        
        /// <summary>
        /// -1 Network Error - Lỗi mạng (no connection, DNS failure)
        /// </summary>
        public const int NetworkError = -1;
        
        /// <summary>
        /// -2 Cancelled - Request bị hủy bởi client
        /// </summary>
        public const int Cancelled = -2;
        
        /// <summary>
        /// -3 Parse Error - Lỗi parse response data
        /// </summary>
        public const int ParseError = -3;
        
        /// <summary>
        /// -4 Unknown Error - Lỗi không xác định
        /// </summary>
        public const int UnknownError = -4;
        
        #endregion
        
        /// <summary>
        /// Lấy mô tả chi tiết của status code
        /// </summary>
        /// <param name="statusCode">Mã status code</param>
        /// <returns>Mô tả chi tiết về status code</returns>
        public static string GetDescription(int statusCode)
        {
            return statusCode switch
            {
                Success => "Request thành công",
                Created => "Resource được tạo thành công",
                Accepted => "Request được chấp nhận nhưng chưa hoàn thành xử lý",
                NoContent => "Request thành công nhưng không có nội dung trả về",
                
                MovedPermanently => "Resource đã được di chuyển vĩnh viễn sang địa chỉ khác",
                Found => "Resource tạm thời ở địa chỉ khác",
                NotModified => "Resource không thay đổi (cache hit)",
                
                BadRequest => "Request không hợp lệ - kiểm tra lại syntax hoặc dữ liệu đầu vào",
                Unauthorized => "Cần xác thực - token không hợp lệ hoặc đã hết hạn",
                Forbidden => "Không có quyền truy cập - đã xác thực nhưng thiếu permission",
                NotFound => "Resource không tồn tại hoặc đã bị xóa",
                MethodNotAllowed => "HTTP method không được hỗ trợ cho endpoint này",
                RequestTimeout => "Request timeout từ phía client",
                Conflict => "Xung đột với trạng thái hiện tại của resource",
                Gone => "Resource đã bị xóa vĩnh viễn",
                PayloadTooLarge => "Request body vượt quá giới hạn cho phép",
                UnsupportedMediaType => "Content-Type không được hỗ trợ",
                UnprocessableEntity => "Dữ liệu không hợp lệ - kiểm tra lại business rules",
                TooManyRequests => "Vượt quá giới hạn rate limit - vui lòng thử lại sau",
                
                InternalServerError => "Lỗi server không xác định",
                NotImplemented => "Server không hỗ trợ chức năng này",
                BadGateway => "Gateway/proxy nhận response không hợp lệ từ upstream server",
                ServiceUnavailable => "Server tạm thời không khả dụng (maintenance hoặc overload)",
                GatewayTimeout => "Gateway/proxy timeout khi chờ response từ upstream server",
                
                NetworkError => "Lỗi mạng - kiểm tra kết nối internet",
                Cancelled => "Request bị hủy bởi client",
                ParseError => "Lỗi parse response data - format không hợp lệ",
                UnknownError => "Lỗi không xác định",
                
                _ => $"Unknown status code: {statusCode}"
            };
        }
        
        /// <summary>
        /// Kiểm tra xem status code có phải là thành công (2xx) không
        /// </summary>
        /// <param name="statusCode">Mã status code</param>
        /// <returns>True nếu là success code, false nếu là error code</returns>
        public static bool IsSuccess(int statusCode)
        {
            return statusCode is >= 200 and < 300;
        }
        
        /// <summary>
        /// Kiểm tra xem status code có phải là client error (4xx) không
        /// </summary>
        /// <param name="statusCode">Mã status code</param>
        /// <returns>True nếu là client error, false nếu không phải</returns>
        public static bool IsClientError(int statusCode)
        {
            return statusCode is >= 400 and < 500;
        }
        
        /// <summary>
        /// Kiểm tra xem status code có phải là server error (5xx) không
        /// </summary>
        /// <param name="statusCode">Mã status code</param>
        /// <returns>True nếu là server error, false nếu không phải</returns>
        public static bool IsServerError(int statusCode)
        {
            return statusCode is >= 500 and < 600;
        }
    }
}

