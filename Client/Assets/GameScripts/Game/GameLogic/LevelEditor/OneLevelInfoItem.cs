using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GameLogic
{
    public class OneLevelInfoItem : MonoBehaviour
    {
        public TextMeshProUGUI levelNameText;

        public void Init(MapConfig data)
        {
            levelNameText.text = data.LevelName;
        }
    }

    public class Item_LevelInfo : UILoopGridItemWidget, IListDataItem<MapConfig>
    {
        public void SetItemData(MapConfig data)
        {
            OneLevelInfoItem item = gameObject.GetComponent<OneLevelInfoItem>();
            item.Init(data);
        }
    }
}
