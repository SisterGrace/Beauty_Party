using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sky
{
    public class SkyReflectionUtils
    {
        /// <summary>
        /// 调用Java静态方法
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="className">完整Java类名</param>
        /// <param name="methodName">方法名</param>
        /// <param name="args">参数数组</param>
        public static T CallStaticMethod<T>(string className, string methodName)
        {
            using var cls = new AndroidJavaClass(className);
            return cls.CallStatic<T>(methodName);
        }

        public static void CallStaticMethod(string className, string methodName)
        {
            using var cls = new AndroidJavaClass(className);
            cls.CallStatic(methodName);
        }

        public static T CallStaticMethod<T>(string className, string methodName, object arg1)
        {
            using var cls = new AndroidJavaClass(className);
            return cls.CallStatic<T>(methodName, arg1);
        }

        public static void CallStaticMethod(string className, string methodName, object arg1)
        {
            using var cls = new AndroidJavaClass(className);
            cls.CallStatic(methodName, arg1);
        }

        public static T CallStaticMethod<T>(string className, string methodName, object arg1, object arg2)
        {
            using var cls = new AndroidJavaClass(className);
            return cls.CallStatic<T>(methodName, arg1, arg2);
        }

        public static void CallStaticMethod(string className, string methodName, object arg1, object arg2)
        {
            using var cls = new AndroidJavaClass(className);
            cls.CallStatic(methodName, arg1, arg2);
        }

        public static T CallStaticMethod<T>(string className, string methodName, object arg1, object arg2, object arg3)
        {
            using var cls = new AndroidJavaClass(className);
            return cls.CallStatic<T>(methodName, arg1, arg2, arg3);
        }

        public static void CallStaticMethod(string className, string methodName, object arg1, object arg2, object arg3)
        {
            using var cls = new AndroidJavaClass(className);
            cls.CallStatic(methodName, arg1, arg2, arg3);
        }

        /// <summary>
        /// 创建Java对象实例
        /// </summary>
        /// <param name="className">完整类名</param>
        /// <param name="args">构造函数参数</param>
        public static AndroidJavaObject CreateInstance(string className)
        {
            return new AndroidJavaObject(className);
        }

        public static AndroidJavaObject CreateInstance(string className, object arg1)
        {
            return new AndroidJavaObject(className, arg1);
        }

        public static AndroidJavaObject CreateInstance(string className, object arg1, object arg2)
        {
            return new AndroidJavaObject(className, arg1, arg2);
        }

        public static AndroidJavaObject CreateInstance(string className, object arg1, object arg2, object arg3)
        {
            return new AndroidJavaObject(className, arg1, arg2, arg3);
        }

        /// <summary>
        /// 调用实例方法
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="instance">Java对象实例</param>
        /// <param name="methodName">方法名</param>
        public static T CallInstanceMethod<T>(AndroidJavaObject instance, string methodName)
        {
            return instance.Call<T>(methodName);
        }

        public static void CallInstanceMethod(AndroidJavaObject instance, string methodName)
        {
            instance.Call(methodName);
        }

        public static T CallInstanceMethod<T>(AndroidJavaObject instance, string methodName, object arg1)
        {
            return instance.Call<T>(methodName, arg1);
        }

        public static void CallInstanceMethod(AndroidJavaObject instance, string methodName, object arg1)
        {
            instance.Call(methodName, arg1);
        }

        public static T CallInstanceMethod<T>(AndroidJavaObject instance, string methodName, object arg1, object arg2)
        {
            return instance.Call<T>(methodName, arg1, arg2);
        }

        public static void CallInstanceMethod(AndroidJavaObject instance, string methodName, object arg1, object arg2)
        {
            instance.Call(methodName, arg1, arg2);
        }

        public static T CallInstanceMethod<T>(AndroidJavaObject instance, string methodName, object arg1, object arg2, object arg3)
        {
            return instance.Call<T>(methodName, arg1, arg2, arg3);
        }

        public static void CallInstanceMethod(AndroidJavaObject instance, string methodName, object arg1, object arg2, object arg3)
        {
            instance.Call(methodName, arg1, arg2, arg3);
        }

        public static T CallInstanceMethod<T>(AndroidJavaObject instance, string methodName, object arg1, object arg2, object arg3, object arg4)
        {
            return instance.Call<T>(methodName, arg1, arg2, arg3, arg4);
        }

        public static void CallInstanceMethod(AndroidJavaObject instance, string methodName, object arg1, object arg2, object arg3, object arg4)
        {
            instance.Call(methodName, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        public static T GetField<T>(AndroidJavaObject instance, string fieldName)
        {
            return instance.Get<T>(fieldName);
        }

        /// <summary>
        /// 设置字段值
        /// </summary>
        public static void SetField(AndroidJavaObject instance, string fieldName, object value)
        {
            instance.Set(fieldName, value);
        }


        #region Activity/Application 相关

        /// <summary>
        /// 获取当前Unity的Activity上下文
        /// </summary>
        public static AndroidJavaObject GetCurrentActivity()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            catch (Exception e)
            {
                Debug.LogError("获取Activity失败: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取Application上下文
        /// </summary>
        public static AndroidJavaObject GetApplicationContext()
        {
            try
            {
                AndroidJavaObject activity = GetCurrentActivity();
                return activity?.Call<AndroidJavaObject>("getApplicationContext");
            }
            catch (Exception e)
            {
                Debug.LogError("获取ApplicationContext失败: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取PackageName
        /// </summary>
        public static string GetPackageName()
        {
            try
            {
                AndroidJavaObject context = GetApplicationContext();
                return context?.Call<string>("getPackageName");
            }
            catch (Exception e)
            {
                Debug.LogError("获取包名失败: " + e.Message);
                return null;
            }
        }

        #endregion

        #region UI线程操作

        /// <summary>
        /// 在Android主线程执行代码（自动处理AndroidJavaProxy）
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="delayMillis">延迟时间（毫秒，0表示立即）</param>
        public static void RunOnUiThread(Action action, long delayMillis = 0)
        {
            try
            {
                AndroidJavaObject activity = GetCurrentActivity();
                if (activity == null)
                {
                    Debug.LogWarning("无法获取Activity，无法切换至UI线程");
                    return;
                }

                // 创建Runnable代理
                var runnableProxy = new AndroidRunnableProxy(action);

                if (delayMillis > 0)
                {
                    // 使用Handler实现延时
                    AndroidJavaObject handler = new AndroidJavaObject("android.os.Handler");
                    handler.Call("postDelayed", runnableProxy, delayMillis);
                }
                else
                {
                    // 立即执行
                    activity.Call("runOnUiThread", runnableProxy);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("UI线程操作失败: " + e.Message);
            }
        }

        /// <summary>
        /// 自定义Runnable代理类
        /// </summary>
        private class AndroidRunnableProxy : AndroidJavaProxy
        {
            private readonly Action _action;

            public AndroidRunnableProxy(Action action) : base("java.lang.Runnable")
            {
                _action = action;
            }

            // 实现Runnable接口的run方法
            [Preserve]
            public void run()
            {
                try
                {
                    _action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("UI线程操作执行异常: " + e.Message);
                }
            }
        }

        #endregion
    }

    // 辅助类：封装Android.os相关操作
    public static class AndroidOs
    {
        public static class Process
        {
            public static int MyTid()
            {
                using var processClass = new AndroidJavaClass("android.os.Process");
                return processClass.CallStatic<int>("myTid");
            }
        }
    }
}