using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// World-only hints driven by <see cref="GameNotifications"/>. Extend by adding PlayerPrefs keys and calls to <see cref="GameNotifications.Show"/>.
/// </summary>
public class WorldOnboardingNotifications : MonoBehaviour
{
    public const string PrefsPetShopHint = "Petropolis_Notif_World_PetShop_v1";
    public const string PrefsChoosePetHint = "Petropolis_Notif_World_ChoosePet_v1";
    public const string PrefsPetStatsHint = "Petropolis_Notif_World_PetStats_v1";
    /// <summary>Shown once on entering World (no character gate).</summary>
    public const string PrefsSpawnWelcome = "Petropolis_Notif_World_SpawnWelcome_v3";
    /// <summary>One-line Pet Statistics slider hint after the player has a pet.</summary>
    public const string PrefsPetStatsSlider = "Petropolis_Notif_World_PetStatsSlider_v3";

    /// <summary>Welcome toast copy (shown from <see cref="GlobalData.ScheduleWorldWelcomeNotification"/>).</summary>
    public const string WelcomeMessage = "Welcome to Petropolis—visit the pet shop when you want to pick a companion.";

    /// <summary>
    /// Call when the player opens PetChoose from World so we do not show the vague pet-shop tip after they have already found the entrance.
    /// </summary>
    public static void ConsumePetShopHintIfNeeded()
    {
        if (PlayerPrefs.GetInt(PrefsPetShopHint, 0) != 0)
            return;
        PlayerPrefs.SetInt(PrefsPetShopHint, 1);
        PlayerPrefs.Save();
    }

    [SerializeField] float initialDelaySeconds = 0.12f;

    void OnEnable()
    {
        if (SceneManager.GetActiveScene().name != "World")
            return;

        StartCoroutine(EvaluateDeferred());
    }

    IEnumerator EvaluateDeferred()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(initialDelaySeconds);
        EvaluateWorldHints(returnedFromAdditivePetChoose: false);
    }

    /// <summary>Re-run hint logic (e.g. after returning from an additive scene).</summary>
    public static void EvaluateWorldHintsFromWorldScene()
    {
        if (SceneManager.GetActiveScene().name != "World")
            return;

        var driver = FindFirstObjectByType<WorldOnboardingNotifications>();
        if (driver != null)
            driver.EvaluateWorldHints(returnedFromAdditivePetChoose: true);
    }

    void EvaluateWorldHints(bool returnedFromAdditivePetChoose)
    {
        if (SceneManager.GetActiveScene().name != "World")
            return;

        if (!GlobalData.HasCharacterChoice())
            return;

        if (GlobalData.StoredPetSkin > 0)
        {
            if (GlobalData.PetStatsToastFromPetConfirmPending)
                return;

            if (PlayerPrefs.GetInt(PrefsPetStatsSlider, 0) == 0)
            {
                bool petFromMainMenuPath = !GlobalData.HasOpenedPetChooseFlowFromWorld;
                bool neverHadLegacyStatsHint = PlayerPrefs.GetInt(PrefsPetStatsHint, 0) == 0;
                if (returnedFromAdditivePetChoose || petFromMainMenuPath || neverHadLegacyStatsHint)
                {
                    PlayerPrefs.SetInt(PrefsPetStatsSlider, 1);
                    PlayerPrefs.SetInt(PrefsPetStatsHint, 1);
                    PlayerPrefs.Save();
                    GameNotifications.Show(PetStatisticsHintText.BuildSliderOneLiner(), 7f);
                }
            }

            return;
        }

        if (PlayerPrefs.GetInt(PrefsPetShopHint, 0) == 0)
        {
            PlayerPrefs.SetInt(PrefsPetShopHint, 1);
            PlayerPrefs.Save();
            GameNotifications.Show("Head to the pet shop to choose your pet.", 5f);
            return;
        }

        if (GlobalData.HasOpenedPetChooseFlowFromWorld &&
            PlayerPrefs.GetInt(PrefsChoosePetHint, 0) == 0)
        {
            PlayerPrefs.SetInt(PrefsChoosePetHint, 1);
            PlayerPrefs.Save();
            GameNotifications.Show("At the pet shop, confirm a pet to bring them into the world.", 6f);
        }
    }
}
