using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public static class SkyLoginEnum
    {
        [System.Serializable]
        public class UserData
        {
            public int loginType;
            public string userID = "";
            public string token = "";
            public string email = "";
            public string nickName = "";
            public string photoUrl = "";
            public int firstLogin = 0;
            public string gameUID = "";
            public string pwd = "";
            public string smsCode = "";      
            

            public void ClearData()
            {
                userID = "";
                token = "";
                email = "";
                nickName = "";
                photoUrl = "";
                firstLogin = 0;
                gameUID = "";
                pwd = "";
            }
        }

        [System.Serializable]
        public struct ServerBackLoginInfo
        {
            public string user_id;
            public string token;
            public int user_type;
            public int first_login;
            public string display_name;
            public string photo_url;
            public string email;
            public string uid;
        }
    }
}

