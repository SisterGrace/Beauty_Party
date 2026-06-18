using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

namespace TK_Engine.Editor.UI
{
    public class ScriptGenerator
    {
        public static List<ScriptGenerateRuler> componentDir
        {
            get
            {
                

                return SettingsUtils.GetScriptGenerateRule();
            }
        }

        public static string uiCsPath = Application.dataPath + "/GameScripts/Game/GameLogic/UI/{0}.cs";

        public static Dictionary<string, Dictionary<string, string>> fieldDir =
            new Dictionary<string, Dictionary<string, string>>();

        [MenuItem("GameObject/TK/All  %q", priority = 40)]
        public static void All()
        {
            CheckPanel();
            CreateViewCs();
            CreatePanelCs();
        }

        [MenuItem("GameObject/TK/CheckPanel", priority = 41)]
        public static void CheckPanel()
        {
            var root = Selection.activeTransform;
            if (root != null)
            {
                if (root.name.Contains("Widget_"))
                {
                    return;
                }
                var canvas = root.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = root.gameObject.AddComponent<Canvas>();
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.sortingOrder = 0;
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "Default";
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
                
                var graphicRaycaster = root.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                {
                    graphicRaycaster = root.gameObject.AddComponent<GraphicRaycaster>();
                }

            }
        }

        [MenuItem("GameObject/TK/CreateViewCs %w", priority = 42)]
        public static void CreateViewCs()
        {
            var root = Selection.activeTransform;
            if (root != null)
            {
                fieldDir.Clear();
                GetComponentsInChildren(root, root);
                StringBuilder sb = new StringBuilder();
                List<string> codeBlock = new List<string>();
                var path = string.Format(uiCsPath, root.name + "View");
                if (File.Exists(path))
                {
                    bool isRead = false;
                    var allLine = File.ReadAllLines(path);
                    foreach (var line in allLine)
                    {
                        if (line.Contains("#endregion")) break;
                        if (isRead) codeBlock.Add(line);
                        if (line.Contains("#region 可编辑代码块")) isRead = true;
                    }
                }
                
                sb.AppendLine("using TMPro;");
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine("using UnityEngine.UI;");
                //sb.AppendLine("using SuperScrollView;");
                sb.AppendLine("using Y_Base;");
                sb.AppendLine("using TK_Engine;");
                sb.AppendLine("using RenderHeads.Media.AVProVideo;");
                sb.AppendLine("namespace GameLogic");
                sb.AppendLine("{");
                sb.AppendLine($"\tpublic partial class {root.name}");
                sb.AppendLine("\t{");
                sb.AppendLine("");
                foreach (var info in fieldDir)
                {
                    foreach (var data in info.Value)
                    {
                        sb.AppendLine($"\t\tprotected {info.Key} m{data.Key};");
                    }
                }
                sb.AppendLine("");
                sb.AppendLine("\t\tprotected override void ScriptGenerator()");
                sb.AppendLine("\t\t{");
                foreach (var info in fieldDir)
                {
                    foreach (var data in info.Value)
                    {
                        if (info.Key.Equals("Transform"))
                            sb.AppendLine($"\t\t\tm{data.Key} = FindChild(\"{data.Value}\");");
                        else if (info.Key.Equals("GameObject"))
                            sb.AppendLine($"\t\t\tm{data.Key} = FindChild(\"{data.Value}\").gameObject;");
                        else 
                            sb.AppendLine($"\t\t\tm{data.Key} = FindChildComponent<{info.Key}>(\"{data.Value}\");");
                    }
                }
                sb.AppendLine("\t\t\tInitCodeBlock();");
                sb.AppendLine("\t\t}");
                sb.AppendLine("");
                sb.AppendLine("\t\t#region 可编辑代码块");
                if (codeBlock.Count > 0)
                {
                    foreach (var line in codeBlock)
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    sb.AppendLine("\t\tprivate void InitCodeBlock()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t}");
                }
                sb.AppendLine("\t\t#endregion");
                sb.AppendLine("\t}");
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
            }
        }
        
        [MenuItem("GameObject/TK/CreatePanelCs", priority = 41)]
        public static void CreatePanelCs()
        {
            var root = Selection.activeTransform;
            if (root != null)
            {
                StringBuilder sb = new StringBuilder();
                var path = string.Format(uiCsPath, root.name);
                if (File.Exists(path))
                {
                    Debug.LogError("该文件已创建，无法覆盖");
                    return;
                }

                string baseName = "UIWindow";
                if (root.name.Contains("Widget_"))
                {
                    baseName = "UIWidget";
                }
                
                sb.AppendLine("using TK_Engine;");
                sb.AppendLine("using UnityEngine;");
                //sb.AppendLine("using SuperScrollView;");
                sb.AppendLine("");
                sb.AppendLine("using System.Collections;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("namespace GameLogic");
                sb.AppendLine("{");
                sb.AppendLine("\t[Window(UILayer.UI)]");
                sb.AppendLine($"\tpublic partial class {root.name} : {baseName}");
                sb.AppendLine("\t{");
                if (baseName.Equals("UIWindow"))
                {
                    sb.AppendLine("\t\tprotected override void OnRefresh()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tprotected override void RegisterEvent()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tprotected override void Close()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t}");
                }
                else
                {
                    sb.AppendLine("\t\tpublic void Open()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tpublic void Hide()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t}");
                }
                sb.AppendLine("\t}");
                sb.AppendLine("}");    
                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
            }
        }

        public static void GetComponentsInChildren(Transform root, Transform trans)
        {
            if (root != null)
            {
                if (trans.name.Contains("Item_"))
                {
                    // 子 Item 不再往下遍历
                    return;
                }
                for (int i = 0; i < trans.childCount; i++)
                {
                    Transform child = trans.GetChild(i);
                    
                    // 检查是否是 Item_ 对象，如果是则添加 GameObject 类型并跳过子遍历
                    if (child.name.Contains("Item_"))
                    {
                        if (!fieldDir.ContainsKey("GameObject"))
                        {
                            fieldDir.Add("GameObject", new Dictionary<string, string>());
                        }
                        var name = child.name.Split("_")[1];
                        fieldDir["GameObject"].Add(name, GetRelativePath(child, root));
                        continue; // 不再遍历 Item_ 的子对象
                    }
                    
                    foreach (var data in componentDir)
                    {
                        if (child.name.Contains(data.uiElementRegex + "_"))
                        {
                            if (!fieldDir.ContainsKey(data.componentName))
                            {
                                fieldDir.Add(data.componentName, new Dictionary<string, string>());
                            }
                            var name = child.name.Split("_")[1];
                            fieldDir[data.componentName].Add(name, GetRelativePath(child, root));
                        }
                    }
                    GetComponentsInChildren(root, child);
                }
            }
        }
        
        private static string GetRelativePath(Transform child, Transform root)
        {
            StringBuilder path = new StringBuilder();
            path.Append(child.name);
            while (child.parent != null && child.parent != root)
            {
                child = child.parent;
                path.Insert(0, "/");
                path.Insert(0, child.name);
            }

            return path.ToString();
        }
    }

    public class GeneratorHelper : EditorWindow
    {
        [MenuItem("GameObject/TK/UI命名规则", priority = 49)]
        public static void About()
        {
            GeneratorHelper welcomeWindow = (GeneratorHelper)EditorWindow.GetWindow(typeof(GeneratorHelper), false, "About");
        }
        public void Awake()
        {
            minSize = new Vector2(400, 600);
        }
        protected void OnGUI()
        {
            GUILayout.BeginVertical();
            foreach (var item in SettingsUtils.GetScriptGenerateRule())
            {
                GUILayout.Label(item.uiElementRegex + "：\t" + item.componentName);
            }

            GUILayout.EndVertical();
        }
    }
        /*private const string Gap = "/";

        [MenuItem("GameObject/ScriptGenerator/UIProperty", priority = 41)]
        public static void MemberProperty()
        {
            Generate(false);
        }

        [MenuItem("GameObject/ScriptGenerator/UIProperty - UniTask", priority = 43)]
        public static void MemberPropertyUniTask()
        {
            Generate(false, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListener", priority = 42)]
        public static void MemberPropertyAndListener()
        {
            Generate(true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListener - UniTask", priority = 44)]
        public static void MemberPropertyAndListenerUniTask()
        {
            Generate(true, true);
        }

        private static void Generate(bool includeListener, bool isUniTask = false)
        {
            var root = Selection.activeTransform;
            if (root != null)
            {
                StringBuilder strVar = new StringBuilder();
                StringBuilder strBind = new StringBuilder();
                StringBuilder strOnCreate = new StringBuilder();
                StringBuilder strCallback = new StringBuilder();
                Ergodic(root, root, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
                StringBuilder strFile = new StringBuilder();

                if (includeListener)
                {
#if ENABLE_TEXTMESHPRO
                    strFile.Append("using TMPro;\n");
#endif
                    if (isUniTask)
                    {
                        strFile.Append("using Cysharp.Threading.Tasks;\n");
                    }

                    strFile.Append("using UnityEngine;\n");
                    strFile.Append("using UnityEngine.UI;\n");
                    strFile.Append("using TK_Engine;\n\n");
                    strFile.Append($"namespace {SettingsUtils.GetUINameSpace()}\n");
                    strFile.Append("{\n");
                    strFile.Append("\t[Window(UILayer.UI)]\n");
                    strFile.Append("\tclass " + root.name + " : UIWindow\n");
                    strFile.Append("\t{\n");
                }

                // 脚本工具生成的代码
                strFile.Append("\t\t#region 脚本工具生成的代码\n");
                strFile.Append(strVar);
                strFile.Append("\t\tprotected override void ScriptGenerator()\n");
                strFile.Append("\t\t{\n");
                strFile.Append(strBind);
                strFile.Append(strOnCreate);
                strFile.Append("\t\t}\n");
                strFile.Append("\t\t#endregion");

                if (includeListener)
                {
                    strFile.Append("\n\n");
                    // #region 事件
                    strFile.Append("\t\t#region 事件\n");
                    strFile.Append(strCallback);
                    strFile.Append("\t\t#endregion\n\n");

                    strFile.Append("\t}\n");
                    strFile.Append("}\n");
                }

                TextEditor te = new TextEditor();
                te.text = strFile.ToString();
                te.SelectAll();
                te.Copy();
            }
        }

        public static void Ergodic(Transform root, Transform transform, ref StringBuilder strVar, ref StringBuilder strBind, ref StringBuilder strOnCreate,
            ref StringBuilder strCallback, bool isUniTask)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                WriteScript(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
                if (child.name.StartsWith("m_item"))
                {
                    // 子 Item 不再往下遍历
                    continue;
                }

                Ergodic(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            }
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            StringBuilder path = new StringBuilder();
            path.Append(child.name);
            while (child.parent != null && child.parent != root)
            {
                child = child.parent;
                path.Insert(0, Gap);
                path.Insert(0, child.name);
            }

            return path.ToString();
        }

        public static string GetBtnFuncName(string varName)
        {
            return "OnClick" + varName.Replace("m_btn", string.Empty) + "Btn";
        }

        public static string GetToggleFuncName(string varName)
        {
            return "OnToggle" + varName.Replace("m_toggle", string.Empty) + "Change";
        }

        public static string GetSliderFuncName(string varName)
        {
            return "OnSlider" + varName.Replace("m_slider", string.Empty) + "Change";
        }

        private static void WriteScript(Transform root, Transform child, ref StringBuilder strVar, ref StringBuilder strBind, ref StringBuilder strOnCreate,
            ref StringBuilder strCallback, bool isUniTask)
        {
            string varName = child.name;
            string componentName = string.Empty;

            var rule = SettingsUtils.GetScriptGenerateRule().Find(t => varName.StartsWith(t.uiElementRegex));

            if (rule != null)
            {
                componentName = rule.componentName;
            }

            if (componentName == string.Empty)
            {
                return;
            }

            string varPath = GetRelativePath(child, root);
            if (!string.IsNullOrEmpty(varName))
            {
                strVar.Append("\t\tprivate " + componentName + " " + varName + ";\n");
                switch (componentName)
                {
                    case "Transform":
                        strBind.Append($"\t\t\t{varName} = FindChild(\"{varPath}\");\n");
                        break;
                    case "GameObject":
                        strBind.Append($"\t\t\t{varName} = FindChild(\"{varPath}\").gameObject;\n");
                        break;
                    case "AnimationCurve":
                        strBind.Append($"\t\t\t{varName} = FindChildComponent<AnimCurveObject>(\"{varPath}\").m_animCurve;\n");
                        break;
                    case "RichItemIcon":
                    case "CommonFightWidget":
                    case "PlayerHeadWidget":
                        strBind.Append($"\t\t\t{varName} = CreateWidgetByType<{componentName}>(\"{varPath}\");\n");
                        break;
                    case "RedNoteBehaviour":
                    case "TextButtonItem":
                    case "SwitchTabItem":
                    case "UIActorWidget":
                    case "UIEffectWidget":
                    case "UISpineWidget":
                        strBind.Append($"\t\t\t{varName} = CreateWidget<{componentName}>(\"{varPath}\");\n");
                        break;
                    case "ActorNameBinderText":
                        strBind.Append($"\t\t\t{varName} = FindTextBinder(\"{varPath}\");\n");
                        break;
                    case "ActorNameBinderEffect":
                        strBind.Append($"\t\t\t{varName} = FindEffectBinder(\"{varPath}\");\n");
                        break;
                    default:
                        strBind.Append($"\t\t\t{varName} = FindChildComponent<{componentName}>(\"{varPath}\");\n");
                        break;
                }

                if (componentName == "Button")
                {
                    string varFuncName = GetBtnFuncName(varName);
                    if (isUniTask)
                    {
                        strOnCreate.Append($"\t\t\t{varName}.onClick.AddListener(UniTask.UnityAction({varFuncName}));\n");
                        strCallback.Append($"\t\tprivate async UniTaskVoid {varFuncName}()\n");
                        strCallback.Append("\t\t{\n await UniTask.Yield();\n\t\t}\n");
                    }
                    else
                    {
                        strOnCreate.Append($"\t\t\t{varName}.onClick.AddListener({varFuncName});\n");
                        strCallback.Append($"\t\tprivate void {varFuncName}()\n");
                        strCallback.Append("\t\t{\n\t\t}\n");
                    }
                }
                else if (componentName == "Toggle")
                {
                    string varFuncName = GetToggleFuncName(varName);
                    strOnCreate.Append($"\t\t\t{varName}.onValueChanged.AddListener({varFuncName});\n");
                    strCallback.Append($"\t\tprivate void {varFuncName}(bool isOn)\n");
                    strCallback.Append("\t\t{\n\t\t}\n");
                }
                else if (componentName == "Slider")
                {
                    string varFuncName = GetSliderFuncName(varName);
                    strOnCreate.Append($"\t\t\t{varName}.onValueChanged.AddListener({varFuncName});\n");
                    strCallback.Append($"\t\tprivate void {varFuncName}(float value)\n");
                    strCallback.Append("\t\t{\n\t\t}\n");
                }
            }
        }

        public class GeneratorHelper : EditorWindow
        {
            [MenuItem("GameObject/ScriptGenerator/About", priority = 49)]
            public static void About()
            {
                GeneratorHelper welcomeWindow = (GeneratorHelper)EditorWindow.GetWindow(typeof(GeneratorHelper), false, "About");
            }

            public void Awake()
            {
                minSize = new Vector2(400, 600);
            }

            protected void OnGUI()
            {
                GUILayout.BeginVertical();
                foreach (var item in SettingsUtils.GetScriptGenerateRule())
                {
                    GUILayout.Label(item.uiElementRegex + "：\t" + item.componentName);
                }

                GUILayout.EndVertical();
            }
        }

        public class SwitchGroupGenerator
        {
            private const string Condition = "m_switchGroup";

            public static readonly SwitchGroupGenerator Instance = new SwitchGroupGenerator();

            public string Process(Transform root)
            {
                var sbd = new StringBuilder();
                var list = new List<Transform>();
                Collect(root, list);
                foreach (var node in list)
                {
                    sbd.AppendLine(Process(root, node)).AppendLine();
                }

                return sbd.ToString();
            }

            public void Collect(Transform node, List<Transform> nodeList)
            {
                if (node.name.StartsWith(Condition))
                {
                    nodeList.Add(node);
                    return;
                }

                var childCnt = node.childCount;
                for (var i = 0; i < childCnt; i++)
                {
                    var child = node.GetChild(i);
                    Collect(child, nodeList);
                }
            }

            private string Process(Transform root, Transform groupTf)
            {
                var parentPath = GetPath(root, groupTf);
                var name = groupTf.name;
                var sbd = new StringBuilder(@"
var _namePath = ""#parentPath"";
var _nameTf = FindChild(_namePath);
var childCnt = _nameTf.childCount;
SwitchTabItem[] _name;
_name = new SwitchTabItem[childCnt];
for (var i = 0; i < childCnt; i++)
{
    var child = _nameTf.GetChild(i);
    _name[i] = CreateWidget<SwitchTabItem>(_namePath + ""/"" + child.name);
}");
                sbd.Replace("_name", name);
                sbd.Replace("#parentPath", parentPath);
                return sbd.ToString();
            }

            public string GetPath(Transform root, Transform childTf)
            {
                if (childTf == null)
                {
                    return string.Empty;
                }

                if (childTf == root)
                {
                    return childTf.name;
                }

                var parentPath = GetPath(root, childTf.parent);
                if (parentPath == string.Empty)
                {
                    return childTf.name;
                }

                return parentPath + "/" + childTf.name;
            }
        }*/
    }
