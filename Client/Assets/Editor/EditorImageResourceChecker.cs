using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 基于现有数据表的图片资源检查工具
/// </summary>
public static class TableImageResourceChecker
{
    #region 配置字段
    
    private static string _baseDomain = "https://your-domain.com/images/";
    
    #endregion
    
    #region 控制字段
    
    // 用于控制检查过程的取消令牌
    private static CancellationTokenSource _currentCheckCancellationTokenSource;
    private static bool _isChecking = false;
    private static string _currentOperationName = "";
    
    #endregion
    
    #region 属性配置
    
    /// <summary>
    /// 基础域名
    /// </summary>
    public static string BaseDomain
    {
        get => _baseDomain;
        set => _baseDomain = value;
    }
    
    /// <summary>
    /// 是否正在检查
    /// </summary>
    public static bool IsChecking => _isChecking;
    
    /// <summary>
    /// 当前操作名称
    /// </summary>
    public static string CurrentOperationName => _currentOperationName;

    #endregion

    #region 编辑器菜单项

#if UNITY_EDITOR
    [MenuItem("Tools/图片资源检查/检查视频文件")]
    private static void CheckVideoFilesMenu()
    {
        if (_isChecking)
        {
            bool stop = EditorUtility.DisplayDialog("操作确认",
                $"当前正在执行: {_currentOperationName}\n是否停止当前检查并开始视频检查？",
                "停止并开始", "取消");
            if (stop)
            {
                ForceStopCheck();
                CheckVideoFiles().Forget();
            }
        }
        else
        {
            CheckVideoFiles().Forget();
        }
    }
    [MenuItem("Tools/图片资源检查/检查表格中所有图片")]
    private static void CheckAllTableImagesMenu()
    {
        if (_isChecking)
        {
            bool stop = EditorUtility.DisplayDialog("操作确认", 
                $"当前正在执行: {_currentOperationName}\n是否停止当前检查并开始新的检查？", 
                "停止并开始", "取消");
            
            if (stop)
            {
                ForceStopCheck();
                CheckAllTableImages().Forget();
            }
        }
        else
        {
            CheckAllTableImages().Forget();
        }
    }
    
    [MenuItem("Tools/图片资源检查/检查表格中缺失的图片")]
    private static void CheckMissingTableImagesMenu()
    {
        if (_isChecking)
        {
            bool stop = EditorUtility.DisplayDialog("操作确认", 
                $"当前正在执行: {_currentOperationName}\n是否停止当前检查并开始新的检查？", 
                "停止并开始", "取消");
            
            if (stop)
            {
                ForceStopCheck();
                CheckMissingTableImages().Forget();
            }
        }
        else
        {
            CheckMissingTableImages().Forget();
        }
    }
    
    [MenuItem("Tools/图片资源检查/导出表格图片报告")]
    private static void ExportTableReportMenu()
    {
        if (_isChecking)
        {
            bool stop = EditorUtility.DisplayDialog("操作确认", 
                $"当前正在执行: {_currentOperationName}\n是否停止当前检查并开始导出报告？", 
                "停止并开始", "取消");
            
            if (stop)
            {
                ForceStopCheck();
                ExportTableImageReport().Forget();
            }
        }
        else
        {
            ExportTableImageReport().Forget();
        }
    }
    
    [MenuItem("Tools/图片资源检查/验证表格图片配置")]
    private static void ValidateTableConfigMenu()
    {
        if (_isChecking)
        {
            bool stop = EditorUtility.DisplayDialog("操作确认", 
                $"当前正在执行: {_currentOperationName}\n是否停止当前检查并开始验证配置？", 
                "停止并开始", "取消");
            
            if (stop)
            {
                ForceStopCheck();
                ValidateTableConfig().Forget();
            }
        }
        else
        {
            ValidateTableConfig().Forget();
        }
    }
    
    [MenuItem("Tools/图片资源检查/强制停止检查")]
    private static void ForceStopCheckMenu()
    {
        if (_isChecking)
        {
            bool confirm = EditorUtility.DisplayDialog("强制停止", 
                $"确认要强制停止当前正在执行的检查吗？\n当前操作: {_currentOperationName}", 
                "强制停止", "取消");
            
            if (confirm)
            {
                ForceStopCheck();
            }
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "当前没有正在执行的检查任务", "确定");
        }
    }
    
    // 更新菜单项状态
    [MenuItem("Tools/图片资源检查/强制停止检查", true)]
    private static bool ValidateForceStopCheckMenu()
    {
        return _isChecking;
    }
#endif
    
    #endregion
    
    #region 强制停止方法
    
    /// <summary>
    /// 强制停止当前检查
    /// </summary>
    public static void ForceStopCheck()
    {
        if (_isChecking && _currentCheckCancellationTokenSource != null)
        {
            Debug.Log($"正在强制停止检查: {_currentOperationName}");
            
            // 取消所有正在进行的任务
            _currentCheckCancellationTokenSource.Cancel();
            _currentCheckCancellationTokenSource.Dispose();
            _currentCheckCancellationTokenSource = null;
            
            // 更新状态
            _isChecking = false;
            _currentOperationName = "";
            
            // 清除进度条
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
            
            Debug.Log("检查已强制停止");
        }
    }
    
    /// <summary>
    /// 开始新的检查，设置取消令牌
    /// </summary>
    private static void StartNewCheck(string operationName)
    {
        // 如果已有检查在运行，先停止
        if (_isChecking)
        {
            ForceStopCheck();
        }
        
        // 创建新的取消令牌源
        _currentCheckCancellationTokenSource = new CancellationTokenSource();
        _isChecking = true;
        _currentOperationName = operationName;
        
        Debug.Log($"开始新的检查: {operationName}");
    }
    
    /// <summary>
    /// 完成检查
    /// </summary>
    private static void CompleteCheck()
    {
        _isChecking = false;
        _currentOperationName = "";
        
        if (_currentCheckCancellationTokenSource != null)
        {
            _currentCheckCancellationTokenSource.Dispose();
            _currentCheckCancellationTokenSource = null;
        }
        
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
        
        Debug.Log("检查完成");
    }
    
    #endregion
    
    #region 核心检查方法
    
    /// <summary>
    /// 检查表格中的所有图片资源
    /// </summary>
    public static async UniTaskVoid CheckAllTableImages()
    {
#if UNITY_EDITOR
        try
        {
            // 开始新的检查
            StartNewCheck("检查表格中所有图片");
            var cancellationToken = _currentCheckCancellationTokenSource.Token;
            
            // 获取表格数据
            var photosTable = ConfigLoader.Instance.Tables.TbCfgPhotos.DataList;
            if (photosTable == null || photosTable.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "TbCfgPhotos 表格为空或未加载", "确定");
                CompleteCheck();
                return;
            }
            
            // 准备检查数据
            List<PhotoItem> photoItems = new List<PhotoItem>();
            foreach (var photo in photosTable)
            {
                // 检查是否被取消
                cancellationToken.ThrowIfCancellationRequested();
                
                string imageId = photo.ImageID?.ToString() ?? "";
                string imageType = photo.ImageType?.ToString() ?? "";
                
                if (!string.IsNullOrEmpty(imageId) && !string.IsNullOrEmpty(imageType))
                {
                    photoItems.Add(new PhotoItem
                    {
                        ID = photo.ID,
                        ImageId = imageId,
                        ImageType = imageType,
                        RowId = GetRowId(photo) // 获取行标识
                    });
                }
            }
            
            if (photoItems.Count == 0)
            {
                EditorUtility.DisplayDialog("警告", "表格中没有有效的图片数据", "确定");
                CompleteCheck();
                return;
            }
            
            Debug.Log($"开始检查表格中的 {photoItems.Count} 个图片");
            
            // 执行检查
            await CheckPhotoItemsAsync(photoItems, "表格图片检查", cancellationToken);
            
            CompleteCheck();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("检查被用户取消");
            CompleteCheck();
        }
        catch (Exception e)
        {
            Debug.LogError($"检查表格图片失败: {e.Message}");
            CompleteCheck();
            
#if UNITY_EDITOR
            EditorUtility.DisplayDialog("错误", $"检查失败: {e.Message}", "确定");
#endif
        }
#endif
    }
    
    /// <summary>
    /// 检查表格中已标记为缺失的图片
    /// </summary>
    public static async UniTaskVoid CheckMissingTableImages()
    {
#if UNITY_EDITOR
        try
        {
            // 开始新的检查
            StartNewCheck("检查表格中缺失的图片");
            var cancellationToken = _currentCheckCancellationTokenSource.Token;
            
            // 检查是否被取消
            cancellationToken.ThrowIfCancellationRequested();
            
            // 读取之前记录的缺失图片
            string logPath = GetMissingImagesLogPath();
            if (!File.Exists(logPath))
            {
                EditorUtility.DisplayDialog("提示", "没有缺失图片记录", "确定");
                CompleteCheck();
                return;
            }
            
            string[] missingImageIds = await UniTask.RunOnThreadPool(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();
                return File.ReadAllLines(logPath);
            });
            
            if (missingImageIds.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有缺失图片记录", "确定");
                CompleteCheck();
                return;
            }
            
            Debug.Log($"找到 {missingImageIds.Length} 个缺失图片记录");
            
            // 检查是否被取消
            cancellationToken.ThrowIfCancellationRequested();
            
            // 从表格中查找对应的图片
            List<PhotoItem> missingPhotoItems = new List<PhotoItem>();
            var photosTable = ConfigLoader.Instance.Tables.TbCfgPhotos.DataList;
            
            foreach (string imageId in missingImageIds)
            {
                // 检查是否被取消
                cancellationToken.ThrowIfCancellationRequested();
                
                // 这里需要根据实际表格结构查找对应的行
                // 假设 ImageId 是唯一的
                foreach (var photo in photosTable)
                {
                    if (photo.ImageID?.ToString() == imageId)
                    {
                        missingPhotoItems.Add(new PhotoItem
                        {
                            ID = photo.ID,
                            ImageId = photo.ImageID.ToString(),
                            ImageType = photo.ImageType?.ToString() ?? "",
                            RowId = GetRowId(photo)
                        });
                        break;
                    }
                }
            }
            
            if (missingPhotoItems.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "在表格中未找到对应的缺失图片", "确定");
                CompleteCheck();
                return;
            }
            
            Debug.Log($"重新检查 {missingPhotoItems.Count} 个缺失图片");
            await CheckPhotoItemsAsync(missingPhotoItems, "缺失图片重新检查", cancellationToken);
            
            CompleteCheck();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("检查被用户取消");
            CompleteCheck();
        }
        catch (Exception e)
        {
            Debug.LogError($"检查缺失图片失败: {e.Message}");
            CompleteCheck();
            
#if UNITY_EDITOR
            EditorUtility.DisplayDialog("错误", $"检查失败: {e.Message}", "确定");
#endif
        }
#endif
    }
    
    /// <summary>
    /// 验证表格配置的完整性
    /// </summary>
    public static async UniTaskVoid ValidateTableConfig()
    {
#if UNITY_EDITOR
        try
        {
            // 开始新的检查
            StartNewCheck("验证表格图片配置");
            var cancellationToken = _currentCheckCancellationTokenSource.Token;
            
            var photosTable = ConfigLoader.Instance.Tables.TbCfgPhotos.DataList;
            if (photosTable == null)
            {
                EditorUtility.DisplayDialog("错误", "TbCfgPhotos 表格未加载", "确定");
                CompleteCheck();
                return;
            }
            
            EditorUtility.DisplayProgressBar("验证表格配置", "分析表格数据...", 0);
            
            List<string> issues = new List<string>();
            int validCount = 0;
            int emptyImageIdCount = 0;
            int emptyImageTypeCount = 0;
            
            foreach (var photo in photosTable)
            {
                // 检查是否被取消
                cancellationToken.ThrowIfCancellationRequested();
                
                string imageId = photo.ImageID?.ToString() ?? "";
                string imageType = photo.ImageType?.ToString() ?? "";
                string rowId = GetRowId(photo);
                
                if (string.IsNullOrEmpty(imageId))
                {
                    emptyImageIdCount++;
                    issues.Add($"行 {rowId}: ImageId 为空");
                }
                
                if (string.IsNullOrEmpty(imageType))
                {
                    emptyImageTypeCount++;
                    issues.Add($"行 {rowId}: ImageType 为空");
                }
                
                if (!string.IsNullOrEmpty(imageId) && !string.IsNullOrEmpty(imageType))
                {
                    validCount++;
                }
            }
            
            EditorUtility.ClearProgressBar();
            
            // 显示验证结果
            StringBuilder result = new StringBuilder();
            result.AppendLine("表格配置验证结果:");
            result.AppendLine($"=================================");
            result.AppendLine($"总行数: {photosTable.Count}");
            result.AppendLine($"有效数据: {validCount}");
            result.AppendLine($"ImageId 为空: {emptyImageIdCount}");
            result.AppendLine($"ImageType 为空: {emptyImageTypeCount}");
            
            if (issues.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("发现的问题:");
                for (int i = 0; i < Mathf.Min(issues.Count, 20); i++)
                {
                    result.AppendLine($"  {i + 1}. {issues[i]}");
                }
                
                if (issues.Count > 20)
                {
                    result.AppendLine($"  ... 还有 {issues.Count - 20} 个问题");
                }
                
                // 保存问题列表到文件
                string issuesPath = Path.Combine(Application.dataPath, "../TableValidationIssues.txt");
                await UniTask.RunOnThreadPool(() => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.WriteAllLines(issuesPath, issues);
                });
                
                result.AppendLine();
                result.AppendLine($"详细问题列表已保存到: {issuesPath}");
            }
            
            Debug.Log(result.ToString());
            EditorUtility.DisplayDialog("验证结果", result.ToString(), "确定");
            
            CompleteCheck();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("验证被用户取消");
            CompleteCheck();
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"验证表格配置失败: {e.Message}");
            CompleteCheck();
            
            EditorUtility.DisplayDialog("错误", $"验证失败: {e.Message}", "确定");
        }
#endif
    }
    
    #endregion
    
    #region 批量检查实现
    
    /// <summary>
    /// 批量检查图片项
    /// </summary>
    private static async UniTask CheckPhotoItemsAsync(
        List<PhotoItem> photoItems, 
        string operationName, 
        CancellationToken cancellationToken = default)
    {
#if UNITY_EDITOR
        EditorUtility.DisplayProgressBar(operationName, "初始化...", 0);
        
        try
        {
            int total = photoItems.Count;
            int completed = 0;
            int existsCount = 0;
            int missingCount = 0;
            List<string> missingImages = new List<string>();
            
            // 分批处理，避免同时发起过多请求
            int batchSize = 10;
            for (int i = 0; i < total; i += batchSize)
            {
                // 检查是否被取消
                cancellationToken.ThrowIfCancellationRequested();
                
                int endIndex = Mathf.Min(i + batchSize, total);
                var batchTasks = new List<UniTask<bool>>();
                
                for (int j = i; j < endIndex; j++)
                {
                    var item = photoItems[j];
                    batchTasks.Add(CheckPhotoItemAsync(item, cancellationToken));
                }
                
                bool[] batchResults = await UniTask.WhenAll(batchTasks);
                
                // 更新进度和结果
                for (int j = 0; j < batchResults.Length; j++)
                {
                    completed++;
                    
                    if (batchResults[j])
                    {
                        existsCount++;
                    }
                    else
                    {
                        missingCount++;
                        missingImages.Add(photoItems[i + j].ImageId);
                    }
                    
                    float progress = (float)completed / total;
                    EditorUtility.DisplayProgressBar(operationName, 
                        $"正在检查 {completed}/{total}", progress);
                }
                
                // 小延迟，避免对服务器造成太大压力
                await UniTask.Delay(100, cancellationToken: cancellationToken);
            }
            
            EditorUtility.ClearProgressBar();
            
            // 显示结果
            StringBuilder result = new StringBuilder();
            result.AppendLine($"{operationName} 完成:");
            result.AppendLine($"=================================");
            result.AppendLine($"检查总数: {total}");
            result.AppendLine($"存在: {existsCount}");
            result.AppendLine($"缺失: {missingCount}");
            
            if (missingCount > 0)
            {
                result.AppendLine();
                result.AppendLine("缺失的图片:");
                for (int i = 0; i < Mathf.Min(missingImages.Count, 15); i++)
                {
                    result.AppendLine($"  {i + 1}. {missingImages[i]}");
                }
                
                if (missingImages.Count > 15)
                {
                    result.AppendLine($"  ... 还有 {missingImages.Count - 15} 个");
                }
                
                // 保存缺失图片到日志
                await SaveMissingImagesAsync(missingImages, cancellationToken);
            }
            
            Debug.Log(result.ToString());
            EditorUtility.DisplayDialog("检查结果", result.ToString(), "确定");
            
            // 自动打开缺失图片日志
            if (missingCount > 0)
            {
                bool openLog = EditorUtility.DisplayDialog("缺失图片", 
                    $"发现 {missingCount} 个缺失图片，是否查看详细日志？", "是", "否");
                
                if (openLog)
                {
                    string logPath = GetMissingImagesLogPath();
                    if (File.Exists(logPath))
                    {
                        EditorUtility.RevealInFinder(logPath);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            EditorUtility.ClearProgressBar();
            throw;
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            throw;
        }
#endif
    }
    
    /// <summary>
    /// 检查单个图片项
    /// </summary>
    private static async UniTask<bool> CheckPhotoItemAsync(
        PhotoItem item, 
        CancellationToken cancellationToken = default)
    {
        string imageName = $"{item.ImageId}.{item.ImageType}";
        string fullUrl = BuildImageUrl(item.ID, imageName);
        
        try
        {
            // 检查是否被取消
            cancellationToken.ThrowIfCancellationRequested();
            
            using (UnityWebRequest request = UnityWebRequest.Head(fullUrl))
            {
                // 发送请求，支持取消
                var asyncOperation = request.SendWebRequest();
                
                // 等待请求完成，同时支持取消
                while (!asyncOperation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        throw new OperationCanceledException(cancellationToken);
                    }
                    await UniTask.Yield();
                }
                
                bool exists = false;
                
                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                        exists = (request.responseCode == 200);
                        if (exists)
                        {
                            Debug.Log($"图片存在: {imageName} (行: {item.RowId})");
                        }
                        else
                        {
                            Debug.LogWarning($"图片不存在: {imageName} (行: {item.RowId}), 状态码: {request.responseCode}");
                        }
                        break;
                        
                    case UnityWebRequest.Result.ProtocolError:
                        if (request.responseCode == 404 || request.responseCode == 403)
                        {
                            Debug.LogWarning($"图片不存在 ({request.responseCode}): {imageName} (行: {item.RowId})");
                        }
                        else
                        {
                            Debug.LogError($"HTTP错误 ({request.responseCode}): {imageName} (行: {item.RowId})");
                        }
                        break;
                        
                    default:
                        Debug.LogError($"网络错误: {imageName} (行: {item.RowId}) - {request.error}");
                        break;
                }
                
                return exists;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"检查被取消: {imageName}");
            throw;
        }
        catch (Exception e)
        {
            Debug.LogError($"检查图片失败: {imageName} (行: {item.RowId}) - {e.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region 导出功能
    
    /// <summary>
    /// 导出表格图片报告
    /// </summary>
    public static async UniTaskVoid ExportTableImageReport()
    {
#if UNITY_EDITOR
        try
        {
            // 开始新的检查
            StartNewCheck("导出表格图片报告");
            var cancellationToken = _currentCheckCancellationTokenSource.Token;
            
            var photosTable = ConfigLoader.Instance.Tables.TbCfgPhotos.DataList;
            if (photosTable == null || photosTable.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "TbCfgPhotos 表格为空", "确定");
                CompleteCheck();
                return;
            }
            
            // 让用户选择保存位置
            string savePath = EditorUtility.SaveFilePanel(
                "保存图片资源报告",
                Application.dataPath,
                $"TableImages_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                "txt");
            
            if (string.IsNullOrEmpty(savePath))
            {
                CompleteCheck();
                return;
            }
            
            EditorUtility.DisplayProgressBar("生成报告", "收集数据...", 0);
            
            // 检查是否被取消
            cancellationToken.ThrowIfCancellationRequested();
            
            // 读取缺失图片日志
            HashSet<string> missingImages = new HashSet<string>();
            string logPath = GetMissingImagesLogPath();
            if (File.Exists(logPath))
            {
                string[] missingLines = await UniTask.RunOnThreadPool(() => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return File.ReadAllLines(logPath);
                });
                
                foreach (string line in missingLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        missingImages.Add(line.Trim());
                    }
                }
            }
            
            // 检查是否被取消
            cancellationToken.ThrowIfCancellationRequested();
            
            // 生成报告
            StringBuilder report = new StringBuilder();
            report.AppendLine("表格图片资源报告");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"基础域名: {BaseDomain}");
            report.AppendLine($"=============================================");
            report.AppendLine();
            
            int index = 0;
            foreach (var photo in photosTable)
            {
                // 定期检查是否被取消
                if (index % 10 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                
                string imageId = photo.ImageID?.ToString() ?? "";
                string imageType = photo.ImageType?.ToString() ?? "";
                string rowId = GetRowId(photo);
                
                if (string.IsNullOrEmpty(imageId) || string.IsNullOrEmpty(imageType))
                {
                    report.AppendLine($"{index + 1}. [配置错误] 行 {rowId}: ImageId='{imageId}', ImageType='{imageType}'");
                }
                else
                {
                    string imageName = $"{imageId}.{imageType}";
                    string fullUrl = BuildImageUrl(photo.ID, imageName);
                    bool isMissing = missingImages.Contains(imageId);
                    
                    report.AppendLine($"{index + 1}. 行 {rowId}");
                    report.AppendLine($"   图片: {imageName}");
                    report.AppendLine($"   URL: {fullUrl}");
                    report.AppendLine($"   状态: {(isMissing ? "❌ 缺失" : "✅ 正常")}");
                    report.AppendLine();
                }
                
                index++;
                
                // 更新进度
                if (index % 10 == 0)
                {
                    float progress = (float)index / photosTable.Count;
                    EditorUtility.DisplayProgressBar("生成报告", $"处理第 {index}/{photosTable.Count} 行", progress);
                }
            }
            
            // 统计信息
            report.AppendLine("=============================================");
            report.AppendLine("统计信息:");
            report.AppendLine($"总图片数: {photosTable.Count}");
            report.AppendLine($"缺失图片数: {missingImages.Count}");
            report.AppendLine($"缺失比例: {(float)missingImages.Count / photosTable.Count:P2}");
            
            // 检查是否被取消
            cancellationToken.ThrowIfCancellationRequested();
            
            // 保存报告
            await UniTask.RunOnThreadPool(() => 
            {
                File.WriteAllText(savePath, report.ToString());
            });
            
            EditorUtility.ClearProgressBar();
            
            Debug.Log($"报告已保存到: {savePath}");
            
            bool openReport = EditorUtility.DisplayDialog("报告生成完成", 
                $"报告已保存到:\n{savePath}\n\n是否打开报告文件？", "打开", "关闭");
            
            if (openReport)
            {
                EditorUtility.RevealInFinder(savePath);
            }
            
            CompleteCheck();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("报告导出被用户取消");
            EditorUtility.ClearProgressBar();
            CompleteCheck();
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"导出报告失败: {e.Message}");
            CompleteCheck();
            
            EditorUtility.DisplayDialog("错误", $"导出失败: {e.Message}", "确定");
        }
#endif
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 构建完整的图片URL
    /// </summary>
    private static string BuildImageUrl(int ID, string imageName)
    {
        var photo = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(ID);
        if (photo.ABGroup == 1) // B面,测试期间默认使用B面
        {
            return $"{ConfigLoader.Constant.ImgBUrl}{photo.ImageID}.{photo.ImageType}";
        }
        else
        {
            return $"{ConfigLoader.Constant.ImgAUrl}{photo.ImageID}.{photo.ImageType}";
        }
    }
    
    /// <summary>
    /// 获取行的唯一标识
    /// </summary>
    private static string GetRowId(object photoRow)
    {
        try
        {
            // 假设表格行有一个 Id 属性
            var idProperty = photoRow.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                return idProperty.GetValue(photoRow)?.ToString() ?? "Unknown";
            }
            
            // 或者使用索引
            var indexProperty = photoRow.GetType().GetProperty("Index");
            if (indexProperty != null)
            {
                return indexProperty.GetValue(photoRow)?.ToString() ?? "Unknown";
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static async UniTask SaveMissingImagesAsync(
     List<string> missingImageIds,
     CancellationToken cancellationToken = default)
    {
        string logPath = GetMissingImagesLogPath();
        try
        {
            await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                // 直接覆盖写入，不再读取旧文件
                File.WriteAllLines(logPath, missingImageIds);
            });
            Debug.Log($"缺失图片日志已更新（覆盖写入）: {logPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存缺失图片日志失败: {e.Message}");
        }
    }
    /// <summary>
    /// 获取缺失图片日志路径
    /// </summary>
    private static string GetMissingImagesLogPath()
    {
        return Path.Combine(Application.dataPath, "../MissingTableImages.log");
    }
    
    #endregion
    
    #region 数据结构
    
    /// <summary>
    /// 图片项数据结构
    /// </summary>
    private class PhotoItem
    {
        //图片的ID
        public int ID { get; set; }
        //图片名字
        public string ImageId { get; set; }
        //图片类型：jpg、png等
        public string ImageType { get; set; }
        public string RowId { get; set; }
    }

    #endregion

    #region 视频检查功能

    /// <summary>
    /// 检查表格中标记为视频（IsVideo = true）的图片资源对应的 .mp4 文件是否存在
    /// </summary>
    public static async UniTaskVoid CheckVideoFiles()
    {
#if UNITY_EDITOR
        try
        {
            StartNewCheck("检查视频文件");
            var cancellationToken = _currentCheckCancellationTokenSource.Token;

            var photosTable = ConfigLoader.Instance.Tables.TbCfgPhotos.DataList;
            if (photosTable == null || photosTable.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "TbCfgPhotos 表格为空或未加载", "确定");
                CompleteCheck();
                return;
            }

            var videoItems = new List<VideoItem>();
            foreach (var photo in photosTable)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 读取 IsVideo 字段（bool 类型）
                bool isVideo = false;
                var videoProp = photo.GetType().GetProperty("IsVideo");
                if (videoProp != null)
                    isVideo = (bool)videoProp.GetValue(photo);
                else
                {
                    Debug.LogWarning($"行 {GetRowId(photo)} 缺少 IsVideo 字段，跳过");
                    continue;
                }

                if (!isVideo) continue;

                string imageId = photo.ImageID?.ToString() ?? "";
                if (string.IsNullOrEmpty(imageId))
                {
                    Debug.LogWarning($"行 {GetRowId(photo)} 的 ImageID 为空，跳过");
                    continue;
                }

                videoItems.Add(new VideoItem
                {
                    ID = photo.ID,
                    ImageId = imageId,
                    RowId = GetRowId(photo)
                });
            }

            if (videoItems.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有需要检查的视频记录（IsVideo = true）", "确定");
                CompleteCheck();
                return;
            }

            Debug.Log($"开始检查 {videoItems.Count} 个视频文件");
            await CheckVideoItemsAsync(videoItems, "视频文件检查", cancellationToken);
            CompleteCheck();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("视频检查被用户取消");
            CompleteCheck();
        }
        catch (Exception e)
        {
            Debug.LogError($"检查视频文件失败: {e.Message}");
            CompleteCheck();
            EditorUtility.DisplayDialog("错误", $"检查失败: {e.Message}", "确定");
        }
#endif
    }

    /// <summary>
    /// 批量检查视频文件
    /// </summary>
    private static async UniTask CheckVideoItemsAsync(
        List<VideoItem> videoItems,
        string operationName,
        CancellationToken cancellationToken)
    {
#if UNITY_EDITOR
        EditorUtility.DisplayProgressBar(operationName, "初始化...", 0);

        try
        {
            int total = videoItems.Count;
            int completed = 0;
            int existsCount = 0;
            int missingCount = 0;
            List<string> missingVideos = new List<string>();

            int batchSize = 10;
            for (int i = 0; i < total; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int endIndex = Mathf.Min(i + batchSize, total);
                var batchTasks = new List<UniTask<bool>>();

                for (int j = i; j < endIndex; j++)
                {
                    var item = videoItems[j];
                    batchTasks.Add(CheckVideoItemAsync(item, cancellationToken));
                }

                bool[] batchResults = await UniTask.WhenAll(batchTasks);

                for (int j = 0; j < batchResults.Length; j++)
                {
                    completed++;
                    if (batchResults[j])
                        existsCount++;
                    else
                    {
                        missingCount++;
                        missingVideos.Add(videoItems[i + j].ImageId);
                    }

                    float progress = (float)completed / total;
                    EditorUtility.DisplayProgressBar(operationName,
                        $"正在检查 {completed}/{total}", progress);
                }

                await UniTask.Delay(100, cancellationToken: cancellationToken);
            }

            EditorUtility.ClearProgressBar();

            StringBuilder result = new StringBuilder();
            result.AppendLine($"{operationName} 完成:");
            result.AppendLine($"=================================");
            result.AppendLine($"检查总数: {total}");
            result.AppendLine($"存在: {existsCount}");
            result.AppendLine($"缺失: {missingCount}");

            if (missingCount > 0)
            {
                result.AppendLine();
                result.AppendLine("缺失的视频（仅显示 ImageId）:");
                for (int i = 0; i < Mathf.Min(missingVideos.Count, 15); i++)
                {
                    result.AppendLine($"  {i + 1}. {missingVideos[i]}.mp4");
                }
                if (missingVideos.Count > 15)
                    result.AppendLine($"  ... 还有 {missingVideos.Count - 15} 个");

                string logPath = GetMissingVideosLogPath();
                await UniTask.RunOnThreadPool(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.WriteAllLines(logPath, missingVideos);
                });
                result.AppendLine($"缺失日志已保存至: {logPath}");
            }

            Debug.Log(result.ToString());
            EditorUtility.DisplayDialog("检查结果", result.ToString(), "确定");

            if (missingCount > 0)
            {
                bool openLog = EditorUtility.DisplayDialog("缺失视频",
                    $"发现 {missingCount} 个缺失视频，是否查看日志？", "是", "否");
                if (openLog)
                {
                    string logPath = GetMissingVideosLogPath();
                    if (File.Exists(logPath))
                        EditorUtility.RevealInFinder(logPath);
                }
            }
        }
        catch (OperationCanceledException)
        {
            EditorUtility.ClearProgressBar();
            throw;
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            throw;
        }
#endif
    }

    /// <summary>
    /// 检查单个视频文件是否存在（使用 HEAD 请求）
    /// </summary>
    private static async UniTask<bool> CheckVideoItemAsync(
        VideoItem item,
        CancellationToken cancellationToken)
    {
        string videoName = $"{item.ImageId}.mp4";
        string fullUrl = BuildVideoUrl(item.ID, videoName);

        if (string.IsNullOrEmpty(fullUrl))
        {
            Debug.LogError($"无法构建视频 URL: ID={item.ID}, ImageId={item.ImageId}");
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (UnityWebRequest request = UnityWebRequest.Head(fullUrl))
            {
                var asyncOp = request.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        throw new OperationCanceledException(cancellationToken);
                    }
                    await UniTask.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
                {
                    Debug.Log($"视频存在: {videoName} (行: {item.RowId})");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"视频不存在: {videoName} (行: {item.RowId}) - 状态码: {request.responseCode}");
                    return false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"视频检查被取消: {videoName}");
            throw;
        }
        catch (Exception e)
        {
            Debug.LogError($"检查视频失败: {videoName} (行: {item.RowId}) - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 构建视频 URL —— 使用 Constant 表中的 VideoAUrl / VideoBUrl（根据 ABGroup）
    /// </summary>
    private static string BuildVideoUrl(int ID, string videoName)
    {
        var photo = ConfigLoader.Instance.Tables.TbCfgPhotos.Get(ID);
        if (photo == null)
        {
            Debug.LogError($"未找到 ID={ID} 的图片数据");
            return "";
        }

        // 根据 ABGroup 选择对应的视频域名（注意：这里固定使用安卓，若需要 iOS 可自行扩展）
        string baseUrl = photo.ABGroup == 1
            ? ConfigLoader.Constant.VideoBUrl
            : ConfigLoader.Constant.VideoAUrl;

        // 确保末尾有 '/'
        if (!string.IsNullOrEmpty(baseUrl) && !baseUrl.EndsWith("/"))
            baseUrl += "/";

        return $"{baseUrl}{videoName}";
    }

    /// <summary>
    /// 获取缺失视频日志路径
    /// </summary>
    private static string GetMissingVideosLogPath()
    {
        return Path.Combine(Application.dataPath, "../MissingVideos.log");
    }

    /// <summary>
    /// 视频项数据结构
    /// </summary>
    private class VideoItem
    {
        public int ID { get; set; }
        public string ImageId { get; set; }
        public string RowId { get; set; }
    }

    #endregion
}