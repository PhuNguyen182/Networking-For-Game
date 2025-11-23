using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Models;
using GameNetworking.RequestOptimizer.Scripts.Utilities;
using Newtonsoft.Json.Linq;

namespace GameNetworking.RequestOptimizer.Scripts.MergingStrategies
{
    /// <summary>
    /// Last-Wins Merging Strategy
    /// Gộp nhiều requests bằng cách giữ lại giá trị cuối cùng (latest value wins)
    /// Ví dụ: 10 requests update player position → 1 request với position cuối cùng
    /// </summary>
    public class LastWinsMergingStrategy : BaseMergingStrategy
    {
        private readonly string _mergeKeyField;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mergeKeyField">Field name trong JSON để tạo merge key (ví dụ: "playerId", "userId")</param>
        /// <param name="maxMergeDelay">Thời gian chờ tối đa trước khi gửi merged request</param>
        public LastWinsMergingStrategy(string mergeKeyField = "id", float maxMergeDelay = 2f) 
            : base(maxMergeDelay)
        {
            this._mergeKeyField = mergeKeyField;
        }
        
        public override string GetMergeKey(QueuedRequest request)
        {
            try
            {
                // Parse JSON để lấy merge key
                var jsonObject = JObject.Parse(request.jsonBody);
                
                if (jsonObject.TryGetValue(this._mergeKeyField, out var keyToken))
                {
                    // Tạo merge key: endpoint + ":" + field value
                    return $"{request.endpoint}:{keyToken}";
                }
                
                // Fallback: sử dụng endpoint làm merge key
                return request.endpoint;
            }
            catch
            {
                // Nếu parse JSON thất bại, dùng endpoint
                return request.endpoint;
            }
        }
        
        public override async UniTask<QueuedRequest> MergeRequestsAsync(IReadOnlyList<QueuedRequest> requests)
        {
            if (requests.Count == 0)
            {
                throw new System.ArgumentException("Cannot merge empty request list");
            }
            
            if (requests.Count == 1)
            {
                return requests[0];
            }
            
            await UniTask.SwitchToThreadPool();
            
            // Last-Wins strategy: giữ lại request cuối cùng
            // Nhưng merge các fields từ tất cả requests
            var mergedJson = await this.MergeJsonBodiesAsync(requests);
            
            // Lấy properties từ request cuối cùng (highest priority)
            var lastRequest = requests[^1];
            var httpMethod = lastRequest.httpMethod;
            
            var mergedRequest = new QueuedRequest(
                lastRequest.endpoint,
                mergedJson,
                httpMethod,
                lastRequest.priority,
                lastRequest.config
            );
            
            await UniTask.SwitchToMainThread();
            
            return mergedRequest;
        }
        
        private async UniTask<string> MergeJsonBodiesAsync(IReadOnlyList<QueuedRequest> requests)
        {
            try
            {
                // Base object từ request đầu tiên
                var mergedObject = JObject.Parse(requests[0].jsonBody);
                
                // Merge từng request sau đó
                for (var i = 1; i < requests.Count; i++)
                {
                    var currentObject = JObject.Parse(requests[i].jsonBody);
                    
                    // Merge: giá trị mới sẽ override giá trị cũ (last wins)
                    mergedObject.Merge(currentObject, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Replace
                    });
                }
                
                await UniTask.Yield();
                
                return JsonSerializer.SerializeCompact(mergedObject);
            }
            catch
            {
                // Fallback: nếu merge thất bại, trả về request cuối cùng
                return requests[requests.Count - 1].jsonBody;
            }
        }
    }
}

