using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace FeishuBuildTool.Editor
{
    /// <summary>
    /// Android 打包工具窗口。
    /// 通过反射自动发现 IBuildPipeline 实现类，将项目特定的构建逻辑解耦。
    /// 通用部分（飞书通知、用户管理、云盘管理、APK 签名与构建）全部在此处理。
    /// </summary>
    public class BuildToolWindow : EditorWindow
    {
        private BuildToolConfig _config;
        private int _selectedUserIndex;
        private bool _cleanBuild;
        private bool _isBuilding;
        private string _statusMessage = "";
        private Vector2 _scrollPos;

        private bool _showFeishuSection = true;
        private bool _showUserSection = true;
        private bool _showKeystoreSection = true;

        private string _newUserName = "";
        private string _newUserOpenId = "";
        private bool _showRegisterPanel;

        private bool _showDriveSection;
        private List<FeishuAPI.DriveFileInfo> _driveFiles;
        private bool _driveLoading;
        private Vector2 _driveScrollPos;

        private IBuildPipeline[] _pipelines;
        private int _selectedPipelineIndex;

        [MenuItem("Tools/Feishu Android Build Tool", false, 80)]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildToolWindow>("Android Build Tool");
            window.minSize = new Vector2(460, 580);
        }

        private void OnEnable()
        {
            _config = BuildToolConfig.Load();
            _selectedUserIndex = _config.GetUserIndex(_config.lastUser);
            if (_selectedUserIndex < 0 && _config.users.Count > 0) _selectedUserIndex = 0;
            DiscoverPipelines();
        }

        private void DiscoverPipelines()
        {
            var pipelineType = typeof(IBuildPipeline);
            var found = new List<IBuildPipeline>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.IsAbstract && !type.IsInterface && pipelineType.IsAssignableFrom(type))
                        {
                            var instance = (IBuildPipeline)Activator.CreateInstance(type);
                            found.Add(instance);
                        }
                    }
                }
                catch
                {
                    // skip assemblies that can't be reflected
                }
            }

            _pipelines = found.ToArray();
            _selectedPipelineIndex = 0;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Android Build Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawPipelineSection();
            EditorGUILayout.Space(4);
            DrawFeishuSection();
            EditorGUILayout.Space(4);
            DrawUserSection();
            EditorGUILayout.Space(4);
            DrawKeystoreSection();
            EditorGUILayout.Space(4);
            DrawBuildSection();
            EditorGUILayout.Space(4);
            DrawStatusSection();
            EditorGUILayout.Space(4);
            DrawDriveSection();

            EditorGUILayout.EndScrollView();
        }

        #region UI Sections

        private void DrawPipelineSection()
        {
            if (_pipelines == null || _pipelines.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "未找到 IBuildPipeline 实现。\n" +
                    "请在你的项目中创建一个类实现 FeishuBuildTool.Editor.IBuildPipeline 接口。",
                    MessageType.Error);
                return;
            }

            if (_pipelines.Length > 1)
            {
                string[] names = _pipelines.Select(p => p.PipelineName).ToArray();
                _selectedPipelineIndex = EditorGUILayout.Popup("构建管线", _selectedPipelineIndex, names);
            }
            else
            {
                EditorGUILayout.LabelField("构建管线", _pipelines[0].PipelineName);
            }
        }

        private void DrawFeishuSection()
        {
            _showFeishuSection = EditorGUILayout.Foldout(_showFeishuSection, "飞书配置", true);
            if (!_showFeishuSection) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            _config.feishuAppId = EditorGUILayout.TextField("App ID", _config.feishuAppId);
            _config.feishuAppSecret = EditorGUILayout.PasswordField("App Secret", _config.feishuAppSecret);
            if (EditorGUI.EndChangeCheck()) _config.Save();

            if (string.IsNullOrEmpty(_config.feishuAppId) || string.IsNullOrEmpty(_config.feishuAppSecret))
            {
                EditorGUILayout.HelpBox(
                    "飞书凭据为空。请手动填写，或确保 ~/.openclaw/openclaw.json 中已配置。",
                    MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawUserSection()
        {
            _showUserSection = EditorGUILayout.Foldout(_showUserSection, "用户管理", true);
            if (!_showUserSection) return;

            EditorGUI.indentLevel++;

            string[] userNames = _config.GetUserNames();

            if (userNames.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                _selectedUserIndex = EditorGUILayout.Popup("当前用户", _selectedUserIndex, userNames);
                if (_selectedUserIndex >= 0 && _selectedUserIndex < userNames.Length)
                {
                    _config.lastUser = userNames[_selectedUserIndex];
                    var user = _config.FindUser(_config.lastUser);
                    if (user != null)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Open ID", user.openId);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("删除此用户", GUILayout.Width(80)))
                        {
                            if (EditorUtility.DisplayDialog("确认", $"删除用户 \"{user.name}\"？", "删除", "取消"))
                            {
                                _config.RemoveUser(user.name);
                                _config.Save();
                                _selectedUserIndex = _config.users.Count > 0 ? 0 : -1;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("尚未注册任何用户，请先注册。", MessageType.Info);
            }

            EditorGUILayout.Space(2);
            _showRegisterPanel = EditorGUILayout.Foldout(_showRegisterPanel, "注册新用户", true);
            if (_showRegisterPanel)
            {
                EditorGUI.indentLevel++;
                _newUserName = EditorGUILayout.TextField("用户名", _newUserName);
                _newUserOpenId = EditorGUILayout.TextField("飞书 Open ID", _newUserOpenId);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = !string.IsNullOrEmpty(_newUserName) && !string.IsNullOrEmpty(_newUserOpenId);
                if (GUILayout.Button("注册", GUILayout.Width(80)))
                {
                    _config.AddOrUpdateUser(_newUserName, _newUserOpenId);
                    _config.lastUser = _newUserName;
                    _config.Save();
                    _selectedUserIndex = _config.GetUserIndex(_newUserName);
                    _newUserName = "";
                    _newUserOpenId = "";
                    _showRegisterPanel = false;
                    Debug.Log($"[BuildTool] User registered: {_config.lastUser}");
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawKeystoreSection()
        {
            _showKeystoreSection = EditorGUILayout.Foldout(_showKeystoreSection, "Keystore 签名", true);
            if (!_showKeystoreSection) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            _config.keystorePass = EditorGUILayout.PasswordField("Keystore Password", _config.keystorePass);
            _config.keyAliasPass = EditorGUILayout.PasswordField("Key Alias Password", _config.keyAliasPass);
            _config.rememberPassword = EditorGUILayout.Toggle("记住密码", _config.rememberPassword);
            if (EditorGUI.EndChangeCheck()) _config.Save();

            if (string.IsNullOrEmpty(_config.keystorePass) || string.IsNullOrEmpty(_config.keyAliasPass))
            {
                EditorGUILayout.HelpBox("Keystore 密码未填写，构建 APK 签名时会失败。", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBuildSection()
        {
            EditorGUILayout.LabelField("构建选项", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            _cleanBuild = EditorGUILayout.Toggle("Clean Build", _cleanBuild);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(6);

            bool hasPipeline = _pipelines != null && _pipelines.Length > 0;
            GUI.enabled = !_isBuilding && hasPipeline;
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 36
            };

            if (GUILayout.Button(_isBuilding ? "打包中..." : "开始打包", buttonStyle))
            {
                StartBuild();
            }
            GUI.enabled = true;
        }

        private void DrawStatusSection()
        {
            if (string.IsNullOrEmpty(_statusMessage)) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
        }

        private void DrawDriveSection()
        {
            _showDriveSection = EditorGUILayout.Foldout(_showDriveSection, "应用云盘管理", true);
            if (!_showDriveSection) return;

            EditorGUI.indentLevel++;

            if (string.IsNullOrEmpty(_config.feishuAppId) || string.IsNullOrEmpty(_config.feishuAppSecret))
            {
                EditorGUILayout.HelpBox("请先配置飞书 App ID / Secret。", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_driveLoading;
            if (GUILayout.Button(_driveLoading ? "查询中..." : "刷新文件列表", GUILayout.Width(120)))
            {
                RefreshDriveFiles();
            }
            GUI.enabled = true;

            if (_driveFiles != null && _driveFiles.Count > 0)
            {
                if (GUILayout.Button("清空全部", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("确认", $"删除云盘上全部 {_driveFiles.Count} 个文件？", "删除", "取消"))
                        DeleteAllDriveFiles();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_driveFiles != null)
            {
                if (_driveFiles.Count == 0)
                {
                    EditorGUILayout.LabelField("  云盘中没有文件");
                }
                else
                {
                    long totalBytes = 0;
                    foreach (var f in _driveFiles) totalBytes += f.size;
                    long totalMB = totalBytes / 1024 / 1024;
                    EditorGUILayout.LabelField($"  共 {_driveFiles.Count} 个文件，总大小 {totalMB}MB");

                    _driveScrollPos = EditorGUILayout.BeginScrollView(_driveScrollPos,
                        GUILayout.MaxHeight(160));

                    for (int i = 0; i < _driveFiles.Count; i++)
                    {
                        var f = _driveFiles[i];
                        long mb = f.size / 1024 / 1024;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  {f.name}  ({mb}MB)", GUILayout.MinWidth(200));
                        if (GUILayout.Button("X", GUILayout.Width(24)))
                        {
                            DeleteSingleDriveFile(f.token, i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void RefreshDriveFiles()
        {
            _driveLoading = true;
            Repaint();
            try
            {
                string token = FeishuAPI.GetTenantAccessToken(_config.feishuAppId, _config.feishuAppSecret);
                _driveFiles = FeishuAPI.DriveListFiles(token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildTool] List drive files failed: {e.Message}");
                EditorUtility.DisplayDialog("查询失败", e.Message, "确定");
                _driveFiles = null;
            }
            finally
            {
                _driveLoading = false;
                Repaint();
            }
        }

        private void DeleteAllDriveFiles()
        {
            try
            {
                string token = FeishuAPI.GetTenantAccessToken(_config.feishuAppId, _config.feishuAppSecret);
                int deleted = 0;
                foreach (var f in _driveFiles)
                {
                    FeishuAPI.DriveDeleteFile(token, f.token);
                    deleted++;
                }

                _config.driveFileTokens.Clear();
                _config.Save();

                EditorUtility.DisplayDialog("完成", $"已删除 {deleted} 个文件", "确定");
                _driveFiles = new List<FeishuAPI.DriveFileInfo>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildTool] Delete all failed: {e.Message}");
                EditorUtility.DisplayDialog("删除失败", e.Message, "确定");
            }
            Repaint();
        }

        private void DeleteSingleDriveFile(string fileToken, int index)
        {
            try
            {
                string token = FeishuAPI.GetTenantAccessToken(_config.feishuAppId, _config.feishuAppSecret);
                FeishuAPI.DriveDeleteFile(token, fileToken);

                _config.driveFileTokens.RemoveAll(r => r.fileToken == fileToken);
                _config.Save();

                if (_driveFiles != null && index >= 0 && index < _driveFiles.Count)
                    _driveFiles.RemoveAt(index);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildTool] Delete file failed: {e.Message}");
            }
            Repaint();
        }

        #endregion

        #region Build + Notify Flow

        /// <summary>
        /// APK 输出目录（ToolBuild/output/），通用约定。
        /// </summary>
        private static string GetApkOutputDir()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ToolBuild/output"));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// APK 文件名：包名_月日时分.apk，通用约定。
        /// </summary>
        private static string GetApkFileName()
        {
            string bundleId = PlayerSettings.applicationIdentifier;
            string timestamp = DateTime.Now.ToString("MMddHHmm");
            return $"{bundleId}_{timestamp}.apk";
        }

        private void StartBuild()
        {
            if (_config.users.Count == 0 || _selectedUserIndex < 0 ||
                _selectedUserIndex >= _config.users.Count)
            {
                EditorUtility.DisplayDialog("提示", "请先注册并选择一个用户。", "确定");
                return;
            }

            if (string.IsNullOrEmpty(_config.feishuAppId) ||
                string.IsNullOrEmpty(_config.feishuAppSecret))
            {
                EditorUtility.DisplayDialog("提示", "飞书 App ID 和 App Secret 不能为空。", "确定");
                return;
            }

            if (string.IsNullOrEmpty(_config.keystorePass) ||
                string.IsNullOrEmpty(_config.keyAliasPass))
            {
                if (!EditorUtility.DisplayDialog("Keystore 密码",
                        "Keystore 密码未填写，APK 签名会失败。\n是否继续？", "继续", "返回填写"))
                    return;
            }

            var pipeline = _pipelines[_selectedPipelineIndex];
            var targetUser = _config.users[_selectedUserIndex];
            string mode = _cleanBuild ? "Clean Build" : "Build";

            if (!EditorUtility.DisplayDialog("确认打包",
                    $"管线: {pipeline.PipelineName}\n模式: {mode}\n通知用户: {targetUser.name}\nOpen ID: {targetUser.openId}\n\n开始打包？",
                    "开始", "取消"))
                return;

            _config.Save();
            _isBuilding = true;
            _statusMessage = "准备打包...";
            Repaint();

            string apkPath = null;
            bool buildSuccess = false;
            string errorMessage = "";

            try
            {
                if (!string.IsNullOrEmpty(_config.keystorePass))
                    PlayerSettings.Android.keystorePass = _config.keystorePass;
                if (!string.IsNullOrEmpty(_config.keyAliasPass))
                    PlayerSettings.Android.keyaliasPass = _config.keyAliasPass;

                BuildTarget target = BuildTarget.Android;

                // IBuildPipeline: project-specific pre-build steps
                pipeline.PreBuild(target, _cleanBuild, (desc, progress) =>
                {
                    _statusMessage = desc;
                    Repaint();
                    EditorUtility.DisplayProgressBar("Android Build", desc, progress);
                });
                AssetDatabase.Refresh();

                // Generic APK build
                _statusMessage = "构建 APK...";
                Repaint();
                EditorUtility.DisplayProgressBar("Android Build", "构建 APK...", 0.6f);
                apkPath = Path.Combine(GetApkOutputDir(), GetApkFileName());

                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                AssetDatabase.Refresh();

                var options = _cleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None;
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                    locationPathName = apkPath,
                    targetGroup = BuildTargetGroup.Android,
                    target = BuildTarget.Android,
                    options = options
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    buildSuccess = true;
                    long sizeMB = (long)(report.summary.totalSize / 1024 / 1024);
                    _statusMessage = $"构建成功！({sizeMB}MB) 正在发送飞书通知...";
                    Debug.Log($"[BuildTool] Build success: {sizeMB}MB, path: {apkPath}");
                }
                else
                {
                    errorMessage =
                        $"Build result: {report.summary.result}, totalErrors: {report.summary.totalErrors}";
                    _statusMessage = $"构建失败: {errorMessage}";
                    Debug.LogError($"[BuildTool] Build failed: {errorMessage}");
                }
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                buildSuccess = false;
                errorMessage = e.Message;
                _statusMessage = $"构建异常: {e.Message}";
                Debug.LogError($"[BuildTool] Build exception: {e}");
            }

            Repaint();
            SendFeishuNotification(buildSuccess, apkPath, errorMessage, targetUser, mode);
        }

        private void SendFeishuNotification(bool buildSuccess, string apkPath, string errorMessage,
            BuildToolConfig.FeishuUser targetUser, string mode)
        {
            try
            {
                _statusMessage += "\n正在发送飞书通知...";
                Repaint();

                string token = FeishuAPI.GetTenantAccessToken(_config.feishuAppId, _config.feishuAppSecret);

                if (buildSuccess && !string.IsNullOrEmpty(apkPath) && File.Exists(apkPath))
                {
                    long sizeMB = new FileInfo(apkPath).Length / 1024 / 1024;
                    string fileName = Path.GetFileName(apkPath);
                    string successText =
                        $"✅ Android 打包成功！\n" +
                        $"👤 构建者: {targetUser.name}\n" +
                        $"🔧 模式: {mode}\n" +
                        $"📦 文件: {fileName}\n" +
                        $"📏 大小: {sizeMB}MB\n" +
                        $"🕐 时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                    FeishuAPI.SendText(token, targetUser.openId, successText);

                    _statusMessage = "构建成功！正在上传 APK...";
                    Repaint();

                    try
                    {
                        string bundleId = PlayerSettings.applicationIdentifier;
                        string oldToken = _config.GetDriveToken(bundleId);
                        string newFileToken = FeishuAPI.SendFile(token, targetUser.openId, apkPath,
                            oldDriveFileToken: oldToken);

                        if (!string.IsNullOrEmpty(newFileToken))
                        {
                            _config.SetDriveToken(bundleId, newFileToken);
                            _config.Save();
                        }

                        _statusMessage = "全部完成！APK 已发送到飞书。";
                    }
                    catch (Exception fe)
                    {
                        _statusMessage = $"构建成功，但 APK 上传失败: {fe.Message}\n文件路径: {apkPath}";
                        Debug.LogWarning($"[BuildTool] File send failed: {fe.Message}");
                        FeishuAPI.SendText(token, targetUser.openId,
                            $"⚠️ APK 文件上传失败\n📂 请到构建机器上获取:\n{apkPath}");
                    }
                }
                else
                {
                    string failText =
                        $"❌ Android 打包失败\n" +
                        $"👤 构建者: {targetUser.name}\n" +
                        $"🔧 模式: {mode}\n" +
                        $"❗ 原因: {FeishuEscape(errorMessage)}\n" +
                        $"🕐 时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                    FeishuAPI.SendText(token, targetUser.openId, failText);
                    _statusMessage = $"构建失败，已发送通知到飞书。\n{errorMessage}";
                }

                EditorUtility.DisplayDialog(buildSuccess ? "打包完成" : "打包失败",
                    _statusMessage, "确定");
            }
            catch (Exception e)
            {
                _statusMessage += $"\n飞书通知发送失败: {e.Message}";
                Debug.LogError($"[BuildTool] Feishu notify failed: {e}");
                EditorUtility.DisplayDialog("飞书通知失败",
                    $"构建{(buildSuccess ? "成功" : "失败")}，但飞书通知发送失败:\n{e.Message}", "确定");
            }
            finally
            {
                _isBuilding = false;
                Repaint();
            }
        }

        private static string FeishuEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", " ").Replace("\r", "");
        }

        #endregion
    }
}
