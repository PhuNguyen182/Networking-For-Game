using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Request Deduplication mechanism - loại bỏ duplicate requests
    /// Sử dụng hash-based deduplication để tối ưu performance
    /// </summary>
    public class RequestDeduplicator
    {
        private readonly HashSet<string> _requestHashes;
        private readonly Dictionary<string, QueuedRequest> _pendingRequests;
        private readonly int _maxCacheSize;
        private readonly Queue<string> _hashQueue; // FIFO queue để track insertion order
        
        public RequestDeduplicator(int maxCacheSize = 1000)
        {
            this._maxCacheSize = maxCacheSize;
            this._requestHashes = new HashSet<string>(maxCacheSize);
            this._pendingRequests = new Dictionary<string, QueuedRequest>(maxCacheSize);
            this._hashQueue = new Queue<string>(maxCacheSize);
        }
        
        /// <summary>
        /// Kiểm tra xem request có phải duplicate không
        /// </summary>
        /// <param name="request">Request cần kiểm tra</param>
        /// <param name="existingRequest">Request đã tồn tại (nếu là duplicate)</param>
        /// <returns>True nếu là duplicate</returns>
        public bool IsDuplicate(QueuedRequest request, out QueuedRequest existingRequest)
        {
            var hash = this.CalculateRequestHash(request);
            
            if (this._requestHashes.Contains(hash))
            {
                existingRequest = this._pendingRequests.TryGetValue(hash, out var existing) 
                    ? existing 
                    : null;
                return true;
            }
            
            existingRequest = null;
            return false;
        }
        
        /// <summary>
        /// Track request để phát hiện duplicates sau này
        /// </summary>
        public void TrackRequest(QueuedRequest request)
        {
            var hash = this.CalculateRequestHash(request);
            
            // Nếu cache đầy, xóa oldest entry (FIFO)
            if (this._requestHashes.Count >= this._maxCacheSize)
            {
                var oldestHash = this._hashQueue.Dequeue();
                this._requestHashes.Remove(oldestHash);
                this._pendingRequests.Remove(oldestHash);
            }
            
            // Thêm new entry
            if (this._requestHashes.Add(hash))
            {
                this._hashQueue.Enqueue(hash);
                this._pendingRequests[hash] = request;
            }
        }
        
        /// <summary>
        /// Xóa request khỏi tracking khi đã hoàn thành
        /// </summary>
        public void UntrackRequest(QueuedRequest request)
        {
            var hash = this.CalculateRequestHash(request);
            this._requestHashes.Remove(hash);
            this._pendingRequests.Remove(hash);
        }
        
        /// <summary>
        /// Clear tất cả tracked requests
        /// </summary>
        public void Clear()
        {
            this._requestHashes.Clear();
            this._pendingRequests.Clear();
            this._hashQueue.Clear();
        }
        
        /// <summary>
        /// Tính hash của request để deduplication
        /// Hash dựa trên: endpoint + jsonBody
        /// </summary>
        private string CalculateRequestHash(QueuedRequest request)
        {
            // Sử dụng SHA256 để tạo hash - collision-resistant và fast enough
            using (var sha256 = SHA256.Create())
            {
                // Combine endpoint và jsonBody
                var combined = string.Concat(request.endpoint, "|", request.jsonBody);
                var bytes = Encoding.UTF8.GetBytes(combined);
                var hashBytes = sha256.ComputeHash(bytes);
                
                // Convert sang hex string (compact representation)
                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Lấy số lượng requests đang track
        /// </summary>
        public int TrackedCount => this._requestHashes.Count;
    }
}

