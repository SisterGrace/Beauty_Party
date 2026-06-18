using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Y_Base;
using TK_Engine;

namespace GameLogic
{
    public class EditorScene : SingletonBehaviour<EditorScene>
    {
        public GameObject LevelEditorUI;
        public RectTransform EditBoard;
        public RectTransform AllUnits;
        public Camera EditorCamera;
        #region 网格基础数据
        [Header("网格基础数据")]
        public int LayerRow = 14;
        public int LayerColumn = 14;

        public int UnitWidth = 70;
        public int UnitHeight = 70;
        
        private int mStartPosX;
        private int mStartPosY;

        public int MaxCardType = 18;
        #endregion
        private void Awake()
        {
            GameModule.Timer.AddTimer(time =>
            {
                //加载关卡编辑器UI
                GameModule.UI.ShowUI<Panel_EditorHome>();
                StartLevelEditor(false);
            }, .002f);
           
        }

        public void StartLevelEditor(bool IsStart)
        {
            LevelEditorUI.gameObject.SetActive(IsStart);
        }
        /// <summary>
        /// 获取网格的起始位置
        /// </summary>
        /// <returns></returns>
        public Vector2 GetStartPos()
        {
            mStartPosX = -Mathf.RoundToInt((LayerColumn - 1) * UnitWidth * 0.5f);
            mStartPosY = Mathf.RoundToInt((LayerRow - 1) * UnitHeight * 0.5f);
            return new Vector2(mStartPosX, mStartPosY);
        }
    }
}
