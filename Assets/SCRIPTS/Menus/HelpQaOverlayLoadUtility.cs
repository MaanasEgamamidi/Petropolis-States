using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared suspend/restore for <see cref="UnityEngine.EventSystems.EventSystem"/> and
/// <see cref="AudioListener"/> when additively loading the Help Q&amp;A scene on top of an
/// already-loaded menu or world, so the new scene’s singletons are the active ones and
/// the prior scene’s can be restored on unload.
/// </summary>
public static class HelpQaOverlayLoadUtility
{
    static readonly List<EventSystem> SuspendedEventSystems = new();
    static readonly List<AudioListener> SuspendedAudioListeners = new();

    public static void SuspendEventSystemsAndAudioListeners()
    {
        SuspendedEventSystems.Clear();
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (es == null || !es.enabled)
                continue;
            SuspendedEventSystems.Add(es);
            es.enabled = false;
        }

        SuspendedAudioListeners.Clear();
        foreach (var al in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (al == null || !al.enabled)
                continue;
            SuspendedAudioListeners.Add(al);
            al.enabled = false;
        }
    }

    public static void RestoreSuspendedSingletons()
    {
        foreach (var es in SuspendedEventSystems)
        {
            if (es != null)
                es.enabled = true;
        }

        SuspendedEventSystems.Clear();

        foreach (var al in SuspendedAudioListeners)
        {
            if (al != null)
                al.enabled = true;
        }

        SuspendedAudioListeners.Clear();
    }

    public static void EnsureAtLeastOneActiveEventSystem()
    {
        if (HasAnyEnabled<EventSystem>())
            return;
        foreach (var es in SuspendedEventSystems)
        {
            if (es == null)
                continue;
            es.enabled = true;
            return;
        }
    }

    public static void EnsureAtLeastOneActiveAudioListener()
    {
        if (HasAnyEnabled<AudioListener>())
            return;
        foreach (var al in SuspendedAudioListeners)
        {
            if (al == null)
                continue;
            al.enabled = true;
            return;
        }
    }

    static bool HasAnyEnabled<T>() where T : Behaviour
    {
        foreach (var comp in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (comp != null && comp.enabled)
                return true;
        }

        return false;
    }
}
