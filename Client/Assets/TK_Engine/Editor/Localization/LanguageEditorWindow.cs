// LanguageEditorWindow.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TK_Engine;
using UnityEditor;
using UnityEngine;

namespace TK_Engine.Editor
{
    public class LanguageEditorWindow : EditorWindow
    {
        private MultiLanguageContainer languageData = new MultiLanguageContainer();
        private Dictionary<Language, TextMode> languages = new Dictionary<Language, TextMode>();
        
        private Vector2 scrollPosition = Vector2.zero;
        private bool[] languageFoldouts;
        private bool showAllLanguages = true;
        private Language selectedLanguage = Language.English;
        private string searchText = "";
        private List<string> filteredFields = new List<string>();
        private List<FieldInfo> textModeFields = new List<FieldInfo>();
        
        // 颜色定义
        private readonly Color headerColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        private readonly Color evenRowColor = new Color(0.9f, 0.9f, 0.9f, 0.2f);
        private readonly Color oddRowColor = new Color(0.95f, 0.95f, 0.95f, 0.1f);
        
        public List<Language> CurLanguages => new List<Language>()
        {
            Language.ChineseSimplified,
            Language.ChineseTraditional,
            Language.English,
            Language.French,
            Language.German,
            Language.Spanish,
            Language.Russian,
            Language.Japanese,
            Language.Korean,
            Language.PortugueseBrazil,
            Language.Indonesian,
            Language.Vietnamese,
        };
        
        [MenuItem("Tools/GameMain/多语言编辑器")]
        public static void ShowWindow()
        {
            var window = GetWindow<LanguageEditorWindow>("热更加载多语言编辑器");
            window.minSize = new Vector2(800, 600);
            window.LoadTextModeFields();
        }
        
        private void OnEnable()
        {
            LoadTextModeFields();
            InitializeLanguageData();
        }
        
        private void LoadTextModeFields()
        {
            textModeFields.Clear();
            var fields = typeof(TextMode).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                textModeFields.Add(field);
            }
            
            // 按字段名排序
            textModeFields.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }
        
        private void InitializeLanguageData()
        {
            languages.Clear();
            
            // 初始化所有语言
            foreach (Language lang in CurLanguages)
            {
                if (!languages.ContainsKey(lang))
                {
                    languages[lang] = new TextMode();
                }
            }
            
            languageFoldouts = new bool[Enum.GetValues(typeof(Language)).Length];
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.Space(10);
            
            if (showAllLanguages)
            {
                DrawAllLanguagesView();
            }
            else
            {
                DrawSingleLanguageView();
            }
            
            EditorGUILayout.Space(10);
            DrawExportSection();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // 导入按钮
                if (GUILayout.Button("导入JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ImportFromJson();
                }
                
                // 导出按钮
                if (GUILayout.Button("导出JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ExportToJson();
                }
                
                GUILayout.FlexibleSpace();
                
                // 搜索框
                EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
                searchText = EditorGUILayout.TextField(searchText, GUILayout.Width(200));
                
                GUILayout.FlexibleSpace();
                
                // 视图切换
                EditorGUILayout.LabelField("视图模式:", GUILayout.Width(60));
                showAllLanguages = GUILayout.Toggle(showAllLanguages, "所有语言", EditorStyles.toolbarButton, GUILayout.Width(80));
                showAllLanguages = !GUILayout.Toggle(!showAllLanguages, "单语言编辑", EditorStyles.toolbarButton, GUILayout.Width(80));
                
                // 语言选择下拉框（单语言模式）
                if (!showAllLanguages)
                {
                    selectedLanguage = (Language)EditorGUILayout.EnumPopup(selectedLanguage, GUILayout.Width(100));
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 应用搜索过滤
            if (!string.IsNullOrEmpty(searchText))
            {
                UpdateFilteredFields();
            }
        }
        
        private void UpdateFilteredFields()
        {
            filteredFields.Clear();
            foreach (var field in textModeFields)
            {
                if (field.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredFields.Add(field.Name);
                }
            }
        }
        
        private void DrawAllLanguagesView()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                int languageIndex = 0;
                foreach (Language lang in CurLanguages)
                {
                    if (!languages.ContainsKey(lang))
                    {
                        languages[lang] = new TextMode();
                    }
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        // 语言标题
                        EditorGUILayout.BeginHorizontal();
                        {
                            string foldoutLabel = $"{GetLanguageDisplayName(lang)} ({lang})";
                            languageFoldouts[languageIndex] = EditorGUILayout.Foldout(
                                languageFoldouts[languageIndex], 
                                foldoutLabel, 
                                true, 
                                EditorStyles.foldoutHeader
                            );
                            
                            GUILayout.FlexibleSpace();
                            
                            // 复制按钮
                            if (GUILayout.Button("复制到其他语言", GUILayout.Width(120)))
                            {
                                ShowCopyToOtherLanguagesMenu(lang);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        if (languageFoldouts[languageIndex])
                        {
                            EditorGUI.indentLevel++;
                            DrawTextModeFields(languages[lang], lang);
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                    
                    languageIndex++;
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSingleLanguageView()
        {
            if (!languages.ContainsKey(selectedLanguage))
            {
                languages[selectedLanguage] = new TextMode();
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"当前编辑: {GetLanguageDisplayName(selectedLanguage)}", 
                    EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                {
                    DrawTextModeFields(languages[selectedLanguage], selectedLanguage);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTextModeFields(TextMode textMode, Language language)
        {
            int fieldIndex = 0;
            
            foreach (var field in textModeFields)
            {
                // 应用搜索过滤
                if (!string.IsNullOrEmpty(searchText) && !filteredFields.Contains(field.Name))
                {
                    continue;
                }
                
                // 交替行背景色
                Rect bgRect = EditorGUILayout.BeginHorizontal();
                if (Event.current.type == EventType.Repaint)
                {
                    Color bgColor = fieldIndex % 2 == 0 ? evenRowColor : oddRowColor;
                    EditorGUI.DrawRect(bgRect, bgColor);
                }
                
                // 字段名标签
                string displayName = GetDisplayName(field.Name);
                EditorGUILayout.LabelField(displayName, GUILayout.Width(200));
                
                // 文本区域
                string currentValue = (string)field.GetValue(textMode);
                string newValue = EditorGUILayout.TextArea(currentValue, 
                    GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
                
                if (newValue != currentValue)
                {
                    field.SetValue(textMode, newValue);
                    EditorUtility.SetDirty(this);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 参数提示
                if (newValue.Contains("{0}"))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(210);
                    EditorGUILayout.HelpBox("此文本包含格式化参数 {0}", MessageType.Info);
                    EditorGUILayout.EndHorizontal();
                }
                
                fieldIndex++;
                EditorGUILayout.Space(2);
            }
        }
        
        private void ShowCopyToOtherLanguagesMenu(Language sourceLanguage)
        {
            GenericMenu menu = new GenericMenu();
            
            foreach (Language targetLang in Enum.GetValues(typeof(Language)))
            {
                if (targetLang != sourceLanguage)
                {
                    menu.AddItem(
                        new GUIContent($"复制到 {GetLanguageDisplayName(targetLang)}"),
                        false,
                        () => CopyLanguageData(sourceLanguage, targetLang)
                    );
                }
            }
            
            menu.ShowAsContext();
        }
        
        private void CopyLanguageData(Language source, Language target)
        {
            if (!languages.ContainsKey(source) || !languages.ContainsKey(target))
                return;
            
            var sourceTextMode = languages[source];
            var targetTextMode = languages[target];
            
            foreach (var field in textModeFields)
            {
                var value = field.GetValue(sourceTextMode);
                field.SetValue(targetTextMode, value);
            }
            
            Debug.Log($"已将 {GetLanguageDisplayName(source)} 的文本复制到 {GetLanguageDisplayName(target)}");
        }
        
        private void DrawExportSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("导出设置", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("导出所有语言", GUILayout.Height(30)))
                    {
                        ExportToJson();
                    }
                    
                    if (GUILayout.Button("导出选中语言", GUILayout.Width(120), GUILayout.Height(30)))
                    {
                        ExportSelectedLanguage();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox(
                    "导出文件将包含所有语言的文本数据，可以在运行时动态加载。",
                    MessageType.Info
                );
            }
            EditorGUILayout.EndVertical();
        }
        
        private void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("选择多语言JSON文件", 
                Application.dataPath, "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var container = Utility.Json.ToObject<MultiLanguageContainer>(json);
                    
                    if (container != null)
                    {
                        languages.Clear();
                        foreach (var pair in container.languagePairs)
                        {
                            languages[pair.language] = pair.textMode;
                        }
                        
                        Debug.Log($"成功导入 {container.languagePairs.Count} 种语言的文本数据");
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("导入失败", $"导入JSON文件时发生错误:\n{e.Message}", "确定");
                }
            }
        }
        
        private void ExportToJson()
        {
            string path = EditorUtility.SaveFilePanel("导出多语言JSON文件",
                Application.dataPath,
                "LoadTipsLanguage.json",
                "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var container = new MultiLanguageContainer();
                    container.FromDictionary(languages);
                    
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(container);
                    File.WriteAllText(path, json);
                    
                    Debug.Log($"已成功导出到: {path}");
                    
                    // 刷新Unity项目（如果保存在Assets目录下）
                    if (path.StartsWith(Application.dataPath))
                    {
                        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("导出成功", 
                            $"多语言数据已导出到:\n{relativePath}", "确定");
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("导出失败", $"导出JSON文件时发生错误:\n{e.Message}", "确定");
                }
            }
        }
        
        private void ExportSelectedLanguage()
        {
            if (!showAllLanguages)
            {
                var singleLangData = new Dictionary<Language, TextMode>
                {
                    [selectedLanguage] = languages[selectedLanguage]
                };
                
                string path = EditorUtility.SaveFilePanel($"导出 {selectedLanguage} 语言文件",
                    Application.dataPath,
                    $"GameLanguage_{selectedLanguage}.json",
                    "json");
                
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var container = new MultiLanguageContainer();
                        container.FromDictionary(singleLangData);
                        
                        string json = JsonUtility.ToJson(container, true);
                        File.WriteAllText(path, json);
                        
                        Debug.Log($"已成功导出 {selectedLanguage} 语言到: {path}");
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("导出失败", $"导出JSON文件时发生错误:\n{e.Message}", "确定");
                    }
                }
            }
        }
        
        private string GetLanguageDisplayName(Language language)
        {
            switch (language)
            {
                case Language.ChineseSimplified: return "简体中文";
                case Language.ChineseTraditional: return "繁體中文";
                case Language.English: return "英文(美国)";
                case Language.French: return "法语";
                case Language.German: return "德语";
                case Language.Spanish: return "西班牙语";
                case Language.Russian: return "俄语";
                case Language.Japanese: return "日语";
                case Language.Korean: return "韩语";
                case Language.PortugueseBrazil: return "葡萄牙语（巴西）";
                case Language.PortuguesePortugal: return "葡萄牙语";
                case Language.Indonesian:return "印度尼西亚语";
                case Language.Vietnamese: return "越南语";
                default: return language.ToString();
            }
        }
        
        private string GetDisplayName(string fieldName)
        {
            // 移除 "Label_" 前缀并添加空格
            string displayName = fieldName.Replace("Label_", "").Replace("_", " ");
            
            // 将首字母大写
            if (!string.IsNullOrEmpty(displayName))
            {
                displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            }
            
            return displayName;
        }
    }
}
