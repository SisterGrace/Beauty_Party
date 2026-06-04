using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using Config.common;
using GameBase;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Panel_VIP : UIWindow
	{
        int _selectedVIP = 0;
        private int _curGetVIPLevel => UserController.Instance.GetVIPLevel;
        //所有VIPItem
        private UIListWidget<Widget_OneVipItem, VIPItemData> _vipItemList;
        //VIP界面的启动来源，1-从关卡启动，2-从抽卡启动
        private int _launchSource = 0;
		protected override void OnRefresh()
        {
            _launchSource = 0;
            if (userDatas.Length > 0)
            {
                _launchSource = (int)userDatas[0];
            }
            CreatVIPItemList();
        }

		protected override void RegisterEvent()
		{
		}
        private void CreatVIPItemList()
        {
            if (_vipItemList == null)
            {
                _vipItemList = CreateWidget<UIListWidget<Widget_OneVipItem, VIPItemData>>(mAllItems.gameObject);
            }
            var vipcfg = ConfigLoader.Instance.Tables.TbCfgGameVIP.DataList;
            List<VIPItemData> vipItemList = new List<VIPItemData>();
            for (int i = 0; i < vipcfg.Count; i++)
            {
                string timeNode = $"bg_vip_djs0{i+1}";
                var vipItemData = new VIPItemData(vipcfg[i].Id, vipcfg[i].VipName, vipcfg[i].VipBg, vipcfg[i].VipIcon, timeNode,
             vipcfg[i].Price.ID, vipcfg[i].VipDays, vipcfg[i].VipInfos);
                vipItemList.Add(vipItemData);
            }
            _vipItemList.SetDatas(vipItemList);
        }
        protected override void Close()
        {
            base.Close();
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameModule.UI.RemoveOrderPanel<Panel_VIP>(true);
            //就是如果当前VIP等级不大于0，就不会跳转和更新
            if (UserController.Instance.GetVIPLevel <= 0) return;
            switch (_launchSource)
            {
                case 1:
                    //直接从关卡启动
                    GameEvent.Send(EventType.EnterGame);
                    break;
                case 2:
                    //刷新抽卡界面的按钮状态
                    GameEvent.Send(EventType.UpdatePackDisplay);
                    //刷新抽卡红点
                    bool isGachaRed = UserController.Instance.CheckGachaAdTime();
                    GameEvent.Send(EventType.SetGachaRedPoint, isGachaRed, "");
                    break;
            }
		}
		void SelectVIP(int selected)
		{
            _selectedVIP = selected;
            
		} 
        
	}
}
