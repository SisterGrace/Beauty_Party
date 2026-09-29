
using System;
namespace Sky
{

    public class SkyAdsBidResult : IComparable<SkyAdsBidResult>
    {
        public string ClientName { get; set; }
        public double Revenue { get; set; }
        public ISkyAdService Client { get; set; }
        public SkyAdsType AdType { get; set; }
        public SkyPlatformType PlatformType { get; set; }

        public int CompareTo(SkyAdsBidResult other)
        {
            return other.Revenue.CompareTo(this.Revenue);
        }
    }
}
