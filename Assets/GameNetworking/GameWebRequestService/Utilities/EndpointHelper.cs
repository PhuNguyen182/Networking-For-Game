using System;
using GameNetworking.GameWebRequestService.Attributes;

namespace GameNetworking.GameWebRequestService.Utilities
{
    /// <summary>
    /// Helper class để làm việc với EndpointAttribute
    /// </summary>
    /// <remarks>
    /// Class này cung cấp các utility methods để extract thông tin từ EndpointAttribute.
    /// </remarks>
    public static class EndpointHelper
    {
        /// <summary>
        /// Lấy endpoint path từ EndpointAttribute của response type
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>Endpoint path hoặc null nếu không tìm thấy</returns>
        public static string GetEndpointPath<TResponse>() where TResponse : class
        {
            return GetEndpointPath(typeof(TResponse));
        }

        /// <summary>
        /// Lấy endpoint path từ EndpointAttribute của type
        /// </summary>
        /// <param name="type">Type cần lấy endpoint</param>
        /// <returns>Endpoint path hoặc null nếu không tìm thấy</returns>
        public static string GetEndpointPath(Type type)
        {
            var attribute = GetEndpointAttribute(type);
            return attribute?.Route;
        }

        /// <summary>
        /// Lấy endpoint name từ EndpointAttribute của response type
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>Endpoint name hoặc null nếu không tìm thấy</returns>
        public static string GetEndpointName<TResponse>() where TResponse : class
        {
            return GetEndpointName(typeof(TResponse));
        }

        /// <summary>
        /// Lấy endpoint name từ EndpointAttribute của type
        /// </summary>
        /// <param name="type">Type cần lấy endpoint</param>
        /// <returns>Endpoint name hoặc null nếu không tìm thấy</returns>
        public static string GetEndpointName(Type type)
        {
            var attribute = GetEndpointAttribute(type);
            return attribute?.Name;
        }

        /// <summary>
        /// Lấy EndpointAttribute từ type
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>EndpointAttribute hoặc null nếu không tìm thấy</returns>
        public static EndpointAttribute GetEndpointAttribute<TResponse>() where TResponse : class
        {
            return GetEndpointAttribute(typeof(TResponse));
        }

        /// <summary>
        /// Lấy EndpointAttribute từ type
        /// </summary>
        /// <param name="type">Type cần lấy attribute</param>
        /// <returns>EndpointAttribute hoặc null nếu không tìm thấy</returns>
        public static EndpointAttribute GetEndpointAttribute(Type type)
        {
            if (type == null)
            {
                return null;
            }

            var attributes = type.GetCustomAttributes(typeof(EndpointAttribute), true);

            if (attributes.Length > 0 && attributes[0] is EndpointAttribute endpointAttr)
            {
                return endpointAttr;
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra xem type có EndpointAttribute không
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <returns>True nếu có EndpointAttribute, false nếu không</returns>
        public static bool HasEndpointAttribute<TResponse>() where TResponse : class
        {
            return HasEndpointAttribute(typeof(TResponse));
        }

        /// <summary>
        /// Kiểm tra xem type có EndpointAttribute không
        /// </summary>
        /// <param name="type">Type cần kiểm tra</param>
        /// <returns>True nếu có EndpointAttribute, false nếu không</returns>
        public static bool HasEndpointAttribute(Type type)
        {
            return GetEndpointAttribute(type) != null;
        }

        /// <summary>
        /// Validate xem response type có EndpointAttribute hợp lệ không
        /// </summary>
        /// <typeparam name="TResponse">Kiểu response</typeparam>
        /// <exception cref="InvalidOperationException">Nếu không có hoặc EndpointAttribute không hợp lệ</exception>
        public static void ValidateEndpointAttribute<TResponse>() where TResponse : class
        {
            ValidateEndpointAttribute(typeof(TResponse));
        }

        /// <summary>
        /// Validate xem type có EndpointAttribute hợp lệ không
        /// </summary>
        /// <param name="type">Type cần validate</param>
        /// <exception cref="InvalidOperationException">Nếu không có hoặc EndpointAttribute không hợp lệ</exception>
        public static void ValidateEndpointAttribute(Type type)
        {
            if (!HasEndpointAttribute(type))
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' does not have EndpointAttribute. " +
                    "Please add [Endpoint(path, name)] attribute to the response class."
                );
            }

            var attribute = GetEndpointAttribute(type);

            if (string.IsNullOrEmpty(attribute.Route))
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' has EndpointAttribute but Path is null or empty. " +
                    "Please provide a valid endpoint path."
                );
            }
        }
    }
}
