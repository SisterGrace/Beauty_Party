using UnityEditor;
using UnityEngine;


namespace TK_Engine.Editor.Inspector.Attribute
{
    /// <summary>
    /// 变量名扩展(可以在界面上显示代码里给变量打的注释)
    /// </summary>
    [CustomPropertyDrawer(typeof(FieldNameAttribute))]
    public class FieldNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (FieldNameAttribute)attribute;
            var color = Color.white;
            //在这里重新绘制
            var cacheColor = GUI.color;
            GUI.color = color;
            EditorGUI.PropertyField(position, property, new GUIContent(attr.label), true);
            GUI.color = cacheColor;
        }
    }
}
