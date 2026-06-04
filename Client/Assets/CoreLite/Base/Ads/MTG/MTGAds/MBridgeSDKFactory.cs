namespace MTGAds
{
    public class MBridgeSDKFactory
    {
        private static MBridgeSDKImpl mBridgeSDKImpl;

        public static MBridgeSDKImpl getMBridgeSDK()
        {
            return mBridgeSDKImpl ??= new MBridgeSDKImpl();
        }
    }
}