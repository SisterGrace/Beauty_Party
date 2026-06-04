using CoreLite;
using UnityEngine;

namespace MTGAds
{
    public class MBridgeIds
    {
        private AndroidJavaObject obj;


        public string bidToken = "";
        public bool isCache = false;
        public string placementId;
        public string unitId;
        public string requestId;

        public MBridgeIds(AndroidJavaObject obj)
        {
            this.obj = obj;

            bidToken = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getBidToken");
            isCache = CoreReflectionUtils.CallInstanceMethod<bool>(obj, "isCache");
            placementId = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getPlacementId");
            unitId = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getUnitId");
            requestId = CoreReflectionUtils.CallInstanceMethod<string>(obj, "getRequestId");
        }
    }
}