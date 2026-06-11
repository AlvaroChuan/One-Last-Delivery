using System.Diagnostics;

public static class DevLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, string filter = "", [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string callerName = System.IO.Path.GetFileNameWithoutExtension(caller);
        if (string.IsNullOrEmpty(filter))
        {
            UnityEngine.Debug.Log($"[{callerName}] {message}");
        }
        else
        {
            UnityEngine.Debug.Log($"[{filter}] [{callerName}] {message}");
        }
#endif
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, string filter = "", [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string callerName = System.IO.Path.GetFileNameWithoutExtension(caller);
        if (string.IsNullOrEmpty(filter))
        {
            UnityEngine.Debug.LogWarning($"[{callerName}] {message}");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[{filter}] [{callerName}] {message}");
        }
#endif
    }

    public static void LogError(string message, string filter = "", [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
        string callerName = System.IO.Path.GetFileNameWithoutExtension(caller);
        if (string.IsNullOrEmpty(filter))
        {
            UnityEngine.Debug.LogError($"[{callerName}] {message}");
        }
        else
        {
            UnityEngine.Debug.LogError($"[{filter}] [{callerName}] {message}");
        }
    }
}