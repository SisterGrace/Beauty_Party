using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;
using System;
using GameBase;
using TK_Engine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    public class EditStack : MonoBehaviour,IBeginDragHandler, IEndDragHandler, IDragHandler
    {
       public TextMeshProUGUI DirectionText;
       public TMP_InputField CountText;
       public UIButton deleteButton;
       private RectTransform _rect;
       private RectTransform _rectParent;
       private bool _isDragging;
       private int _curCount;
       private int _direction;
       private List<int> _alltypes;
       public int Direction=>_direction;
       public List<int> AllTypes => _alltypes;
       private List<int> _goldIndex;
       public List<int> GoldIndexs => _goldIndex;
       public Vector2Int Pos => new Vector2Int((int)_rect.anchoredPosition.x, (int)_rect.anchoredPosition.y);
       #region 事件监听
       public Action DelChangeCount;
       public Action<EditStack> DeletStack;
       public Action<bool> DelDragging;
       #endregion
      
       private Image _image;
       public int Count
       {
           get
           {
               /*int count = 0;
               int.TryParse(CountText.text, out count);*/
               return _curCount;
           }
       }
       public void InitState()
       {
           _rect = GetComponent<RectTransform>();
           _rectParent = GetComponentInParent<RectTransform>();
           deleteButton.AddListener(ClickDelete);
           CountText.onEndEdit.AddListener(OnInputCount);
           _image = GetComponent<Image>();
       }
        //自定义堆叠类型
       public void SetAllTypes(List<int> pAllTypes)
       {
           if (pAllTypes != null && pAllTypes.Count > 0)
           {
               List<int> temp = new List<int>();
               temp.AddRange(pAllTypes);
               _alltypes = temp;
           }
       }

       private void ClickDelete(GameObject obj)
       {
           DeletStack?.Invoke(this);
       }
    
       private void OnInputCount(string pCountStr)
       {
           if (pCountStr.Contains(";"))
           {
               string[] infos = pCountStr.Split(";");
               int temp = 0;
               int.TryParse(infos[0], out temp);
               temp = Mathf.Clamp(temp, 1, 30);
               string[] goldsInfo = infos[1].Split(",");
               if (_goldIndex == null)
               {
                   _goldIndex = new List<int>();
               }
               for(int i=0;i<goldsInfo.Length;i++)
               {
                   int index = 1;
                   int.TryParse(goldsInfo[i], out index);
                   _goldIndex.AddWithoutRepetition(index-1);
               }
               CountText.text = $"{pCountStr}";
               _curCount = temp;
           }
           else
           {
               int temp = 0;
               int.TryParse(pCountStr, out temp);
               temp = Mathf.Clamp(temp, 1, 30);
               CountText.text = $"{temp}";
               _curCount = temp;
           }
           DelChangeCount?.Invoke();
       }

      public void SetDirection(int pDirection)
       {
           _direction = pDirection;
           switch (_direction)
           {
               case 1:
                   DirectionText.text = "上";
                   break;
               case 2:
                   DirectionText.text = "下";
                   break;
               case 3:
                   DirectionText.text = "左";
                   break;
               case 4:
                   DirectionText.text = "右";
                   break;
           }
       }

       public void SetCount(string info)
       {
           CountText.text = info;
           if (info.Contains(";"))
           {
               string[] infos = info.Split(";");
               if (int.TryParse(infos[0], out int temp))
                   _curCount = Mathf.Clamp(temp, 1, 30);
               if (infos.Length > 1)
               {
                   string[] goldsInfo = infos[1].Split(",");
                   if (_goldIndex == null)
                       _goldIndex = new List<int>();
                   _goldIndex.Clear();
                   for (int i = 0; i < goldsInfo.Length; i++)
                   {
                       if (int.TryParse(goldsInfo[i], out int index))
                           _goldIndex.AddWithoutRepetition(index - 1);
                   }
               }
           }
           else
           {
               if (int.TryParse(info, out int temp))
                   _curCount = Mathf.Clamp(temp, 1, 30);
           }
       }
       private Vector2 _dragOffset;

       public void OnBeginDrag(PointerEventData eventData)
       {
           _isDragging = true;
           DelDragging?.Invoke(_isDragging);
           //计算偏移
           RectTransformUtility.ScreenPointToLocalPointInRectangle(
               _rectParent,
               eventData.position,
               GameModule.UI.UICamera,
               out Vector2 localPoint);
           _dragOffset = _rect.anchoredPosition - localPoint;
       }

       public void OnDrag(PointerEventData eventData)
       {
           if (_isDragging)
           {
               Vector2 movePos;
               RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectParent, eventData.position,
                   GameModule.UI.UICamera, out movePos);
               // 计算目标位置（包含初始偏移）
               Vector2 targetPos = movePos;
               targetPos.x = Mathf.Round(targetPos.x/70)*70;
               targetPos.y = Mathf.Round(targetPos.y/70)*70;
               _rect.anchoredPosition += targetPos;
           }
       }

       public void OnEndDrag(PointerEventData eventData)
       {
           _isDragging = false;
           DelDragging?.Invoke(_isDragging);
       }
       
    }
}
