using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK_Engine;
using System;

namespace GameBase
{
    public class WebRequestCert : UnityEngine.Networking.CertificateHandler
{
    /// <summary>
    /// 强制跳过ssl验证
    /// </summary>
    /// <param name="certificateData"></param>
    /// <returns></returns>
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
    public class APIRequestManager : SingletonBehaviour<APIRequestManager>
    {
        private List<APIRequestUnit> mRequestList = new List<APIRequestUnit>();
        private Dictionary<Type, object> mRespondDataDic = new Dictionary<Type, object>();

        public T GetData<T>() where T : APIBase
        {
            Type tTypeKey = typeof(T);
            if (mRespondDataDic.ContainsKey(tTypeKey))
            {
                return mRespondDataDic[tTypeKey] as T;
            }
            return null;
        }

        public void SetData<T>(T pData) where T : APIBase
        {
            mRespondDataDic[typeof(T)] = pData;
        }

        public void SendRequest<T, U>(T pRequestData, Action<U> pRespondCallback = null, Action<string> pErrorCallback = null) where T : APIBase
        {
            APIRequestUnit tUnit = gameObject.AddComponent<APIRequestUnit>();
            mRequestList.Add(tUnit);
            tUnit.DelOver = OnRequestOver;
            tUnit.SendRequest(pRequestData, pRespondCallback, pErrorCallback);
        }

        public void SendRequest<T>(T pRequestData, Action<string> pRespondCallback = null, Action<string> pErrorCallback = null) where T : APIBase
        {
            SendRequest<T, string>(pRequestData, pRespondCallback, pErrorCallback);
        }

        private void OnRequestOver(APIRequestUnit pUnit)
        {
            if (mRequestList.Contains(pUnit))
            {
                mRequestList.Remove(pUnit);
                Destroy(pUnit);
            }
        }
    }
}
