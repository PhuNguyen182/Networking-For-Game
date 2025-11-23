using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameNetworking.WebRequestService.Interfaces
{
    /// <summary>
    /// Interface định nghĩa các phương thức cơ bản cho web request service
    /// </summary>
    /// <remarks>
    /// Interface này tuân thủ nguyên tắc Interface Segregation Principle (ISP)
    /// và Dependency Inversion Principle (DIP) trong SOLID.
    /// </remarks>
    public interface IWebRequest
    {
        /// <summary>
        /// Thực hiện GET request bất đồng bộ
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu request body</typeparam>
        /// <typeparam name="TResponse">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="requestBody">Dữ liệu request body (optional, có thể null)</param>
        /// <param name="headers">Headers tùy chọn</param>
        /// <param name="cancellationToken">Token để hủy request</param>
        /// <returns>Response data hoặc null nếu thất bại</returns>
        public UniTask<TResponse> GetAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody = null,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Thực hiện POST request bất đồng bộ
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu request body</typeparam>
        /// <typeparam name="TResponse">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="requestBody">Dữ liệu request body</param>
        /// <param name="headers">Headers tùy chọn</param>
        /// <param name="cancellationToken">Token để hủy request</param>
        /// <returns>Response data hoặc null nếu thất bại</returns>
        public UniTask<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Thực hiện PUT request bất đồng bộ
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu request body</typeparam>
        /// <typeparam name="TResponse">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="requestBody">Dữ liệu request body</param>
        /// <param name="headers">Headers tùy chọn</param>
        /// <param name="cancellationToken">Token để hủy request</param>
        /// <returns>Response data hoặc null nếu thất bại</returns>
        public UniTask<TResponse> PutAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
            where TResponse : class;
    }
}
