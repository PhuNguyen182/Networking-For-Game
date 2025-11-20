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
    }
}
