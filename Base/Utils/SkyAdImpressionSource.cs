using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Sky
{
    public static class SkyAdImpressionSource
    {
        public static string GetSource(string configKey, string defaultSource)
        {
            var configJson = SkyAnalyticsMgr.GetConfigStr(configKey);
            if (string.IsNullOrEmpty(configJson)) return defaultSource;

            try
            {
                var revenueData = JsonConvert.DeserializeObject<RevenueData[]>(configJson);
                if (revenueData == null || revenueData.Length == 0) return defaultSource;

                int totalRate = 0;
                foreach (var item in revenueData) totalRate += item.rate;

                int randomValue = UnityEngine.Random.Range(0, totalRate);
                int accumulatedRate = 0;

                foreach (var item in revenueData)
                {
                    accumulatedRate += item.rate;
                    if (randomValue < accumulatedRate) return item.name;
                }
            }
            catch { }

            return defaultSource;
        }

        [System.Serializable]
        public class RevenueData
        {
            public string name;
            public int rate;
        }
    }
}

