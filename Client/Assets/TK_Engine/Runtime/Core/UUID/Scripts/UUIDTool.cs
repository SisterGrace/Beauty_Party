using UnityEngine;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

public static class UUIDTool
{
    public static string DeviceID
    {
        get
        {
            if (string.IsNullOrEmpty(sDeviceID))
            {
                sDeviceID = SystemInfo.deviceUniqueIdentifier;

#if UNITY_IOS && !UNITY_EDITOR
                string tUUID = UUIDTool.GetUUID();
                if(!string.IsNullOrEmpty(tUUID))
                {
                    sDeviceID = tUUID;
                }
                UUIDTool.SetUUID(sDeviceID);
#endif
            }

            return sDeviceID;
        }
    }
    private static string sDeviceID = null;

#if UNITY_IOS
    private static void SetUUID(string pUUID)
    {
        mSetUUID(pUUID);
    }

    private static string GetUUID()
    {
        return mGetUUID();
    }

    private static void DeleteUUID()
    {
        mDeleteUUID();
    }

    [DllImport("__Internal")]
    private static extern void mSetUUID(string pUUID);

    [DllImport("__Internal")]
    private static extern string mGetUUID();

    [DllImport("__Internal")]
    private static extern void mDeleteUUID();
#endif
}