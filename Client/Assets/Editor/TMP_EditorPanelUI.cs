
using Sirenix.OdinInspector;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TextMeshProUGUI),true),CanEditMultipleObjects]
    public class TMP_EditorPanelUI : TMP_BaseEditorPanel
    {
        protected SerializedProperty m_IsraycastTarget;

        private static readonly GUIContent k_RaycastTarget = new GUIContent("Raycast Target",
            "Determines if the TextMeshProUGUI can be hit by raycasts.");
        
        private bool showMaterialTools = false;
        private Color searchColor = Color.white;
        private float colorTolerance = 0.01f;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_IsraycastTarget = serializedObject.FindProperty("m_RaycastTarget");
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // 添加分隔线
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Outline Tools", EditorStyles.boldLabel);
            showMaterialTools = EditorGUILayout.Foldout(showMaterialTools, "Outline Tools", true);
            if (showMaterialTools)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                // 颜色选择
                searchColor = EditorGUILayout.ColorField("Target Color", searchColor);
                if (GUILayout.Button("Check Outline"))
                {
                    CheckOutline();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void CheckOutline()
        {
            TMP_FontAsset fontAsset = m_FontAssetProp.objectReferenceValue as TMP_FontAsset;
            m_HavePropertiesChanged = false;
            bool isFound = false;
            int index = 0;
            foreach (var mat in m_MaterialPresets)
            {
                Color outlineColor = mat.GetColor("_OutlineColor");
                if (ColorDistance(outlineColor, searchColor) < colorTolerance)
                {
                    Debug.Log("描边材质名称: " + mat.name);
                    isFound = true;
                }
                index ++;
            }

            if (!isFound)
            {
                Debug.Log("没有找到匹配的描边材质，请自己新建一个");
            }
        }
        private float ColorDistance(Color c1, Color c2)
        {
            // 将颜色转换为LAB颜色空间
            ColorLab lab1 = RGBToLab(c1);
            ColorLab lab2 = RGBToLab(c2);
    
            // 计算Delta E
            float deltaL = lab1.L - lab2.L;
            float deltaA = lab1.A - lab2.A;
            float deltaB = lab1.B - lab2.B;
    
            return Mathf.Sqrt(deltaL*deltaL + deltaA*deltaA + deltaB*deltaB);
        }
        private ColorLab RGBToLab(Color rgb)
        {
            // 先转换为XYZ颜色空间
            float r = rgb.r > 0.04045f ? Mathf.Pow((rgb.r + 0.055f) / 1.055f, 2.4f) : rgb.r / 12.92f;
            float g = rgb.g > 0.04045f ? Mathf.Pow((rgb.g + 0.055f) / 1.055f, 2.4f) : rgb.g / 12.92f;
            float b = rgb.b > 0.04045f ? Mathf.Pow((rgb.b + 0.055f) / 1.055f, 2.4f) : rgb.b / 12.92f;

            float x = r * 0.4124f + g * 0.3576f + b * 0.1805f;
            float y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
            float z = r * 0.0193f + g * 0.1192f + b * 0.9505f;

            // D65白点
            x /= 0.95047f;
            z /= 1.08883f;

            // XYZ转LAB
            x = x > 0.008856f ? Mathf.Pow(x, 1.0f/3.0f) : (7.787f * x) + (16.0f/116.0f);
            y = y > 0.008856f ? Mathf.Pow(y, 1.0f/3.0f) : (7.787f * y) + (16.0f/116.0f);
            z = z > 0.008856f ? Mathf.Pow(z, 1.0f/3.0f) : (7.787f * z) + (16.0f/116.0f);

            return new ColorLab
            {
                L = Mathf.Max(0, 116 * y - 16),
                A = 500 * (x - y),
                B = 200 * (y - z)
            };
        }
        protected override void DrawExtraSettings()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 24);
            if (GUI.Button(rect, new GUIContent("<b>Extra Settings</b>"), TMP_UIStyleManager.sectionHeader))
                Foldout.extraSettings = !Foldout.extraSettings;
            GUI.Label(rect, (Foldout.extraSettings ? k_UiStateLabel[0] : k_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);
            if (Foldout.extraSettings)
            {
                //EditorGUI.indentLevel += 1;
 
                DrawMargins();
 
                DrawGeometrySorting();
 
                DrawIsTextObjectScaleStatic();
 
                DrawRichText();

                DrawRaycastTarget();
                
                DrawParsing();
 
                DrawSpriteAsset();
 
                DrawStyleSheet();
 
                DrawKerning();
 
                DrawPadding();
                
               
                //EditorGUI.indentLevel -= 1;
         
            }
          
        }

        protected void DrawRaycastTarget()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_IsraycastTarget, k_RaycastTarget);
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }
        }
        protected override bool IsMixSelectionTypes()
        {
            GameObject[] objects = Selection.gameObjects;
            if (objects.Length > 1)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].GetComponent<TextMeshProUGUI>() == null)
                        return true;
                }
            }
            return false;
        }

        protected override void OnUndoRedo()
        {
            int undoEventId = Undo.GetCurrentGroup();
            int lastUndoEventId = s_EventId;
 
            if (undoEventId != lastUndoEventId)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    //Debug.Log("Undo & Redo Performed detected in Editor Panel. Event ID:" + Undo.GetCurrentGroup());
                    TMPro_EventManager.ON_TEXTMESHPRO_UGUI_PROPERTY_CHANGED(true, targets[i] as TextMeshProUGUI);
                    s_EventId = undoEventId;
                }
            }
        }
    }
    // LAB颜色结构体
    struct ColorLab
    {
        public float L;
        public float A;
        public float B;
    }
}
