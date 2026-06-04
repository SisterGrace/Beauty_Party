using System;
using UnityEditor;

namespace FeishuBuildTool.Editor
{
    /// <summary>
    /// 构建管线接口。每个项目实现此接口来定义自己的预构建步骤（如 DLL 编译、AssetBundle 打包等）。
    /// BuildToolWindow 通过反射自动发现实现类，无需手动注册。
    /// </summary>
    public interface IBuildPipeline
    {
        /// <summary>
        /// 在 Build Tool 窗口中显示的管线名称。
        /// </summary>
        string PipelineName { get; }

        /// <summary>
        /// 执行 APK 构建前的所有准备步骤（DLL 编译、AssetBundle 打包等）。
        /// 框架会在调用此方法后自动执行 BuildPipeline.BuildPlayer。
        /// </summary>
        /// <param name="target">构建目标平台</param>
        /// <param name="cleanBuild">是否 Clean Build</param>
        /// <param name="onProgress">进度回调 (描述, 进度 0~1)</param>
        void PreBuild(BuildTarget target, bool cleanBuild, Action<string, float> onProgress);
    }
}
