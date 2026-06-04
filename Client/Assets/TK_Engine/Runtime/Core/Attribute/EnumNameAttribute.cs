
using System;
using UnityEngine;

namespace TK_Engine
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumNameAttribute : PropertyAttribute
    {
        /// <summary> 枚举名称 </summary>
        public readonly string name;

        public EnumNameAttribute(string name)
        {
            this.name = name;
        }
    }
    
}


