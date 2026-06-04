using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TK_Engine.Editor
{
    public static class LubanTools
    {
        private static readonly string ToolDir = System.IO.Path.GetFullPath(Application.dataPath + @"/../../Configs/Tool");

        private static bool IsWindows => Application.platform == RuntimePlatform.WindowsEditor;

        [MenuItem("TK_Engine/Tools/Luban 转表")]
        public static void BuildLubanExcel()
        {
            if (!RunScript("gen_code_bin"))
                return;

            if (!RunScript("Move"))
                return;

            AssetDatabase.Refresh();
            Debug.Log("[Luban] 转表流程全部完成");
        }

        private static bool RunScript(string scriptName)
        {
            string ext = IsWindows ? ".bat" : ".sh";
            string fileName = scriptName + ext;
            string scriptPath = System.IO.Path.Combine(ToolDir, fileName);

            if (!System.IO.File.Exists(scriptPath))
            {
                Debug.LogError($"[Luban] 脚本文件不存在: {scriptPath}");
                return false;
            }

            Debug.Log($"[Luban] 开始执行: {fileName}");

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = ToolDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (IsWindows)
            {
                startInfo.FileName = scriptPath;
            }
            else
            {
                startInfo.FileName = "/bin/bash";
                startInfo.Arguments = scriptPath;
            }

            var process = Process.Start(startInfo);
            //读取标准输出
            string output = process.StandardOutput.ReadToEnd();
            //读取标准错误
            string error = process.StandardError.ReadToEnd();
            //等待进程退出
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
                Debug.Log($"[Luban] {output}");
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"[Luban] {error}");

            //获取进程退出码（0 通常代表成功）
            if (process.ExitCode != 0)
            {
                Debug.LogError($"[Luban] {fileName} 执行失败, ExitCode: {process.ExitCode}");
                return false;
            }

            Debug.Log($"[Luban] {fileName} 执行成功");
            return true;
        }
        
        [MenuItem("TK_Engine/Tools/打开表格目录")]
        public static void OpenConfigFolder()
        {
            OpenFolderHelper.Execute(Application.dataPath + @"/../../Configs/Excel");
        }
    }
}