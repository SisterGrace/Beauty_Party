using Bright.Serialization;
using Config;
using Config.config;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TK_Engine;
using UnityEngine;
using Y_Base;

public class ConfigLoader : Singleton<ConfigLoader>
{
    private bool _init = false;

    /// <summary>
    /// 是否优先使用线上下载的配置表
    /// 当下载流程执行过后设为true，LoadByteBuf会优先从persistentDataPath读取
    /// </summary>
    public bool UseOnlineTable { get; set; } = false;

    private Tables _tables;

    public Tables Tables
    {
        get
        {
            if (!_init)
            {
                _init = true;
                Load();
            }

            return _tables;
        }
    }

    public static TbConstant Constant => Instance.Tables.TbConstant;
    /// <summary>
    /// 加载配置
    /// </summary>
    public void Load()
    {
        var tablesCtor = typeof(Tables).GetConstructors()[0];
        var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];

        System.Delegate loader = loaderReturnType == typeof(ByteBuf)
            ? new System.Func<string, ByteBuf>(LoadByteBuf)
            : (System.Delegate)new System.Func<string, JSONNode>(LoadJson);
        _tables = (Tables)tablesCtor.Invoke(new object[] { loader });
        _init = true;
        InitPhotoList();
    }
    
    /// <summary>
    /// 加载Json配置。
    /// </summary>
    /// <param name="file">FileName</param>
    /// <returns>JSONNode</returns>
    private JSONNode LoadJson(string file)
    {
       
#if UNITY_EDITOR
        var ret = File.ReadAllText($"{Application.dataPath}/../GenerateDatas/json/{file}.json",
            System.Text.Encoding.UTF8);
#else
        var textAssets =
 GameModule.Resource.LoadAsset<TextAsset>($"{SettingsUtils.FrameworkGlobalSettings.ConfigFolderName}{file}.json");
        var ret = textAssets.text;
#endif
        return JSON.Parse(ret);
    }
    
    /// <summary>
    /// 加载二进制配置。
    /// 优先从 persistentDataPath 读取下载的线上文件，失败则回退包内
    /// </summary>
    /// <param name="file">FileName</param>
    /// <returns>ByteBuf</returns>
    private ByteBuf LoadByteBuf(string file)
    {
        byte[] ret = null;

        // 优先使用持久化目录中下载的文件
        if (UseOnlineTable)
        {
            string persistentPath = Path.Combine(Application.persistentDataPath, "Configs", file + ".bytes");
            if (File.Exists(persistentPath))
            {
                try
                {
                    ret = File.ReadAllBytes(persistentPath);
                    return new ByteBuf(ret);
                }
                catch (Exception e)
                {
                    Log.Warning($"读取线上配置 {file} 失败: {e.Message}，回退包内文件");
                }
            }
        }

        // 回退：包内或编辑器下读取
#if UNITY_EDITOR
        ret = File.ReadAllBytes($"{Application.dataPath}/AssetRaw/Configs/bytes/{file}.bytes");
#else
        var textAssets =
 GameModule.Resource.LoadAsset<TextAsset>($"{file}");
        ret = textAssets.bytes;
#endif
        return new ByteBuf(ret);
    }


    #region  获取照片配置

    public static Dictionary<MyPhotoType, List<CfgPhotos>> PhotoListDic;
    private static void InitPhotoList()
    {
        PhotoListDic = new Dictionary<MyPhotoType, List<CfgPhotos>>();
        foreach (var item in Instance.Tables.TbCfgPhotos.DataList)
        {
            MyPhotoType type;
            if (Enum.IsDefined(typeof(MyPhotoType), item.ContentRating))
            {
                type = (MyPhotoType)item.ContentRating;
            }
            else
            {
                Log.Warning($"Undefined MyPhotoType for ContentRating: {item.ContentRating}");
                continue;
            }

            if (!PhotoListDic.TryGetValue(type, out List<CfgPhotos> list))
            {
                list = new List<CfgPhotos>();
                PhotoListDic.Add(type, list);
            }
            list.Add(item);
        }
    }
    /// <summary>
    /// 获取照片列表，获取参与随机的
    /// </summary>
    /// <param name="type">内容等级，1：低，2：中，3：高</param>
    /// <param name="Abtype">AB类型，0-A，1-B</param>
    /// <returns></returns>
    public static List<CfgPhotos> GetOnekindPhotoList(MyPhotoType type, int Abtype)
    {
        var tempList = new List<CfgPhotos>();
        if (PhotoListDic.TryGetValue(type, out var list))
        {
            foreach (var item in list)
            {
                if (item.ABGroup == Abtype&&item.IsRandomEnabled)
                {
                    tempList.Add(item);
                }
            }
        }
        return tempList;
    }
    /// <summary>
    /// 获取照片列表，获取参与随机的
    /// </summary>
    /// <param name="Abtype">AB类型，0-A，1-B</param>
    /// <returns></returns>
    public static List<CfgPhotos> GetRandomPhotoList(int Abtype)
    {
        var tempList = new List<CfgPhotos>();
        foreach (var item in Instance.Tables.TbCfgPhotos.DataList)
        {
            if (item.IsRandomEnabled && item.ABGroup == Abtype)
            {
                tempList.Add(item);
            }
        }
        return tempList;
    }
    /// <summary>
    /// 获取上线区域。
    /// </summary>
    public string GetPhotoRegion()
    {
        if (!Constant.PhotoDetection)
        {
            return "EU";
        }
        else
        {
            string sysLanguage = GameModule.Localization.SystemLanguage.ToString();
            if (Constant.JK.Contains(sysLanguage))
            {
                return "JK";
            }
            else
            {
                return "EU";
            }
        }
        
        //后续根据系统语言进行区域判定
        //当检测到手机系统语言简码为以下时,为日韩线(2),其他语言为欧美线(1)
    }
    
    public enum MyPhotoType
    {
        p1=1,
        p2=2,
        p3=3,
        p4=4,//金币卡包
        p5=5,
        p6=6,//付费卡包
        p7=7,//无尽宝藏
        p8=8,//礼品包
        p9=9,//黄金狂潮
        p10=10,//梦想通行证
        p11=11,//新手礼包
        p12=12,//商城礼包
    }
    #endregion
}
