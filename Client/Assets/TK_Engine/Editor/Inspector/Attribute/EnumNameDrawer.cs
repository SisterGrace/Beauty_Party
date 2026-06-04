using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TK_Engine.Editor.Inspector.Attribute
{
    /// <summary>
/// #看不懂 枚举变量名扩展(可以在界面上显示代码里给变量打的注释) 
/// </summary>
[CustomPropertyDrawer(typeof(EnumNameAttribute))]
public class EnumNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //替换属性名称
        var attr = (EnumNameAttribute)attribute;
        //使用枚举型注解标记的类型并不是枚举 
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            Debug.LogError($"非枚举型数据使用了枚举特性:{property.displayName}");
            return;
        }

        //如果字段名为Element 任意数字 我们认为当前字段是list或array中的元素
        var isElement = Regex.IsMatch(property.displayName, "Element \\d+");
        //非list或array的情况 把label显示替换为我们的中文注解
        if (!isElement)
        {
            label.text = attr.name;
        }

        //重新绘制
        DrawEnum(position, property, label);
    }

    /// <summary>
    /// 绘制枚举类型
    /// </summary>
    /// <param name="position"></param>
    /// <param name="property"></param>
    /// <param name="label"></param>
    private void DrawEnum(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginChangeCheck();
        // 当前字段的类型 枚举/枚举数组/枚举列表
        var type = fieldInfo.FieldType;
        type = GetEnumType(type);
        //无法解析的未知情况 不处理了
        if (type == null)
        {
            return;
        }

        // 获取枚举所对应的名称
        var names = property.enumNames;
        var values = new string[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            //获取当前枚举类型中的定义字段
            var info = type.GetField(names[i]);
            //根据自定义的枚举特性 找到中文标注
            var atts = (EnumNameAttribute[])info.GetCustomAttributes(typeof(EnumNameAttribute), false);
            if (atts.Length <= 0)
            {
                continue;
            }

            values[i] = atts.Length == 0 ? names[i] : atts[0].name;
        }

        // 重绘GUI
        var index = EditorGUI.Popup(position, label.text, property.enumValueIndex, values);
        if (EditorGUI.EndChangeCheck() && index != -1) property.enumValueIndex = index;
    }

    /// <summary>
    /// 获取当前类型中的枚举类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static System.Type GetEnumType(System.Type type)
    {
        if (type == null)
        {
            return null;
        }

        //非泛型的情况
        if (!type.IsGenericType)
        {
            //数组的情况
            return type.IsArray ? type.GetElementType() : type;
        }

        //泛型的情况 取最后一个泛型类型作为枚举 比如Dict<string,Enum> 或List<Enum>
        var getGenericArguments = type.GetGenericArguments();
        return getGenericArguments[getGenericArguments.Length - 1];
    }
}
}
