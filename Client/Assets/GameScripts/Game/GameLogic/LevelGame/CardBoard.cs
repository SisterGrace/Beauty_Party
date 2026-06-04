using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK_Engine;

namespace GameLogic
{
    public class CardBoard : MonoBehaviour
    {
        private int _startPosX;
        private int _startPosY;

        private RectTransform _layerRect;
        private RectTransform _stackRect;

        public static CardBoard Get(GameObject obj, Transform layer=null, Transform stack=null)
        {
            if (!obj)
            {
                Log.Warning("CardBoard.Get: CardBoard object not found!");
            }
            CardBoard board = obj.GetComponent<CardBoard>();
            if (board == null)
            {
                board = obj.AddComponent<CardBoard>();
                if (layer != null && stack != null)
                {
                    board.InitCardBoard(layer, stack);
                }
            }
            return board;
        }

        public void InitCardBoard(Transform layer, Transform stack)
        {
            
        }
    }
}
