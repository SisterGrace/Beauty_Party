using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Y_Base;
using TK_Engine;
using Config.common;

namespace GameLogic
{
    public class LevelEdirtorController : Singleton<LevelEdirtorController>
    {
        public MapConfig CurMapConfig;
        //所有编辑单元格
        private List<EditUnit> _allUnits;
        //所有编辑单元格的坐标
        private List<Vector2> _allUnitPos;
        //当前层卡牌位置
        private LayerData _currentLayer;
        //当前所在层
        private int _currentLayerIndex;
        public int CurrentLayerIndex => _currentLayerIndex;
        //堆叠卡牌数据
        private List<EditStack> mStacks = new List<EditStack>();
        //堆叠卡牌父物体
        public Transform MyStackParent;
        //当前卡牌类型数量
        public int CardTypeCount;
        private bool _isDraggingStack = false;
        //上一层编辑的单元格
        private List<EditUnit> _lastEditUnits = new List<EditUnit>();
        //当前层编辑的单元格
        private List<EditUnit> _currentEditUnits = new List<EditUnit>();
        //当前卡牌的锁类型
        public LockCardType CurrentLockCardType = LockCardType.None;
        //当前卡牌功能类型
        public List<FunctionCardType> CurrentFunctionCardTypes;
        //当前贝壳、双面牌所需的数量
        public int ShellOrReversibleCount = 0;
        //当前所选卡牌炸弹引爆数量
        public int BombCount = 0;
        //当前分组id
        private int _currentGroupId = 0;
        //当前自定义样式列表
        public List<int> CustomTypes;
        //创建网格
        public void CreatGrid(bool isCreate,MapConfig mapConfig=null,Transform parent =null)
        {
            EditorScene.Instance.StartLevelEditor(isCreate);
            if (isCreate)
            {
                CurMapConfig = mapConfig;
                _allUnits = new();
                _allUnitPos = new();
                mStacks.Clear();
                //CurrentFunctionCardTypes = new List<FunctionCardType>();
                var allUnitsParent = EditorScene.Instance.AllUnits;
                Vector2 startPos = EditorScene.Instance.GetStartPos();
                for (int tVer = 0; tVer < ConfigLoader.Constant.LayerRow; tVer++)
                {
                    for (int tHor = 0; tHor < ConfigLoader.Constant.LayerColumn; tHor++)
                    {
                        GameObject unit = GameModule.Resource.LoadGameObject("OneEditUnit",parent);
                        EditUnit editUnit = unit.GetComponent<EditUnit>();
                        editUnit.IsTaken = false;
                        _allUnits.Add(editUnit);
                        Vector2 tPos = new Vector2(startPos.x + tHor * EditorScene.Instance.UnitWidth,
                            startPos.y - tVer * EditorScene.Instance.UnitHeight);
                        unit.GetComponent<RectTransform>().anchoredPosition = tPos;

                        if (tVer > 0 && tHor > 0)
                        {
                            tPos.x -= EditorScene.Instance.UnitWidth * 0.5f;
                            tPos.y += EditorScene.Instance.UnitHeight * 0.5f;
                            _allUnitPos.Add(tPos);
                        }
                    }
                }
                _currentLayerIndex = 0;
                CardTypeCount = CurMapConfig.CardWeights.Count > 0
                    ? CurMapConfig.CardWeights.Count
                    : EditorScene.Instance.MaxCardType;
                GameEvent.Send(EventType.UpdateCardTypeCount,CardTypeCount);
                LoadLayerData();
                LoadStackData();
                SetGroupId();
            }
            else
            {
                _allUnits?.Clear();
                _allUnitPos?.Clear();
                mStacks.Clear();
                EditorScene.Instance.AllUnits.DestroyAllChildren();
            }
           
        }
        //加载所有层的卡牌
        private void LoadLayerData()
        {
            while (CurMapConfig.Layers.Count-1<_currentLayerIndex)
            {
                LayerData layerData = new LayerData();
                layerData.Coords = new List<CoordData>();
                CurMapConfig.Layers.Add(layerData);
            }
            //刷新层数显示ui
            GameEvent.Send(EventType.UpdateLayerUI, CurMapConfig.Layers.Count,_currentLayerIndex);
            _currentLayer = CurMapConfig.Layers[_currentLayerIndex];
            SetAllUnitTaken(false);
            _currentEditUnits.Clear();
            for (int i = 0; i < _currentLayer.Coords.Count; i++)
            {
                AddCard(_currentLayer.Coords[i],true);
            }
            RefreshCount();
        }
        //加载所有堆叠卡牌
        private void LoadStackData()
        {
            StackData tStackData;
            for (int i = 0; i < CurMapConfig.Stacks.Count; i++)
            {
                tStackData = CurMapConfig.Stacks[i];
                EditStack tstack = CreateStack(tStackData.Direction, tStackData.Count,MyStackParent,tStackData.CustomTypes,tStackData.GoldIndexs);
                tstack.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(tStackData.Coord.X, tStackData.Coord.Y);
            }
            RefreshCount();
        }

        private void SetGroupId()
        {
            if (CurMapConfig.TotalCardCount > 0)
            {
                int maxGroupId = Int32.MinValue;
                foreach (var layer in CurMapConfig.Layers)
                {
                    foreach (var coord in layer.Coords)
                    {
                        if (coord.CursSpecial != null)
                        {
                            if (coord.CursSpecial.CombinedId > maxGroupId)
                            {
                                maxGroupId = coord.CursSpecial.CombinedId;
                            }
                        }
                    }
                }

                _currentGroupId = maxGroupId;
            }
            else
            {
                _currentGroupId = 0;
            }
        }
        #region 网格卡牌相关的方法
        /// <summary>
        /// 点击编辑器中的某个单元格创建卡牌
        /// </summary>
        /// <param name="pos">屏幕坐标</param>
        /// <param name="isCreate">是否创建</param>
        public void OnClickedEditUnit(Vector2 pos,bool isCreate)
        {
            if(_isDraggingStack)return;
            Vector2 clickPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(EditorScene.Instance.EditBoard, pos,
                EditorScene.Instance.EditorCamera, out clickPos);
            CoordData chosenCoord = GetChosenCoord(clickPos,isCreate);
            if (chosenCoord!= null &&chosenCoord.X >= 0 && chosenCoord.Y >= 0)
            {
                if (isCreate)
                {
                    AddCard(chosenCoord);
                }
                else
                {
                    RemoveCard(chosenCoord);
                }
            }
        }
        //得到当前点击的格子
        private CoordData GetChosenCoord(Vector2 pChosenPos,bool isCreate)
        {
            CoordData chosenCoord=null;
            Vector2 oneUnitPos;
            float minDis = float.MaxValue;
            float curDis;
            int curIndex = -1;
            for (int i = 0; i < _allUnitPos.Count; i++)
            {
                oneUnitPos = _allUnitPos[i];
                if (Mathf.Abs(pChosenPos.x - oneUnitPos.x) >= EditorScene.Instance.UnitWidth ||
                    Mathf.Abs(pChosenPos.y - oneUnitPos.y) >= EditorScene.Instance.UnitHeight)
                {
                    continue;
                }

                curDis = Vector2.Distance(pChosenPos, oneUnitPos);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    curIndex = i;
                }
            }
            if (isCreate)
            {
                chosenCoord = new CoordData(-1,-1,ShellOrReversibleCount>0?ShellOrReversibleCount:1,new SpecialData(CurrentLockCardType, CurrentFunctionCardTypes,bombCount:BombCount));
                if (curIndex >= 0)
                {
                    chosenCoord.X = curIndex % (EditorScene.Instance.LayerColumn - 1);
                    chosenCoord.Y = curIndex / (EditorScene.Instance.LayerRow - 1);
                }
                return chosenCoord;
            }
            else
            {
                int tempX = curIndex % (EditorScene.Instance.LayerColumn - 1);
                int tempY = curIndex / (EditorScene.Instance.LayerRow - 1);
                foreach (var item in _currentLayer.Coords)
                {
                    if (item.X == tempX && item.Y == tempY)
                    {
                        chosenCoord = item;
                        break;
                    }
                }
                return chosenCoord;
            }
            
        }
        //添加卡牌，
        private void AddCard(CoordData chosenCoord,bool IsReadJson =false)
        {
            bool isAllEmpty = true;
            var rowCoordDatas = GetRowCoordData(chosenCoord,IsReadJson);
            foreach (var item in rowCoordDatas)
            {
                var coverIndexes = GetCoverIndexes(item);
                //检测是否有空格子
                for (int i = 0; i < coverIndexes.Count; i++)
                {
                    if (_allUnits[coverIndexes[i]].IsTaken)
                    {
                        isAllEmpty = false;
                        break;
                    }
                }
            }
            //isAllEmpty = rowCoordDatas.Count > 0;
            if (isAllEmpty)
            {
                foreach (var item in rowCoordDatas)
                {
                    var coverIndexes = GetCoverIndexes(item);
                    for (int i = 0; i < coverIndexes.Count; i++)
                    {
                        _allUnits[coverIndexes[i]].IsTaken = true;
                        _currentEditUnits.Add(_allUnits[coverIndexes[i]]);
                        //最后一个Unit
                        if (i == coverIndexes.Count - 1)
                        {
                            _allUnits[coverIndexes[i]].SetInfo(item.CursSpecial._lockCardType,item.CursSpecial.FunctionCardTypeList,item.CardTypes);
                        }
                    }
                    if (!_currentLayer.Coords.Contains(item))
                    {
                        _currentLayer.Coords.Add(item);
                        RefreshCount();
                    }
                }
            }
        }
        //得到一行的坐标数据(从左往右)
        private List<CoordData> GetRowCoordData(CoordData coordData,bool IsReadJson =false)
        {
            List<CoordData> rowCoordDatas = new List<CoordData>();
            rowCoordDatas.Add(coordData);
            if (IsReadJson)
            {
                return rowCoordDatas;
            }
            //当此时的特殊卡牌数据是拉链或者藤蔓的时候需要获取一行的坐标数据
            if (coordData.CursSpecial.FunctionCardTypeList!=null&&coordData.CursSpecial.FunctionCardTypeList.Contains(FunctionCardType.Zipper))
            {
                if (coordData.X > 10)
                {
                    rowCoordDatas.Clear();
                    return rowCoordDatas;
                }
                _currentGroupId++;
                coordData.CursSpecial.CombinedId = _currentGroupId;
                CoordData tempCoord = new CoordData(coordData.X + 2, coordData.Y, 1,
                    new SpecialData(LockCardType.None, new List<FunctionCardType>() { FunctionCardType.Zipper }, _currentGroupId));
                rowCoordDatas.Add(tempCoord);
            }else if (coordData.CursSpecial._lockCardType == LockCardType.Vines)
            {
                //藤蔓需要在获取到两个坐标数据后再添加
                if (coordData.X > 8)
                {
                    rowCoordDatas.Clear();
                    return rowCoordDatas;
                }
                _currentGroupId++;
                coordData.CursSpecial.CombinedId = _currentGroupId;
                for (int i = 1; i <= 2; i++)
                {
                    CoordData tempCoord = new CoordData(coordData.X + i * 2, coordData.Y, 1, new SpecialData(LockCardType.Vines,null,_currentGroupId));
                    rowCoordDatas.Add(tempCoord);
                }
            }
            //设置根节点判断
            if (rowCoordDatas.Count > 1)
            {
                coordData.CursSpecial.IsRoot = true;
            }

            if (CustomTypes != null)
            {
                for (int i = 0; i < rowCoordDatas.Count; i++)
                {
                    List<int> customTypes = new List<int>();
                    if (coordData.CursSpecial.FunctionCardTypeList != null &&
                        coordData.CursSpecial.FunctionCardTypeList.Contains(FunctionCardType.Shell))
                    {
                        customTypes.AddRange(CustomTypes);
                    }
                    else if (coordData.CursSpecial.FunctionCardTypeList != null &&
                             coordData.CursSpecial.FunctionCardTypeList.Contains(FunctionCardType.Reversible))
                    {
                        customTypes.AddRange(CustomTypes);
                    }
                    else
                    {
                        customTypes.Add(CustomTypes[i]);
                    }
                    rowCoordDatas[i].SetCoordType(customTypes);
                }
            }
            
            return rowCoordDatas;
        }
        private void RemoveCard(CoordData chosenCoord)
        {
            if(!_currentLayer.Coords.Contains(chosenCoord)) return;
            var gourpCoords = GetGrouprCoords(chosenCoord);
            foreach (var item in gourpCoords)
            {
                var coverIndexes = GetCoverIndexes(item);
                foreach (var index in coverIndexes)
                {
                    _allUnits[index].IsTaken = false;
                    _currentEditUnits.Remove(_allUnits[index]);
                }

                _currentLayer.Coords.Remove(item);
            }
            //_currentGroupId--;
            RefreshCount();
        }

        public List<CoordData> GetGrouprCoords(CoordData coordData)
        {
            List<CoordData> groupCoords = new List<CoordData>();
            groupCoords.Add(coordData);
            foreach (var item in _currentLayer.Coords)
            {
                if (item.CursSpecial.CombinedId>0&&item.CursSpecial.CombinedId == coordData.CursSpecial.CombinedId)
                {
                    groupCoords.AddWithoutRepetition(item);
                }
            }
            return groupCoords;
        }
        public void AddAllCard()
        {
            SetAllUnitTaken(true);
            _currentLayer.Coords.Clear();
            _currentEditUnits.Clear();
            for (int tVer = 0; tVer < EditorScene.Instance.LayerColumn - 1; tVer+=2)
            {
                for (int tHor = 0; tHor < EditorScene.Instance.LayerRow - 1; tHor += 2)
                {
                    var coord = new CoordData(tHor,tVer,1,new SpecialData(LockCardType.None,new List<FunctionCardType>()));
                    _currentLayer.Coords.Add(coord);
                }
            }
            _currentEditUnits.AddRange(_allUnits);
            RefreshCount();
        }

        public void RemoveAllCard()
        {
            SetAllUnitTaken(false);
            _currentLayer.Coords.Clear();
            RefreshCount();
            _currentEditUnits.Clear();
        }
        private List<int> GetCoverIndexes(CoordData pCoord)
        {
            List<int> coverIndexes = new List<int>();
            coverIndexes.Add(pCoord.X+pCoord.Y*EditorScene.Instance.LayerRow);
            coverIndexes.Add(pCoord.X+1+pCoord.Y*EditorScene.Instance.LayerRow);
            coverIndexes.Add(pCoord.X+(pCoord.Y+1)*EditorScene.Instance.LayerRow);
            coverIndexes.Add(pCoord.X+1+(pCoord.Y+1)*EditorScene.Instance.LayerRow);
            return coverIndexes;
        }

        private void SetAllUnitTaken(bool pTaken)
        {
            for (int i = 0; i < _allUnits.Count; i++)
            {
                _allUnits[i].IsTaken = pTaken;
            }
        }
        //刷新卡牌数量
        private void RefreshCount()
        {
            //_currentLayer.Count = _currentLayer.Coords.Count;
            int tempCount = 0;
            foreach (var item in _currentLayer.Coords)
            {
                tempCount+=item.Count;
            }
            _currentLayer.Count = tempCount;
            CurMapConfig.Layers[_currentLayerIndex] = _currentLayer;
            int totalCount = 0;
            for (int i = 0; i < CurMapConfig.Layers.Count; i++)
            {
                totalCount+=CurMapConfig.Layers[i].Count;
            }

            for (int i = 0; i < mStacks.Count; i++)
            {
                totalCount += mStacks[i].Count;
            }

            CurMapConfig.TotalCardCount = totalCount;
            GameEvent.Send(EventType.UpdateCardUI,totalCount,_currentLayer.Count);
        }
        /// <summary>
        /// 上一层
        /// </summary>
        public void LastLayer()
        {
            _currentLayerIndex--;
            _currentLayerIndex = Mathf.Max(0, _currentLayerIndex);
            LoadLayerData();
            ShowLastLayer();
        }
        /// <summary>
        /// 下一层
        /// </summary>
        public void NextLayer()
        {
            _currentLayerIndex++;
            LoadLayerData();//此时会把所有格子设置为未选中
            ShowLastLayer();
        }
        //展示上一层
        private void ShowLastLayer()
        {
            _lastEditUnits.Clear();
            int lastLayerIndex = _currentLayerIndex - 1;
            if (lastLayerIndex < 0)
            {
                return;
            }
            var lastLayer =  CurMapConfig.Layers[lastLayerIndex].Coords;
            foreach (var item in lastLayer)
            {
                AddShadowCard(item);
            }
        }

        private void AddShadowCard(CoordData coordData)
        {
            var coverIndexes = GetCoverIndexes(coordData);
            for (int i = 0; i < coverIndexes.Count; i++)
            {
                _allUnits[coverIndexes[i]].IsShadow = true;
                _lastEditUnits.Add( _allUnits[coverIndexes[i]]);
            }
        }

        public bool IsContainLastUnits(EditUnit unit)
        {
            return _lastEditUnits.Contains(unit);
        }
        public bool IsContainCurrentUnits(EditUnit unit)
        {
            return _currentEditUnits.Contains(unit);
        }

        
        #endregion

        #region 堆叠卡牌相关的方法

        public void AddStack(int dir,Transform parent = null)
        {
            if (CustomTypes == null || CustomTypes.Count == 0)
            {
                EditStack stack = CreateStack(dir, 3,parent);
                stack.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -600);
                RefreshCount();
            }else if (CustomTypes != null && CustomTypes.Count > 0)
            {
                EditStack stack = CreateStack(dir, CustomTypes.Count,parent,CustomTypes);
                stack.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -600);
                RefreshCount();
            }
        }
        private EditStack CreateStack(int dir, int pCount,Transform parent = null,List<int> customTypes =null,List<int> goldIndexs = null)
        {
            GameObject stackObj = GameModule.Resource.LoadGameObject("OneEditStack", parent==null?EditorScene.Instance.EditBoard:parent);
            EditStack stack = stackObj.GetComponent<EditStack>();
            stack.DelChangeCount = RefreshCount;
            stack.DeletStack = RemoveStack;
            stack.DelDragging = IsDragStack;
            stack.SetDirection(dir);
            string newInfo = pCount.ToString();
            if (goldIndexs != null && goldIndexs.Count > 0)
            {
                string goldStr = string.Join(",", goldIndexs);
                newInfo += $";{goldStr}";
            }
            stack.SetCount(newInfo);
            stack.SetAllTypes(customTypes);
            stack.InitState();
            mStacks.Add(stack);
            return stack;
        }

        private void RemoveStack(EditStack stack)
        {
            stack.DestroySafe(stack.gameObject,0);
            mStacks.Remove(stack);
            //更新堆叠卡牌数量
            RefreshCount();
        }

        private void IsDragStack(bool isDrag)
        {
            _isDraggingStack = isDrag;
        }
        #endregion
        public bool FindLevelInFolder(string levelName)
        {
            string folderPath = Application.dataPath + "/AssetRaw/LevelInfo/"+levelName+".json";
            if (File.Exists(folderPath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Save()
        {
            //设置权重
            CurMapConfig.CardWeights = new List<WeightData>();
            for (int i = 0; i < CardTypeCount; i++)
            {
                CurMapConfig.CardWeights.Add(new WeightData(i+1,1));
            }

            for (int i = 0; i < CurMapConfig.Layers.Count; i++)
            {
                CurMapConfig.Layers[i].Coords.Sort(SordCoord);
            }
            CurMapConfig.Stacks.Clear();
            //ReSetGroupId();
            for (int i = 0; i < mStacks.Count; i++)
            {
                StackData stackData = new StackData();
                stackData.Direction = mStacks[i].Direction;
                stackData.Count = mStacks[i].Count;
                stackData.Coord = new CoordData(mStacks[i].Pos.x, mStacks[i].Pos.y);
                stackData.CustomTypes = mStacks[i].AllTypes;
                if (mStacks[i].GoldIndexs != null && mStacks[i].GoldIndexs.Count > 0)
                {
                    stackData.GoldIndexs = mStacks[i].GoldIndexs;
                }
                CurMapConfig.Stacks.Add(stackData);
            }
            string folderPath = Application.dataPath+"/AssetRaw/LevelInfo/"+CurMapConfig.LevelName+".json";
            var jsonStr = Utility.Json.ToJson(CurMapConfig);
            var result=Utility.File.SaveJsonFile(folderPath, jsonStr);
            if (result)
            {
                
            }
        }

        private void ReSetGroupId()
        {
            List<CoordData> temp = new List<CoordData>();
            List<CoordData> root = new List<CoordData>();
            foreach (var layer in CurMapConfig.Layers)
            {
                foreach (var coord in layer.Coords)
                {
                    if (coord.CursSpecial != null)
                    {
                        if (coord.CursSpecial.FunctionCardTypeList.Contains(FunctionCardType.Zipper))
                        {
                           
                            if (coord.CursSpecial.IsRoot)
                            {
                                root.AddWithoutRepetition(coord);
                            }
                            else
                            {
                                temp.AddWithoutRepetition(coord);
                            }
                        }
                    }
                }
            }

            int groupCount = root.Count;
            if(groupCount==0) return;
            for (int i = 0; i < groupCount; i++)
            {
                CoordData rootCoord = root[i];
                CoordData otherCoord = temp.Find(x => x.X == rootCoord.X + 2 && x.Y == rootCoord.Y);
                rootCoord.CursSpecial.CombinedId = i + 1;
                otherCoord.CursSpecial.CombinedId=i+1;
            }
            
        }
        private int SordCoord(CoordData coordDataA, CoordData coordDataB)
        {
            if (coordDataA.Y < coordDataB.Y)
            {
                return -1;
            }else if (coordDataA.Y == coordDataB.Y)
            {
                if (coordDataA.X < coordDataB.X)
                {
                    return -1;
                }

                return 1;
            }

            return 1;
        }
    }
    #region 配置关卡所需的数据结构
    //关卡配置数据结构
    public class MapConfig
    {
        public string LevelName;
        //当前关卡卡片总数量
        public int TotalCardCount;
        public List<WeightData> CardWeights;
        public List<LayerData> Layers;
        public List<StackData> Stacks;

        public MapConfig(string levelName,int totalCardCount, List<WeightData> cardWeights, List<LayerData> layers,
            List<StackData> stacks)
        {
            this.LevelName = levelName;
            this.TotalCardCount = totalCardCount;
            this.CardWeights = cardWeights;
            this.Layers = layers;
            this.Stacks = stacks;
        }
    }
    //卡牌权重数据结构
    [Serializable]
    public struct WeightData
    {
        public int CardType;
        public int Weight;

        public WeightData(int cardType, int weight)
        {
            CardType = cardType;
            Weight = weight;
        }
    }
    [Serializable]
    public class CoordData
    {
        public int X;
        public int Y;
        //卡牌数量
        public int Count;
        //卡牌类型
        public List<int> CardTypes;
        //卡牌特殊类型
        public SpecialData CursSpecial;
        public int LayerIndex;
        public CoordData(int x, int y,int count=1,SpecialData cursSpecial = null)
        {
            X = x;
            Y = y;
            Count = count;
            CursSpecial = cursSpecial;
        }
        //自定义卡牌类型
        public void SetCoordType(List<int> customs)
        {
            if (customs != null && customs.Count > 0)
            {
                List<int> temp = new List<int>();
                for (int i = 0; i < Count; i++)
                {
                    temp.Add(customs[i]);
                }
                CardTypes = temp;
            }
        }
    }
    //卡牌层数据结构
    [Serializable]
    public struct LayerData
    {
        public int Count;
        public List<CoordData> Coords;
    }
    //卡牌堆数据结构
    [Serializable]
    public struct StackData
    {
        public int Count;
        public CoordData Coord;
        //堆叠方向
        public int Direction;
        public List<int> CustomTypes;
        public List<int> GoldIndexs;
    }
    //TODO: 卡牌特殊类型拆分：无特殊效果、被锁住、能直接点击且有交互的、拉链、生成器卡牌（3的倍数）
    //被锁住的：一条锁链、两条锁链、冰块、蜂蜜、藤蔓、蝴蝶、石头、草
    //能直接点击且有交互的：金块、双面牌、炸弹
    //拉链
    //生成器卡牌（3的倍数）
    [Serializable]
    public class SpecialData 
    {
        //锁住卡牌类型
        public LockCardType _lockCardType = LockCardType.None;
        //卡牌交互功能类型
        public List<FunctionCardType> FunctionCardTypeList;
        //组合卡牌id
        public int CombinedId;
        //是否是分组卡牌根节点
        public bool IsRoot =false;
        // 炸弹的可点击数量
        public int BombCount;
        public SpecialData(LockCardType lockCardType,List<FunctionCardType> functionCardTypelist = null ,int combinedId = -1,int bombCount = 0,bool isRoot = false)
        {
            _lockCardType = lockCardType;
            FunctionCardTypeList = functionCardTypelist;
            CombinedId = combinedId;
            BombCount = bombCount;
            IsRoot = isRoot;
        }

        public bool IsOneFunctionCardType(FunctionCardType functionCardType)
        {
            if (FunctionCardTypeList == null || FunctionCardTypeList.Count == 0) return false;
            return FunctionCardTypeList.Contains(functionCardType);
        }
        public bool IsReject()
        {
            return _lockCardType == LockCardType.Honey ||
                   _lockCardType == LockCardType.Rock ||
                   _lockCardType == LockCardType.Grassy ||
                   _lockCardType == LockCardType.Door ||
                   IsFunctionCard(FunctionCardType.Reversible) ||
                   IsFunctionCard(FunctionCardType.Shell) ||
                   IsFunctionCard(FunctionCardType.GoldCube);
        }
        public bool IsFunctionCard(FunctionCardType fType)
        {
            if (FunctionCardTypeList == null) return false;
            if (FunctionCardTypeList.Count == 0) return false;
            bool isFunction = FunctionCardTypeList.Contains(fType);
            return isFunction;
        }
    }
    #endregion
}
