using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using GameBase;
using TK_Engine;

namespace GameLogic
{
    public class MyTest : MonoBehaviour
    { 
        public float totalAmount = 2f; // 总金额
        public int groupCount = 3;// 分组数量
        [Range(0,1)] public float fluctuation = 0.003f; // 波动比例（0~100%）
        private float _CurrentAmount; // 当前金额
        [Button("Test1")]
        public void Test1()
        {
           List<float> amounts = CardsController.Instance.DistributeCash(totalAmount, groupCount);
           _CurrentAmount = 0;
            for(int i = 0; i < amounts.Count; i++)
            {
                float tGetone1 = (float)Math.Round(amounts[i] * 0.01f, 3);
                _CurrentAmount += tGetone1;
                Debug.Log($"第{i+1}组金额: {tGetone1}");
                //Debug.Log($"第{i+1}组金额: {amounts[i]}");
            }

            if (_CurrentAmount <= totalAmount * 0.01f)
            {
                for (int i = 0; i < amounts.Count; i++)
                {
                    Log.Warning($"第{i+1}组金额: {amounts[i].ToString("0.#####")}");
                }
            }
        }
        public double aValue = 0.01;
        public int aGroupCount = 3;
        [Button("Test2")]
        public void Test2()
        {
            Log.Warning($"{ PlayGameExtensions.FormatPercentage(aValue, aGroupCount)}");
        }
    }
}
