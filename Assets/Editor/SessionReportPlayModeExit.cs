using UnityEditor;

/// <summary>
/// When stopping Play Mode, Unity often does not run <see cref="UnityEngine.Application.quitting"/> / OnApplicationQuit
/// and static or destroy order can skip <c>SessionTracker.OnDestroy</c>. Writing on <see cref="PlayModeStateChange.ExitingPlayMode"/>
/// is the reliable Editor path.
/// </summary>
[InitializeOnLoad]
internal static class SessionReportPlayModeExit
{
    static SessionReportPlayModeExit()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode) return;
        if (!GenerateSessionReports.IsEnabled) return;

        SessionTracker.WriteReportIfNeeded();
    }
}
