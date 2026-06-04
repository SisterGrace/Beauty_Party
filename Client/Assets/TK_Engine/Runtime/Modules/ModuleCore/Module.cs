using UnityEngine;

namespace TK_Engine
{
    /// <summary>
    /// 游戏框架模块抽象类。
    /// </summary>
    public abstract class Module : MonoBehaviour
    {
        /// <summary>
        /// 游戏框架模块初始化。
        /// </summary>
        protected virtual void Awake()
        {
            ModuleSystem.RegisterModule(this);
        }
    }
}