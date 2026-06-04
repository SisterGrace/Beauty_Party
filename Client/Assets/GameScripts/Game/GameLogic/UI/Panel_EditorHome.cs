using TK_Engine;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_EditorHome : UIWindow
	{
		private UILoopGridWidget<Item_LevelInfo,MapConfig> _levelInfoGrid;
		//所有关卡信息
		private List<MapConfig> _allLevelInfo = new();
		protected override void OnRefresh()
		{
			AllLevelInfoJsonToChange();
		}

		protected override void RegisterEvent()
		{
		}

		private void StartCreatLevelBase()
		{
			if (!LevelEdirtorController.Instance.FindLevelInFolder("Level_" + mLevelname.text))
			{
				MapConfig config = new MapConfig("Level_" + mLevelname.text, 0, new List<WeightData>(),
					new List<LayerData>(), new List<StackData>());
				Close();
				GameModule.UI.ShowUI<Panel_EditorLevel>(config);
				//LevelEdirtorController.Instance.CreatGrid(true,config);
			}
			else
			{
				GameEvent.Send(EventType.ShowTips, "该关卡已存在！",false);
			}
		}
		private void AllLevelInfoJsonToChange()
		{
			string folderPath = Application.dataPath + "/AssetRaw/LevelInfo/";
			var mapJsonlist = Utility.File.GetJsonFilesInDirectory(folderPath);
			mapJsonlist = mapJsonlist.OrderBy(x=>int.Parse(x.Split(new char[]{'_', '.'})[1])).ToList();
			foreach (var item in mapJsonlist)
			{
				string jsonStr =  System.IO.File.ReadAllText(item);
				MapConfig mapConfig = Utility.Json.ToObject<MapConfig>(jsonStr);
				_allLevelInfo.Add(mapConfig);
			}

			CreatLevelIInfo();
		}

		private void CreatLevelIInfo()
		{
			if(_allLevelInfo.Count ==0) return;
			_levelInfoGrid = CreateWidget<UILoopGridWidget<Item_LevelInfo, MapConfig>>(mAllLevlelInfo);
			_levelInfoGrid.itemBase = mOneLevelInfo;
			_levelInfoGrid.SetDatas(_allLevelInfo);
			_levelInfoGrid.funcOnItemClick += index =>
			{
				MapConfig config = _levelInfoGrid.datas[index];
				Close();
				GameModule.UI.ShowUI<Panel_EditorLevel>(config);
				//LevelEdirtorController.Instance.CreatGrid(true,config);
			};
		}

		private bool isCheckInPut()
		{
			if (string.IsNullOrEmpty(mLevelname.text))
			{
				GameEvent.Send(EventType.ShowTips, "请输入关卡名称！",false);
				return false;
			}
			return true;
		}
		protected override void Close()
		{
			base.Close();
		}
	}
}
