using System;

namespace GameNetworking.RequestOptimizer.Scripts
{
    /// <summary>
    /// Priority levels for requests
    /// </summary>
    [Serializable]
    public enum RequestPriority : byte
    {
        Critical = 0,    // Must send immediately (e.g., purchase, critical gameplay)
        High = 1,        // Important but can wait a bit (e.g., level complete, boss kill)
        Normal = 2,      // Standard gameplay events (e.g., quest progress)
        Low = 3,         // Analytics, telemetry
        Batch = 4,
    }
}
