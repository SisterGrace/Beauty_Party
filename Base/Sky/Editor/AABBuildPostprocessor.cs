using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using System.IO;

public class AABBuildPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    private static string ConfigDir =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../ToolBuild"));

    private const string PYTHON_EXE = @"C:\Users\41294\AppData\Local\Programs\Python\Python313\python.exe";

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        string outputPath = report.summary.outputPath;
        string extension = Path.GetExtension(outputPath).ToLower();

        // 只处理 AAB 或 APK
        if (extension != ".aab" && extension != ".apk")
            return;

        if (!File.Exists(outputPath))
        {
            UnityEngine.Debug.LogWarning($"⚠️ 构建产物不存在: {outputPath}");
            PlaySound("fail");
            return;
        }

        UnityEngine.Debug.Log($"✅ Android 构建成功: {outputPath}");
        EditorUtility.RevealInFinder(outputPath);

        // 根据扩展名选择对应的 Python 脚本
        string pythonScript;
        if (extension == ".aab")
            pythonScript = Path.Combine(ConfigDir, "deploy_aab.py");  // 处理 AAB 的脚本
        else // .apk
            pythonScript = Path.Combine(ConfigDir, "deploy_apk.py");       // 处理 APK 的脚本

        if (!File.Exists(pythonScript))
        {
            //UnityEngine.Debug.LogWarning($"⚠️ 未找到对应的 Python 脚本: {pythonScript}，跳过部署。");
            return;
        }

        // 构建参数：传递输出路径，并添加 --auto 表示自动（如果脚本支持）
        string arguments = $"\"{pythonScript}\" \"{outputPath}\" --auto";
        UnityEngine.Debug.Log($"🔄 启动 Python: {PYTHON_EXE} {arguments}");

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = PYTHON_EXE,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                bool exited = process.WaitForExit(60000);
                if (!exited)
                {
                    process.Kill();
                    UnityEngine.Debug.LogError("❌ Python 执行超时（60秒），已强制结束。");
                    PlaySound("fail");
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                    UnityEngine.Debug.Log($"Python 输出:\n{output}");
                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.LogError($"Python 错误:\n{error}");

                if (process.ExitCode == 0)
                {
                    UnityEngine.Debug.Log("✅ Python 脚本执行完毕");
                    PlaySound("success");
                }
                else
                {
                    UnityEngine.Debug.LogError($"Python 脚本执行失败，退出码: {process.ExitCode}");
                    PlaySound("fail");
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"❌ 调用 Python 异常: {ex.Message}");
            PlaySound("fail");
        }
    }

    // ============================================
    // 播放音效（Unity 内部完成）
    // ============================================
    private static void PlaySound(string type)
    {
        return;
        try
        {
            string soundDir = Path.Combine(ConfigDir, "Sounds");
            string soundFile = type == "success" ? "success.wav" : "fail.wav";
            string fullPath = Path.Combine(soundDir, soundFile);

            if (File.Exists(fullPath))
            {
                using (var soundPlayer = new System.Media.SoundPlayer(fullPath))
                {
                    soundPlayer.PlaySync();
                }
                UnityEngine.Debug.Log($"🔊 播放音效: {fullPath}");
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
                if (type == "success")
                {
                    System.Threading.Thread.Sleep(200);
                    System.Media.SystemSounds.Beep.Play();
                    System.Threading.Thread.Sleep(200);
                    System.Media.SystemSounds.Beep.Play();
                }
                UnityEngine.Debug.LogWarning($"⚠️ 音效文件不存在: {fullPath}，使用蜂鸣声代替。");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"播放音效失败: {ex.Message}");
        }
    }
}