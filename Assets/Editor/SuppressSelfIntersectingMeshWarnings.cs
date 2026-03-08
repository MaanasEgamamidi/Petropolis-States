using UnityEngine;
using UnityEditor;

/// <summary>
/// Suppresses Unity's "self-intersecting polygon" warnings in the console.
/// Unity automatically discards these polygons during import, so the meshes still work correctly.
/// Enable/disable via Edit > Suppress Self-Intersecting Mesh Warnings.
/// </summary>
[InitializeOnLoad]
internal static class SuppressSelfIntersectingMeshWarnings
{
    private const string PrefsKey = "Petropolis_SuppressSelfIntersectingWarnings";
    private const string FilterPhrase = "self-intersecting";

    private static ILogHandler s_OriginalHandler;
    private static bool s_Installed;

    static SuppressSelfIntersectingMeshWarnings()
    {
        if (IsEnabled)
            InstallFilter();
    }

    public static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefsKey, true);
        set
        {
            EditorPrefs.SetBool(PrefsKey, value);
            if (value && !s_Installed)
                InstallFilter();
            else if (!value && s_Installed)
                UninstallFilter();
        }
    }

    [MenuItem("Edit/Suppress Self-Intersecting Mesh Warnings", false, 301)]
    private static void Toggle()
    {
        IsEnabled = !IsEnabled;
    }

    [MenuItem("Edit/Suppress Self-Intersecting Mesh Warnings", true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked("Edit/Suppress Self-Intersecting Mesh Warnings", IsEnabled);
        return true;
    }

    private static void InstallFilter()
    {
        if (s_Installed) return;
        s_OriginalHandler = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = new FilteringLogHandler(s_OriginalHandler);
        s_Installed = true;
    }

    private static void UninstallFilter()
    {
        if (!s_Installed) return;
        Debug.unityLogger.logHandler = s_OriginalHandler;
        s_Installed = false;
    }

    private sealed class FilteringLogHandler : ILogHandler
    {
        private readonly ILogHandler _inner;

        public FilteringLogHandler(ILogHandler inner) => _inner = inner;

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            string message = args != null && args.Length > 0 ? string.Format(format, args) : format;
            if (logType == LogType.Warning && message.IndexOf(FilterPhrase, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            _inner.LogFormat(logType, context, format, args);
        }

        public void LogException(System.Exception exception, Object context)
        {
            _inner.LogException(exception, context);
        }
    }
}
