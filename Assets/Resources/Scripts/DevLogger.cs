using System.Diagnostics;

public static class DevLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string callerName = System.IO.Path.GetFileNameWithoutExtension(caller);
        UnityEngine.Debug.Log($"[{callerName}]: {message}");
#endif
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string callerName = System.IO.Path.GetFileNameWithoutExtension(caller);
        UnityEngine.Debug.LogWarning($"[{callerName}]: {message}");
#endif
    }

    public static void LogError(string message, [System.Runtime.CompilerServices.CallerFilePath] string caller = "")
    {
        string callerName = System.IO.Path.GetFileNameWithoutExtension(caller);
        UnityEngine.Debug.LogError($"[{callerName}]: {message}");
    }
}