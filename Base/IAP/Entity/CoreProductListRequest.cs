using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{
    public class CoreProductListRequest
    {
         [System.Serializable]
        public class DataList
        {
            public string product_id;
            public float price;
            public string product_name;
            public string product_desc;
            public string currency;

        }

        // 响应数据结构体
        [System.Serializable]
        public class ProductListData
        {
            public List<DataList> items;
        }
    }
}

