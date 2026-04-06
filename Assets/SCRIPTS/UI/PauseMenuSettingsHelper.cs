using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the same gameplay/audio settings controls as <see cref="MainMenu"/> for the pause menu canvas hierarchy.
/// </summary>
public static class PauseMenuSettingsHelper
{
    public static void Bind(Transform root)
    {
        if (root == null) return;

        BindAudioSliders(root);
        BindMinimapDropdown(root);
        BindPetInfoDropdown(root);
        BindLengthOfDayInput(root);
    }

    static void BindAudioSliders(Transform root)
    {
        Slider music = FindSlider(root, "MusicSlider");
        Slider sfx = FindSlider(root, "SFXSlider");

        if (music != null)
        {
            music.SetValueWithoutNotify(AudioSettings.MusicVolumeRaw);
            music.onValueChanged.RemoveAllListeners();
            music.onValueChanged.AddListener(v =>
            {
                AudioSettings.MusicVolumeRaw = v;
                MainMenu.RefreshMusicVolumeFromSettings();
            });
        }

        if (sfx != null)
        {
            sfx.SetValueWithoutNotify(AudioSettings.SFXVolumeRaw);
            sfx.onValueChanged.RemoveAllListeners();
            sfx.onValueChanged.AddListener(v => AudioSettings.SFXVolumeRaw = v);
        }
    }

    static Slider FindSlider(Transform root, string objectName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == objectName)
                return t.GetComponent<Slider>();
        }

        return null;
    }

    static void BindMinimapDropdown(Transform root)
    {
        TMP_Dropdown dd = FindDropdown(root, "MinimapDropdown");
        if (dd == null)
        {
            foreach (var d in root.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                if (d.gameObject.name == "Dropdown")
                {
                    dd = d;
                    break;
                }
            }
        }

        if (dd == null) return;

        int displayIndex = Mathf.Clamp((int)MinimapSettings.Mode, 0, dd.options.Count - 1);
        dd.SetValueWithoutNotify(displayIndex);
        dd.onValueChanged.RemoveAllListeners();
        dd.onValueChanged.AddListener(index =>
        {
            MinimapSettings.Mode = (MinimapMode)Mathf.Clamp(index, 0, 2);
        });
    }

    static TMP_Dropdown FindDropdown(Transform root, string objectName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == objectName)
                return t.GetComponent<TMP_Dropdown>();
        }

        return null;
    }

    static void BindPetInfoDropdown(Transform root)
    {
        TMP_Dropdown dd = FindDropdown(root, "PetInfoDropdown");
        if (dd == null) return;

        int saved = (int)PetInfoSettings.Mode;
        dd.SetValueWithoutNotify(Mathf.Clamp(saved, 0, dd.options.Count - 1));
        dd.onValueChanged.RemoveAllListeners();
        dd.onValueChanged.AddListener(index =>
        {
            PetInfoSettings.Mode = (PetInfoDisplayMode)Mathf.Clamp(index, 0, 2);
        });
    }

    static void BindLengthOfDayInput(Transform root)
    {
        TMP_InputField lengthOfDayInput = null;
        foreach (var f in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (f.gameObject.name == "LengthOfDayInput")
            {
                lengthOfDayInput = f;
                break;
            }
        }

        if (lengthOfDayInput == null) return;

        lengthOfDayInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        lengthOfDayInput.characterValidation = TMP_InputField.CharacterValidation.Digit;
        lengthOfDayInput.characterLimit = 3;
        lengthOfDayInput.onValidateInput = OnLengthOfDayValidateChar;
        lengthOfDayInput.onValueChanged.RemoveAllListeners();
        lengthOfDayInput.onValueChanged.AddListener(text => OnLengthOfDayValueChanged(lengthOfDayInput, text));
        lengthOfDayInput.onEndEdit.RemoveAllListeners();
        lengthOfDayInput.onEndEdit.AddListener(text => OnLengthOfDayEndEdit(lengthOfDayInput, text));

        lengthOfDayInput.SetTextWithoutNotify(DayNightSettings.GetMenuDisplayMinutes());
    }

    static char OnLengthOfDayValidateChar(string text, int charIndex, char addedChar)
    {
        return char.IsDigit(addedChar) ? addedChar : '\0';
    }

    static void OnLengthOfDayValueChanged(TMP_InputField lengthOfDayInput, string text)
    {
        if (lengthOfDayInput == null || string.IsNullOrEmpty(text))
            return;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]))
                sb.Append(text[i]);
        }

        string filtered = sb.ToString();
        if (filtered == text)
            return;

        int caret = lengthOfDayInput.caretPosition;
        lengthOfDayInput.SetTextWithoutNotify(filtered);
        lengthOfDayInput.caretPosition = Mathf.Min(caret, filtered.Length);
    }

    static void OnLengthOfDayEndEdit(TMP_InputField lengthOfDayInput, string text)
    {
        if (lengthOfDayInput == null)
            return;

        if (string.IsNullOrEmpty(text?.Trim()))
        {
            lengthOfDayInput.SetTextWithoutNotify(DayNightSettings.GetMenuDisplayMinutes());
            return;
        }

        if (!int.TryParse(text.Trim(), out int minutes) || minutes < (int)DayNightSettings.MinMinutes)
        {
            lengthOfDayInput.SetTextWithoutNotify(DayNightSettings.GetMenuDisplayMinutes());
            return;
        }

        minutes = Mathf.Clamp(minutes, (int)DayNightSettings.MinMinutes, (int)DayNightSettings.MaxMinutes);
        DayNightSettings.SetFullCycleMinutes(minutes);
        lengthOfDayInput.SetTextWithoutNotify(minutes.ToString());
    }
}
