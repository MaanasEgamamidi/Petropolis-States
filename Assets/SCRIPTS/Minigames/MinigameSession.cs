using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Petropolis.Minigames
{
    /// <summary>
    /// Global, scene-independent manager for launching a minigame scene additively from the World
    /// scene, accumulating a dollar payout while the player plays, and returning to the exact same
    /// spot in the World when the minigame ends. Freezes all World root GameObjects while the
    /// minigame is active so cameras, audio, input, and scripts from the two scenes don't fight
    /// each other — the same pattern <see cref="AdditiveSceneHelper"/> uses for PetChoose.
    /// </summary>
    public static class MinigameSession
    {
        const string WorldSceneName = "World";

        static readonly List<GameObject> _frozenWorldRoots = new List<GameObject>();
        static Runner _runner;

        /// <summary>Name of the minigame scene currently loaded additively, or empty when idle.</summary>
        public static string ActiveMinigameSceneName { get; private set; } = string.Empty;

        /// <summary>Dollars earned so far during the active minigame (reset at session start).</summary>
        public static float PendingEarnings { get; private set; }

        /// <summary>True while a minigame is loaded and the World is frozen underneath it.</summary>
        public static bool IsActive => !string.IsNullOrEmpty(ActiveMinigameSceneName);

        /// <summary>Short human-readable label for the active minigame, used in the return notification.</summary>
        public static string ActiveMinigameLabel { get; private set; } = string.Empty;

        /// <summary>
        /// Additively loads <paramref name="sceneName"/>, freezes the World scene, and resets the
        /// earnings accumulator. No-op if a minigame is already active or we're not in the World.
        /// </summary>
        public static void StartSession(string sceneName, string friendlyLabel = "")
        {
            if (IsActive)
            {
                Debug.LogWarning($"[MinigameSession] Start ignored — '{ActiveMinigameSceneName}' already active.");
                return;
            }

            Scene world = SceneManager.GetSceneByName(WorldSceneName);
            if (!world.isLoaded)
            {
                Debug.LogWarning("[MinigameSession] Start ignored — World scene is not loaded.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[MinigameSession] Start ignored — empty scene name.");
                return;
            }

            ActiveMinigameSceneName = sceneName;
            ActiveMinigameLabel = string.IsNullOrWhiteSpace(friendlyLabel) ? sceneName : friendlyLabel;
            PendingEarnings = 0f;

            EnsureRunner();
            FreezeWorldBeforeLoad(world);
            _runner.StartCoroutine(LoadMinigameRoutine(sceneName));
        }

        /// <summary>Adds to the running dollar total for the active minigame. Ignored if no session is active.</summary>
        public static void AddPayout(float dollars)
        {
            if (!IsActive) return;
            if (dollars == 0f) return;
            PendingEarnings = Mathf.Max(0f, PendingEarnings + dollars);
        }

        /// <summary>
        /// Closes the active minigame, unloads its scene, un-freezes the World, and credits the
        /// accumulated payout to <see cref="GlobalData.PlayerMoney"/>. Safe to call multiple times;
        /// only the first call per session does work.
        /// </summary>
        public static void EndSession()
        {
            if (!IsActive)
                return;

            string sceneName = ActiveMinigameSceneName;
            string label = ActiveMinigameLabel;
            float earned = Mathf.Round(PendingEarnings);

            // Clear state immediately so re-entrant calls (e.g. from a second "game over" trigger)
            // short-circuit on the IsActive check above.
            ActiveMinigameSceneName = string.Empty;
            ActiveMinigameLabel = string.Empty;
            PendingEarnings = 0f;

            EnsureRunner();
            _runner.StartCoroutine(UnloadAndRestoreRoutine(sceneName, label, earned));
        }

        static IEnumerator LoadMinigameRoutine(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[MinigameSession] Failed to start loading minigame scene '{sceneName}'. Is it in Build Settings?");
                UnfreezeWorld();
                ActiveMinigameSceneName = string.Empty;
                ActiveMinigameLabel = string.Empty;
                yield break;
            }

            while (!op.isDone)
                yield return null;

            Scene loaded = SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid())
                SceneManager.SetActiveScene(loaded);

            // Minigames drive their own UI with their own EventSystem, so make sure the cursor is
            // visible and unlocked (the World locks it for third-person look-around).
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static IEnumerator UnloadAndRestoreRoutine(string sceneName, string label, float earned)
        {
            Scene minigame = SceneManager.GetSceneByName(sceneName);
            if (minigame.IsValid() && minigame.isLoaded)
            {
                AsyncOperation op = SceneManager.UnloadSceneAsync(minigame);
                while (op != null && !op.isDone)
                    yield return null;
            }

            UnfreezeWorld();

            Scene world = SceneManager.GetSceneByName(WorldSceneName);
            if (world.IsValid())
                SceneManager.SetActiveScene(world);

            // Restore the World's look-around cursor mode.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (earned > 0f)
                GlobalData.PlayerMoney += earned;

            ShowReturnNotification(label, earned);
        }

        static void FreezeWorldBeforeLoad(Scene world)
        {
            _frozenWorldRoots.Clear();
            foreach (GameObject root in world.GetRootGameObjects())
            {
                if (root != null && root.activeSelf)
                {
                    root.SetActive(false);
                    _frozenWorldRoots.Add(root);
                }
            }
        }

        static void UnfreezeWorld()
        {
            foreach (GameObject root in _frozenWorldRoots)
            {
                if (root != null)
                    root.SetActive(true);
            }
            _frozenWorldRoots.Clear();
        }

        static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("[MinigameSessionRunner]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }

        static void ShowReturnNotification(string label, float earned)
        {
            NotificationController notif = Object.FindFirstObjectByType<NotificationController>(FindObjectsInactive.Include);
            if (notif == null) return;

            string message = earned > 0f
                ? $"You earned ${earned:0} from {label}!"
                : $"Left {label} with $0.";
            notif.ShowMessage(message);
        }

        /// <summary>Private MonoBehaviour host so we can run coroutines from a static class.</summary>
        class Runner : MonoBehaviour { }
    }
}
