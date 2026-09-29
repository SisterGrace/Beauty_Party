using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 配置表下载管理器（只负责下载，不负责反序列化）
/// 使用 UniTask 异步下载，避免阻塞主线程
/// </summary>
public class ConfigDownloader : MonoBehaviour
{
    private static ConfigDownloader _instance;
    public static ConfigDownloader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ConfigDownloader");
                _instance = go.AddComponent<ConfigDownloader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 所有表名列表（与 Tables 构造函数中的 loader 调用完全一致）
    private static readonly string[] KnownTableNames = new string[]
    {
        //"config_tblanguagecfg",
        "config_tbcfglevelinfo",
        //"config_tbcfglevelchest",
        //"config_tbconstant",
        //"config_tbcfgnormalcardinfo",
        //"config_tbcfglockcardinfo",
        //"config_tbcfgfunctioncardinfo",
        //"config_tbcfggamepropinfo",
        "config_tbcfgreward",
        "config_tbcfgphotogrouprew",
        //"config_tbcfgguide",
        //"config_tbcfgshop",
        //"config_tbcfgrecharge",
        //"config_tbcfgactivity",
        //"config_tbcfgdreampass",
        //"config_tbcfgendlesstreasure",
        //"config_tbcfglevelprogress",
        "config_tbcfgphotos",
        "config_tbcfggacha",
        //"config_tbcfgdaycheck",
        //"config_tbcfggamevip"
    };

    private string LocalConfigPath => Path.Combine(Application.persistentDataPath, "Configs");
    private string _cachedBaseUrl = null;
    private bool _urlReady = false;

    // 单文件下载超时（秒），404 场景不需要等太久
    private const int DownloadTimeoutSeconds = 3;

    // 日志里单行最长字符数，避免 404 HTML 刷屏
    private const int MaxLogLineLength = 120;

    /// <summary>
    /// 异步初始化：从 StreamingAssets 读取并解密 CDN URL
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (_urlReady) return;
        await LoadBaseUrlAsync();
    }

    /// <summary>
    /// 下载所有配置表（全量下载）
    /// 返回 true 表示全部成功，false 表示有部分失败（但已下载的文件仍可用）
    /// </summary>
    public async UniTask<bool> DownloadAllAsync()
    {
        if (!_urlReady) await LoadBaseUrlAsync();

        // URL无效时直接跳过下载
        if (!IsUrlValid)
        {
            Debug.LogWarning("[ConfigDownloader] URL无效，跳过下载，使用本地配置");
            return false;
        }

        if (!Directory.Exists(LocalConfigPath))
            Directory.CreateDirectory(LocalConfigPath);

        // 并行下载所有文件，大幅减少总耗时
        var tasks = new UniTask<bool>[KnownTableNames.Length];
        for (int i = 0; i < KnownTableNames.Length; i++)
        {
            tasks[i] = DownloadSingleFileAsync(KnownTableNames[i]);
        }

        bool[] results = await UniTask.WhenAll(tasks);

        int successCount = 0;
        int failCount = 0;
        foreach (bool ok in results)
        {
            if (ok) successCount++; else failCount++;
        }

        Debug.Log($"[ConfigDownloader] 下载完成：成功 {successCount} 个，失败 {failCount} 个");
        return failCount == 0;
    }

    #region 内部方法

    /// <summary>
    /// URL是否有效（读取失败时为false，跳过网络下载）
    /// </summary>
    public bool IsUrlValid => _urlReady && !string.IsNullOrEmpty(_cachedBaseUrl);

    private async UniTask LoadBaseUrlAsync()
    {
        string encrypted = null;
        string url = null;

#if UNITY_EDITOR
        string path = Application.dataPath + "/StreamingAssets/Configs/url_config.txt";
        if (File.Exists(path))
        {
            encrypted = File.ReadAllText(path).Trim();
        }
#else
        string path = Path.Combine(Application.streamingAssetsPath, "Configs/url_config.txt");
        // Android 的 StreamingAssets 需要用 UnityWebRequest 异步读取
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            await req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                encrypted = req.downloadHandler.text.Trim();
            }
            else
            {
                Debug.LogError($"[ConfigDownloader] 读取 url_config.txt 失败: {req.error}");
            }
        }
#endif

        if (!string.IsNullOrEmpty(encrypted))
        {
            try
            {
                url = StringEncryptor.DecryptFromBase64(encrypted);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigDownloader] 解密 URL 失败: {e.Message}");
            }
        }

        // URL读取失败时不再兜底，标记为无效，后续跳过下载直接用本地配置
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[ConfigDownloader] URL配置读取失败，将跳过网络下载，使用本地配置");
            _cachedBaseUrl = null;
            _urlReady = true;
            return;
        }

        _cachedBaseUrl = url;
        _urlReady = true;
        Debug.Log($"[ConfigDownloader] CDN URL: {_cachedBaseUrl}");
    }

    private async UniTask<bool> DownloadSingleFileAsync(string fileName)
    {
        string remotePath = _cachedBaseUrl + fileName + ".bytes";
        string localPath = Path.Combine(LocalConfigPath, fileName + ".bytes");
        string tempPath = localPath + ".tmp";

        using (UnityWebRequest req = UnityWebRequest.Get(remotePath))
        {
            req.timeout = DownloadTimeoutSeconds;
            req.downloadHandler = new DownloadHandlerBuffer();

            try
            {
                await req.SendWebRequest();
            }
            catch (Exception e)
            {
                // 只取第一行，避免 404 HTML 整页刷屏
                Debug.LogWarning($"[ConfigDownloader] 下载 {fileName} 异常: {FirstLine(e.Message)}");
                return false;
            }

            // 失败时只打状态码，不打 req.error（可能整页 HTML）
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ConfigDownloader] 下载 {fileName} 失败: HTTP {req.responseCode}");
                return false;
            }

            try
            {
                // 原子写入：先写临时文件，再重命名，避免写入中断导致文件损坏
                File.WriteAllBytes(tempPath, req.downloadHandler.data);
                if (File.Exists(localPath)) File.Delete(localPath);
                File.Move(tempPath, localPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigDownloader] 写入 {fileName} 失败: {FirstLine(e.Message)}");
                // 清理临时文件
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                return false;
            }
        }
    }

    /// <summary>
    /// 截取字符串第一行，并限制最大长度
    /// </summary>
    private static string FirstLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        int idx = s.IndexOf('\n');
        if (idx >= 0) s = s.Substring(0, idx);
        int idx2 = s.IndexOf('\r');
        if (idx2 >= 0) s = s.Substring(0, idx2);
        if (s.Length > MaxLogLineLength) s = s.Substring(0, MaxLogLineLength);
        return s;
    }

    #endregion
}