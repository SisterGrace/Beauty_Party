using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Y_Base;
using TK_Engine;
using System;
using System.Linq;
using Config.common;
using Config.config;
using Cysharp.Threading.Tasks;
using AudioType = TK_Engine.AudioType;

namespace GameLogic
{
    public class LevelController : Singleton<LevelController>
    {
        //所有关卡配置信息
        private List<MapConfig> _mapConfigs = new List<MapConfig>();
        public List<MapConfig> MapConfigs => _mapConfigs;
        private MapConfig _currentMapConfig;
        private CfgLevelInfo _currentLevelInfo;

        public CfgLevelInfo CurrentLevelInfo => _currentLevelInfo;

        //所有卡牌类型
        private List<int> _typeList;

        //卡牌的临时分组
        private Dictionary<int, List<CardCell>> _tempGruop = new Dictionary<int, List<CardCell>>();
        private List<CoordData> _allCoords = new List<CoordData>();

        //记录当前关卡选择的图片
        public OneSelectPhotoInfo CurLevelSelectPhotoInfo;
        #region 关卡加载相关

        private GameLoadingState _gameLoadingState = GameLoadingState.None;
        public GameLoadingState GameLoadingState => _gameLoadingState;
        private Action<GameLoadingState> _updateCb;

        public void SetUpdateCb(Action<GameLoadingState> updateCb)
        {
            _updateCb = null;
            _updateCb = updateCb;
        }

        #endregion

        #region 节点相关

        private Transform _layerParent;
        private Transform _stackParent;
        private int _startPosX;
        private int _startPosY;
        private int _startDepth;

        #endregion

        #region sdk计数相关
        //广告复活次数
        public int UseAdReviveCount = 0;
        //关卡内触发激励视频次数
        public int GameRvCount = 0;
        //关卡进入时间
        public long GameEnterTime = 0;
        #endregion

        #region 关卡循环相关
        private List<CfgLevelInfo> _loopLevelInfos1 = new List<CfgLevelInfo>();
        #endregion


        public static CfgLevelInfo GetCurrentLevelInfo()
        {
            if (!UserController.Instance.IsLoopLevel)
            {
                int levelIndex = UserController.Instance.Level - 1;
                return ConfigLoader.Instance.Tables.TbCfgLevelInfo.Get(levelIndex);
            }
            else
            {
                return Instance.GetLoopLevelInfo();
            }

        }

        public Transform GetMaxLayerTrans()
        {
            int index = _layerParent.childCount - 1;
            return _layerParent.GetChild(index);
        }
        public void InitLevel()
        {
            AllJsonToLevelInfo();
            //初始化左上角坐标
            _startPosX =
                -Mathf.RoundToInt((ConfigLoader.Constant.LayerColumn - 1) * ConfigLoader.Constant.UnitWidth * 0.5f) +
                ConfigLoader.Constant.StartPosOffestX;
            _startPosY =
                Mathf.RoundToInt((ConfigLoader.Constant.LayerRow - 1) * ConfigLoader.Constant.UnitHeight * 0.5f) -
                ConfigLoader.Constant.StartPosOffestY;
            _loopLevelInfos1.Clear();
            for (int i = 0; i < ConfigLoader.Instance.Tables.TbCfgLevelInfo.DataList.Count; i++)
            {
                var levelInfo = ConfigLoader.Instance.Tables.TbCfgLevelInfo.DataList[i];
                _loopLevelInfos1.Add(levelInfo);
            }


        }

        private void AllJsonToLevelInfo()
        {
            var levels = ConfigLoader.Instance.Tables.TbCfgLevelInfo.DataList;
            foreach (var item in levels)
            {
                var levelInfo = GameModule.Resource.LoadAsset<TextAsset>(item.LevelName);
                var ret = levelInfo.text;
                MapConfig levelConfig = Utility.Json.ToObject<MapConfig>(ret);
                _mapConfigs.Add(levelConfig);
                if (levelConfig.TotalCardCount % 3 != 0)
                {
                    Log.Warning($"{levelConfig.LevelName}的卡牌数量不是3的倍数");
                }
                //输出金卡牌数量为8的关卡
                _currentMapConfig = levelConfig;
                int goldCount = 0;
                HasGoldCard(out goldCount);
                if (goldCount > 0)
                {
                    Log.Warning($"{levelConfig.LevelName}关卡有金块卡牌");
                }
                 CheckCardTypeOverflow(levelConfig, 17,item);
            }
        }
        
        private void CheckCardTypeOverflow(MapConfig config, int threshold,CfgLevelInfo info)
        {
            if (config.CardWeights != null)
            {
                foreach (var w in config.CardWeights)
                {
                    if (w.CardType > threshold)
                    {
                        Log.Warning($"{config.LevelName} CardWeights中存在cardType={w.CardType}，超过{threshold}");
                        //return;
                    }
                }
            }

            if (config.Layers != null)
            {
                foreach (var layer in config.Layers)
                {
                    if (layer.Coords == null) continue;
                    foreach (var coord in layer.Coords)
                    {
                        if (coord.CardTypes == null) continue;
                        foreach (var ct in coord.CardTypes)
                        {
                            if (ct > threshold)
                            {
                                Log.Info($"{config.LevelName} Layer坐标({coord.X},{coord.Y})中存在cardType={ct}，超过{threshold}");
                                //return;
                            }
                        }
                    }
                }
            }

            if (config.Stacks != null)
            {
                foreach (var stack in config.Stacks)
                {
                    if (info.IsCustomizable)
                    {
                        if (stack.CustomTypes == null || stack.CustomTypes.Count == 0)
                        {
                            Log.Warning($"{config.LevelName} Stack中存空的自定义CustomTypes");
                        }
                    }
                    if (stack.CustomTypes == null) continue;
                    foreach (var ct in stack.CustomTypes)
                    {
                        if (ct > threshold)
                        {
                            Log.Warning($"{config.LevelName} Stack中存在cardType={ct}，超过{threshold}");
                            return;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// 加载当前关卡
        /// </summary>
        public void LoadMap(Transform layerParent, Transform stackParent)
        {
            GameRvCount = 0;
            Frist_Loss_Progress = 0;
            IsFirstLose = false;
            GameEnterTime = TimeUtil.GetTimeStamp();
            UserController.Instance.GetDayGameCount++;
            SetGameLevelInfo();
            _layerParent = layerParent;
            _stackParent = stackParent;
            _startDepth = GameModule.UI.GetWindowDepth(typeof(Panel_Game));
            _allCoords.Clear();
            //创建卡牌类型
            CreateCardTypes(_currentMapConfig, _currentLevelInfo);
            //创建层级卡牌
            CreatLayerObj(_currentMapConfig);
            //创建堆叠卡牌
            CreateStackCards(_currentMapConfig);
            if (!_currentLevelInfo.IsCustomizable && (_currentLevelInfo.TimeLevel || _currentLevelInfo.CatchButterfly))
            {
                CardsController.Instance.RemedyCardType(CardsController.Instance.Cards);
            }

            _gameLoadingState = GameLoadingState.None;
            Dictionary<int, List<CardCell>> tempGruop = new Dictionary<int, List<CardCell>>();
            foreach (var item in CardsController.Instance.Cards)
            {
                for (int i = 0; i < item.AllCardTypes.Count; i++)
                {
                    if (!tempGruop.ContainsKey(item.AllCardTypes[i]))
                    {
                        tempGruop.Add(item.AllCardTypes[i], new List<CardCell>());
                    }
                    tempGruop[item.AllCardTypes[i]].Add(item);
                }
            }
            UseAdReviveCount = 0;
        }

        //检测当前关卡是否含有金块卡牌
        public bool HasGoldCard(out int goldCount)
        {
            /*int levelIndex = UserController.Instance.Level - 1;
            _currentMapConfig = _mapConfigs[levelIndex];*/
            int goldCountTemp = 0;
            for (int i = 0; i < _currentMapConfig.Layers.Count; i++)
            {
                for (int j = 0; j < _currentMapConfig.Layers[i].Coords.Count; j++)
                {
                    var coord = _currentMapConfig.Layers[i].Coords[j];
                    if (coord.CursSpecial != null && coord.CursSpecial.FunctionCardTypeList != null &&
                        coord.CursSpecial.FunctionCardTypeList.Contains(FunctionCardType.GoldCube))
                    {
                        goldCountTemp++;
                    }
                }
            }

            for (int i = 0; i < _currentMapConfig.Stacks.Count; i++)
            {
                if (_currentMapConfig.Stacks[i].GoldIndexs != null && _currentMapConfig.Stacks[i].GoldIndexs.Count > 0)
                {
                    goldCountTemp += _currentMapConfig.Stacks[i].GoldIndexs.Count;
                }
            }
            if (goldCountTemp > 0)
            {
                goldCount = goldCountTemp;
                return true;
            }

            goldCount = 0;
            return false;
        }

        #region 关卡物体生成相关

        private void CreateCardTypes(MapConfig mapConfig, CfgLevelInfo levelInfo)
        {
            //是否设置棋子随机样式
            bool isSetCustomType = levelInfo.IsCustomizable;
            if (!isSetCustomType)
            {
                _gameLoadingState = GameLoadingState.CreatLayer;
                _updateCb?.Invoke(_gameLoadingState);
                //总卡牌数量
                int totalCardCount = mapConfig.TotalCardCount;
                //总组数
                int totalCardGroup = totalCardCount / 3;
                int totalWeight = 0;
                for (int i = 0; i < mapConfig.CardWeights.Count; i++)
                {
                    totalWeight += mapConfig.CardWeights[i].Weight;
                }

                _typeList = new List<int>();
                //如果是限时关卡,需要加入雪花类型卡牌
                if (levelInfo.TimeLevel)
                {
                    int snowGoup = levelInfo.SnowGroup > 0 ? levelInfo.SnowGroup : Mathf.Max(totalCardGroup / ConfigLoader.Constant.SnowGroupFactor, 1);
                    for (int i = 0; i < snowGoup * 3; i++)
                    {
                        _typeList.Add(ConfigLoader.Constant.SnowCard);
                    }

                    totalCardGroup -= snowGoup;
                    totalCardCount -= snowGoup * 3;
                }

                //如果是捕捉蝴蝶关卡,需要加入网兜类型卡牌
                if (levelInfo.CatchButterfly)
                {
                    int netGoup = levelInfo.NetBagGroup > 0 ? levelInfo.NetBagGroup : Mathf.Max(totalCardGroup / ConfigLoader.Constant.NetbagCardFactor, 1);
                    for (int i = 0; i < netGoup * 3; i++)
                    {
                        _typeList.Add(ConfigLoader.Constant.NetbagCard);
                    }

                    totalCardGroup -= netGoup;
                    totalCardCount -= netGoup * 3;
                }

                List<int> chosenTypeList = new List<int>();
                chosenTypeList.Add(1);
                List<int> otherTypeList = new List<int>();
                for (int i = 2; i <= 17; i++)
                {
                    otherTypeList.Add(i);
                }

                int otherCount = Mathf.Min(mapConfig.CardWeights.Count - chosenTypeList.Count, 17 - 2);
                for (int i = 0; i < otherCount; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, otherTypeList.Count);
                    chosenTypeList.Add(otherTypeList[randomIndex]);
                    otherTypeList.RemoveAt(randomIndex);
                }

                int cardCount = 0;
                //设置权重
                for (int i = 0; i < mapConfig.CardWeights.Count; i++)
                {
                    var weightData = mapConfig.CardWeights[i];
                    int cardType = chosenTypeList[i % chosenTypeList.Count];
                    int weightGruop = Mathf.RoundToInt((weightData.Weight / (float)totalWeight) * totalCardGroup);
                    cardCount = i < mapConfig.CardWeights.Count - 1 ? weightGruop * 3 : totalCardCount;
                    cardCount = Mathf.Clamp(cardCount, 0, totalCardCount);
                    if (cardCount > 0)
                    {
                        for (int j = 0; j < cardCount; j++)
                        {
                            _typeList.Add(cardType);
                        }
                    }

                    totalCardCount -= cardCount;
                }
            }
            //首先已经有所有的类型数组

            if (mapConfig.Layers != null && mapConfig.Layers.Count > 0)
            {
                for (int i = 0; i < mapConfig.Layers.Count; i++)
                {
                    for (int j = 0; j < mapConfig.Layers[i].Coords.Count; j++)
                    {
                        var data = mapConfig.Layers[i].Coords[j];
                        bool isEmpty = data.CursSpecial.FunctionCardTypeList == null;
                        var functionCardTypeList = new List<FunctionCardType>();
                        if (!isEmpty)
                        {
                            functionCardTypeList.AddRange(data.CursSpecial.FunctionCardTypeList);
                        }
                        CoordData newData = new CoordData(data.X, data.Y, data.Count,
                            new SpecialData(data.CursSpecial._lockCardType, isEmpty ? null : functionCardTypeList,
                                data.CursSpecial.CombinedId, data.CursSpecial.BombCount, data.CursSpecial.IsRoot));
                        if (isSetCustomType)
                        {
                            newData.SetCoordType(data.CardTypes);
                        }
                        newData.LayerIndex = i;
                        _allCoords.Add(newData);
                    }
                }
            }

            if (mapConfig.Stacks != null && mapConfig.Stacks.Count > 0)
            {
                for (int i = 0; i < mapConfig.Stacks.Count; i++)
                {
                    var stackData = mapConfig.Stacks[i];
                    for (int j = stackData.Count - 1; j >= 0; j--)
                    {

                        bool isGold = false;
                        if (stackData.GoldIndexs != null && stackData.GoldIndexs.Contains(j))
                        {
                            isGold = true;
                        }
                        CoordData newData = new CoordData(stackData.Coord.X, stackData.Coord.Y, cursSpecial: new SpecialData(LockCardType.None, isGold ? new List<FunctionCardType> { FunctionCardType.GoldCube } : null));
                        if (isSetCustomType)
                        {
                            List<int> tempTypeList = new List<int>();
                            tempTypeList.Add(stackData.CustomTypes[j]);
                            newData.SetCoordType(tempTypeList);
                        }
                        _allCoords.Add(newData);
                    }
                }
            }

            if (isSetCustomType) return;
            List<int> _tempIndex = new();
            foreach (var t in _typeList)
            {
                if (ConfigLoader.Constant.RejectTypes.Contains(t))
                {
                    var tempList = _allCoords.FindAll(i => i.CursSpecial.IsReject() == false && i.CardTypes == null);
                    var coord = tempList.RandomFromAllList();
                    List<int> typs = new(coord.Count);
                    typs.Add(t);
                    coord.SetCoordType(typs);
                    int index = _typeList.IndexOf(t);
                    _tempIndex.Add(index);
                }
            }
            foreach (var index in _tempIndex)
            {
                _typeList.RemoveAt(index);
            }
            foreach (var coord in _allCoords)
            {
                if (coord.CardTypes != null && coord.Count == coord.CardTypes.Count) continue;
                List<int> typs = new(coord.Count);
                for (int i = 0; i < coord.Count; i++)
                {
                    int cardTy = _typeList.RandomFromAllList();
                    typs.Add(cardTy);
                }
                coord.SetCoordType(typs);
            }

        }

        private void CreatLayerObj(MapConfig mapConfig)
        {
            if (mapConfig.Layers != null && mapConfig.Layers.Count > 0)
            {
                for (int i = 0; i < mapConfig.Layers.Count; i++)
                {
                    CreatLayerObj(mapConfig.Layers[i], i);
                }
            }
        }
        private void CreateStackCards(MapConfig mapConfig)
        {
            _gameLoadingState = GameLoadingState.CreatStack;
            _updateCb?.Invoke(_gameLoadingState);
            if (mapConfig.Stacks != null && mapConfig.Stacks.Count > 0)
            {
                for (int i = 0; i < mapConfig.Stacks.Count; i++)
                {
                    CreatStackObj(mapConfig.Stacks[i], i);
                }
            }
        }

        //生成每一层的
        private void CreatLayerObj(LayerData data, int index)
        {
            GameObject layerObj = new GameObject("Layer" + index);
            layerObj.layer = 5;
            RectTransform layerRect = layerObj.AddComponent<RectTransform>();
            layerRect.SetParent(_layerParent);
            layerRect.localScale = Vector3.one;
            layerRect.localPosition = Vector3.zero;
            layerRect.sizeDelta = Vector2.zero;
            var layerCoords = _allCoords.FindAll(a => a.LayerIndex == index);
            //临时分组的
            _tempGruop.Clear();
            for (int i = 0; i < data.Coords.Count; i++)
            {
                var coord = layerCoords.Find(a => a.X == data.Coords[i].X && a.Y == data.Coords[i].Y);
                CardCell cell = CreateCardObj(coord, layerRect);
                cell.SetAnchoredPos(GetLayerLocation(data.Coords[i]));
                cell.RecordLocation();
                var coverIDs = GetCoverIDs(data.Coords[i]);
                cell.SetCoverIds(coverIDs);
                CardsController.Instance.PushUnitCoverStack(cell);
                //将拉链、藤蔓、加入临时分组
                AddTempGruop(cell.MyCoordData, cell);
            }

            //在将临时分组的卡牌按照特定顺序（从左往右）加入分组列表
            CardsController.Instance.AddGruopList(_tempGruop);
        }

        //加入临时分组
        private void AddTempGruop(CoordData data, CardCell card)
        {
            if (data.CursSpecial._lockCardType == LockCardType.Vines ||
                (data.CursSpecial.FunctionCardTypeList != null && data.CursSpecial
                    .FunctionCardTypeList.Contains(FunctionCardType.Zipper)))
            {
                int gruopid = card.MySpecialCardLogic.GetGroupId;
                List<CardCell> templist;
                if (!_tempGruop.ContainsKey(gruopid))
                {
                    _tempGruop[gruopid] = new List<CardCell>();
                }

                templist = _tempGruop[gruopid];
                templist.Add(card);
            }
        }

        private void CreatStackObj(StackData data, int index)
        {
            GameObject stackObj = new GameObject("Stack" + index);
            stackObj.layer = 5;
            RectTransform stackRect = stackObj.AddComponent<RectTransform>();
            stackRect.SetParent(_stackParent);
            stackRect.localScale = Vector3.one;
            stackRect.localPosition = Vector3.zero;
            stackRect.sizeDelta = Vector2.zero;
            Stack<CardCell> tStack = CardsController.Instance.GetCardFreeStack(index);
            var stackallCoords = _allCoords.FindAll(a => a.X == data.Coord.X && a.Y == data.Coord.Y);
            for (int i = 0; i < stackallCoords.Count; i++)
            {
                CardCell cell = CreateCardObj(stackallCoords[i], stackRect);
                cell.SetAnchoredPos(GetStackLocation(data.Coord, data.Direction, stackallCoords.Count - 1 - i));
                cell.RecordLocation();
                cell.StackIndex = index;
                if (i < stackallCoords.Count - 1)
                {
                    cell.CoverCell();
                }

                tStack.Push(cell);
            }
        }

        //生成每张卡牌的
        private CardCell CreateCardObj(CoordData data, Transform parent)
        {
            CardCell card = GameModule.Resource.LoadGameObject("CardCell", parent).GetComponent<CardCell>();
            card.SetParent(parent, true);
            card.InitCard(data.CardTypes, data);
            card.SetVisible(true);
            CardsController.Instance.AddCard(card);
            return card;
        }
        private List<int> GetCoverIDs(CoordData Coord)
        {
            List<int> coverIDs = new List<int>();
            coverIDs.Add(Coord.Y * ConfigLoader.Constant.LayerRow + (Coord.X));
            coverIDs.Add(Coord.Y * ConfigLoader.Constant.LayerRow + (Coord.X + 1));
            coverIDs.Add((Coord.Y + 1) * ConfigLoader.Constant.LayerRow + (Coord.X));
            coverIDs.Add((Coord.Y + 1) * ConfigLoader.Constant.LayerRow + (Coord.X + 1));
            return coverIDs;
        }

        //获取卡牌在网格当中的位置
        public Vector2 GetLayerLocation(CoordData Coord)
        {
            Vector2 pos = new Vector2();
            pos.x = _startPosX + Coord.X * ConfigLoader.Constant.CardCellOffestX;
            pos.y = _startPosY - Coord.Y * ConfigLoader.Constant.CardCellOffestY;
            return pos;
        }

        //获取卡牌在堆叠当中的位置
        private Vector2 GetStackLocation(CoordData Coord, int dir, int index)
        {
            Vector2 startPos = new Vector2(Coord.X, Coord.Y);
            Vector2 offset = Vector2.zero;
            switch (dir)
            {
                case 1:
                    offset.x = 0;
                    offset.y = ConfigLoader.Constant.FreeStackDelta;
                    break; //上
                case 2:
                    offset.x = 0;
                    offset.y = -ConfigLoader.Constant.FreeStackDelta;
                    break; //下
                case 3:
                    offset.x = -ConfigLoader.Constant.FreeStackDelta;
                    offset.y = 0;
                    break; //左
                case 4:
                    offset.x = ConfigLoader.Constant.FreeStackDelta;
                    offset.y = 0;
                    break; //右
            }

            return startPos + offset * index;
        }

        #endregion

        public void ExitLevel(bool isWin, bool isisDownload = false)
        {
            if (isWin)
            {
                UserController.Instance.Level++;
                //添加图片
                UserController.Instance.AddUserPhotoData(CurLevelSelectPhotoInfo.Id, 0, isisDownload);
                SDKManager.Instance.GetLevelChooseReported = false;
            }
            _currentMapConfig = null;
            _currentLevelInfo = null;
            _typeList = null;
            _tempGruop.Clear();
            UseAdReviveCount = 0;
            GameRvCount = 0;
            CardsController.Instance.ExitGame();
        }

        #region 关卡相关方法 
    
        //是否首次失败
        public bool IsFirstLose = false;
        //首次失败进度
        public float Frist_Loss_Progress = 0;

        public void FirstLose()
        {
            if (!IsFirstLose)
            {
                IsFirstLose = true;
                Frist_Loss_Progress = GetLevelProgressPercent();
            }
        }
        /// <summary>
        /// 获取当前关卡进度百分比
        /// </summary>
        /// <returns></returns>
        public float GetLevelProgressPercent()
        {
            int allCount = _currentMapConfig.TotalCardCount;
            int curCount = CardsController.Instance.Cards.FindAll(i => i.IsInSolt == false).Count;
            return (float)curCount / allCount;
        }
        /// <summary>
        /// 获取游戏关卡游玩时间
        /// </summary>
        /// <returns></returns>
        public long GetGameTime(long CutTime)
        {
            return CutTime - GameEnterTime;
        }
        #endregion

        #region 关卡循环模式
       
        //当进入循环的时候，启动新的关卡配置id
        private CfgLevelInfo GetLoopLevelInfo()
        {
            var count = _loopLevelInfos1.Count;
            if (UserController.Instance.LoopLevelId_1 >= count || UserController.Instance.LoopLevelId_1 < 0)
            {
                UserController.Instance.LoopLevelId_1 = ((UserController.Instance.LoopLevelId_1 % count) + count) % count;
            }
            return _loopLevelInfos1[UserController.Instance.LoopLevelId_1];
        }
        private void SetGameLevelInfo()
        {
            if (!UserController.Instance.IsLoopLevel)
            {
                int levelIndex = UserController.Instance.Level - 1;
                _currentLevelInfo = ConfigLoader.Instance.Tables.TbCfgLevelInfo.Get(levelIndex);
                _currentMapConfig = _mapConfigs[levelIndex];
            }
            else
            {
                _currentLevelInfo = GetLoopLevelInfo();
                _currentMapConfig = _mapConfigs[_currentLevelInfo.LevelId];
            }
        }

        #endregion


    }

    //玩法加载状态
    public enum GameLoadingState
    {
        None = 0,
        CreatLayer = 1, //创建层级卡牌
        CreatStack = 2, //创建堆叠卡牌
    }
}