using System;
using UnityEngine;

namespace UCompile
{
    /// <summary>
    /// Console logger
    /// 
    /// TODO: this primitive logger is mostly a placeholder right now
    /// Need to make it serializable to pass it to remote script engine in remote app domain
    /// </summary>
    //[Serializable]
    public static class Logger
    {
        public static void Log(string text)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(text);
#endif
        }
    }
}