using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Logic
{
    public class LogicDebug
    {
        public static void Log(object message)
        {
            Debug.Log(message);
        }
        public static void Log(object message, Object context)
        {
            Debug.Log(message, context);
        }
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }
        public static void LogError(object message, Object context)
        {
            Debug.LogError(message, context);
        }
        public static void LogException(System.Exception exception)
        {
            Debug.LogException(exception);
        }
        public static void LogException(System.Exception exception, Object context)
        {
            Debug.LogException(exception, context);
        }
        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }
        public static void LogWarning(object message, Object context)
        {
            Debug.LogWarning(message, context);
        }
    }
}
