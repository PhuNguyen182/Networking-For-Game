using System.Collections.Generic;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Configuration
{
    [CreateAssetMenu(fileName = "RequestConfigCollection",
        menuName = "Scriptable Objects/NetworkingForGame/RequestConfigCollection")]
    public class RequestConfigCollection : ScriptableObject
    {
        [SerializeField] public List<RequestConfig> requestConfigs;

        public RequestConfig GetRequestConfigByPriority(RequestPriority priority)
        {
            for (int i = 0; i < this.requestConfigs.Count; i++)
            {
                if (this.requestConfigs[i].priority == priority)
                    return this.requestConfigs[i];
            }
            
            return null;
        }
        
        /// <summary>
        /// Lấy tất cả RequestConfig dưới dạng Dictionary theo Priority
        /// </summary>
        public Dictionary<RequestPriority, RequestConfig> GetAllRequestConfigs()
        {
            var result = new Dictionary<RequestPriority, RequestConfig>();
            
            for (int i = 0; i < this.requestConfigs.Count; i++)
            {
                var config = this.requestConfigs[i];
                if (!result.ContainsKey(config.priority))
                {
                    result[config.priority] = config;
                }
            }
            
            return result;
        }
    }
}
