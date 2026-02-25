using UnityEngine;
using UnityEditor;

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Resolves instance IDs to Unity objects. Centralizes obsolete InstanceIDToObject
    /// so CS0618 warnings are suppressed in one place (Unity 6 recommends EntityIdToObject).
    /// </summary>
    public static class EditorObjectResolver
    {
        /// <summary>
        /// Resolves an instance ID to a Unity Object.
        /// </summary>
        public static Object InstanceIdToObject(int instanceId)
        {
#pragma warning disable CS0618 // InstanceIDToObject is obsolete in Unity 6; use EntityIdToObject when migrating to entity IDs
            return EditorUtility.InstanceIDToObject(instanceId);
#pragma warning restore CS0618
        }
    }
}
