using UnityEngine;
using UnityEditor;

namespace itsmakingthings_daynightcycle
{
    [CustomEditor(typeof(DayNightCycle))]
    public class DayNightCycleEditor : Editor
    {
        private const string PUBLISHER_URL = "https://assetstore.unity.com/publishers/116651";
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DayNightCycle cycle = (DayNightCycle)target;

            GUILayout.Space(10);
            GUILayout.Label("🔆 Set Time of Day", EditorStyles.boldLabel);

            if (GUILayout.Button("🌅 Set to Daybreak"))
            {
                cycle.SetToDaybreak();
            }

            if (GUILayout.Button("☀ Set to Midday"))
            {
                cycle.SetToMidday();
            }

            if (GUILayout.Button("🌇 Set to Sunset"))
            {
                cycle.SetToSunset();
            }

            if (GUILayout.Button("🌙 Set to Night"))
            {
                cycle.SetToNight();
            }

            // Draw a box to frame the promotion
            GUILayout.Space(20);
            
            // Draw a box to frame the promotion
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            // Centered Bold Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("✨ Upgrade to the Advanced System ✨", titleStyle);

            // Description Text - UPDATED to highlight specific features
            var bodyStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Get full control over your world:\n\n• Procedural Sun Disks & Moons\n• Dynamic Skybox & Cubemap Blending\n• Complete Seasonal & Calendar System\n• 21 Environmental Presets Included", bodyStyle);

            GUILayout.Space(10);

            // Green "Call To Action" Button
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Light Green
            
            if (GUILayout.Button("View Publisher Page"))
            {
                Application.OpenURL(PUBLISHER_URL);
            }
            
            GUI.backgroundColor = originalColor; // Reset color so we don't tint the rest of Unity
            
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
    }
}