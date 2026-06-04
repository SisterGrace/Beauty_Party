
using System;
namespace CoreLite
{

    public class CoreAdsBidResult : IComparable<CoreAdsBidResult>
    {
        public string ClientName { get; set; }
        public double Revenue { get; set; }
        public ICoreAdService Client { get; set; }
        public CoreLiteAdsType AdType { get; set; }
        public CorePlatformType PlatformType { get; set; }
        
        public int CompareTo(CoreAdsBidResult other)
        {
            return other.Revenue.CompareTo(this.Revenue);
        }
    }
}
