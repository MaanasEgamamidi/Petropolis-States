using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace MCPForUnity.Editor.Resources.Editor
{
    /// <summary>
    /// Provides detailed information about the current editor selection.
    /// </summary>
    [McpForUnityResource("get_selection")]
    public static class Selection
    {
        public static object HandleCommand(JObject @params)
        {
            try
            {
#if UNITY_6000_0_OR_NEWER
                int activeId = UnityEditor.Selection.activeObject != null ? UnityEditor.Selection.activeObject.GetInstanceID() : 0;
#else
#pragma warning disable CS0618 // activeInstanceID obsolete in Unity 6; use activeEntityId
                int activeId = UnityEditor.Selection.activeInstanceID;
#pragma warning restore CS0618
#endif
                var selectionInfo = new
                {
                    activeObject = UnityEditor.Selection.activeObject?.name,
                    activeGameObject = UnityEditor.Selection.activeGameObject?.name,
                    activeTransform = UnityEditor.Selection.activeTransform?.name,
                    activeInstanceID = activeId,
                    count = UnityEditor.Selection.count,
                    objects = UnityEditor.Selection.objects
                        .Select(obj => new
                        {
                            name = obj?.name,
                            type = obj?.GetType().FullName,
                            instanceID = obj?.GetInstanceID()
                        })
                        .ToList(),
                    gameObjects = UnityEditor.Selection.gameObjects
                        .Select(go => new
                        {
                            name = go?.name,
                            instanceID = go?.GetInstanceID()
                        })
                        .ToList(),
                    assetGUIDs = UnityEditor.Selection.assetGUIDs
                };

                return new SuccessResponse("Retrieved current selection details.", selectionInfo);
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Error getting selection: {e.Message}");
            }
        }
    }
}
