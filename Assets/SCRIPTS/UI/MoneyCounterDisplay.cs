using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the money label on this GameObject (same object as <see cref="TextMeshProUGUI"/>).
/// Includes optional arrow-key test hooks and color feedback.
/// </summary>
[DisallowMultipleComponent]
public class MoneyCounterDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;

    [Header("Balance testing (arrow keys)")]
    [Tooltip("When enabled, Up/Down arrow change PlayerMoney for testing. Turn off for normal play.")]
    [SerializeField] bool enableMoneyArrowTest = true;
    [SerializeField] int moneyTestStep = 100;
    [SerializeField] Color moneyIncreaseColor = new Color(0.35f, 0.95f, 0.45f, 1f);
    [SerializeField] Color moneyDecreaseColor = new Color(0.95f, 0.35f, 0.35f, 1f);
    [SerializeField, Min(0.01f)] float moneyColorRampIn = 0.2f;
    [SerializeField, Min(0f)] float moneyColorHold = 0.35f;
    [SerializeField, Min(0.01f)] float moneyColorRampOut = 0.4f;

    Color _moneyBaseColor;
    Color _moneyAccentColor;

    enum MoneyColorPhase { Base, RampToAccent, HoldAccent, RampToBase }
    MoneyColorPhase _moneyColorPhase;
    float _moneyColorPhaseTime;

    void Awake()
    {
        if (label == null)
            label = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (label != null)
            _moneyBaseColor = label.color;
    }

    void Update()
    {
        if (label == null)
            return;

        if (enableMoneyArrowTest && Keyboard.current != null)
        {
            if (Keyboard.current[Key.UpArrow].wasPressedThisFrame)
            {
                GlobalData.PlayerMoney += moneyTestStep;
                StartMoneyColorPulse(moneyIncreaseColor);
            }
            else if (Keyboard.current[Key.DownArrow].wasPressedThisFrame)
            {
                GlobalData.PlayerMoney = Mathf.Max(0f, GlobalData.PlayerMoney - moneyTestStep);
                StartMoneyColorPulse(moneyDecreaseColor);
            }
        }

        TickMoneyColorAnimation();
        label.text = $"${GlobalData.PlayerMoney:0.00}";
        if (_moneyColorPhase == MoneyColorPhase.Base)
            label.color = _moneyBaseColor;
    }

    void StartMoneyColorPulse(Color accent)
    {
        _moneyAccentColor = accent;
        _moneyColorPhase = MoneyColorPhase.RampToAccent;
        _moneyColorPhaseTime = 0f;
    }

    void TickMoneyColorAnimation()
    {
        if (label == null)
            return;

        switch (_moneyColorPhase)
        {
            case MoneyColorPhase.Base:
                label.color = _moneyBaseColor;
                return;

            case MoneyColorPhase.RampToAccent:
                _moneyColorPhaseTime += Time.deltaTime;
                {
                    float t = Mathf.Clamp01(_moneyColorPhaseTime / moneyColorRampIn);
                    float s = Mathf.SmoothStep(0f, 1f, t);
                    label.color = Color.Lerp(_moneyBaseColor, _moneyAccentColor, s);
                    if (t >= 1f)
                    {
                        _moneyColorPhase = MoneyColorPhase.HoldAccent;
                        _moneyColorPhaseTime = 0f;
                    }
                }
                break;

            case MoneyColorPhase.HoldAccent:
                label.color = _moneyAccentColor;
                _moneyColorPhaseTime += Time.deltaTime;
                if (_moneyColorPhaseTime >= moneyColorHold)
                {
                    _moneyColorPhase = MoneyColorPhase.RampToBase;
                    _moneyColorPhaseTime = 0f;
                }
                break;

            case MoneyColorPhase.RampToBase:
                _moneyColorPhaseTime += Time.deltaTime;
                {
                    float t = Mathf.Clamp01(_moneyColorPhaseTime / moneyColorRampOut);
                    float s = Mathf.SmoothStep(0f, 1f, t);
                    label.color = Color.Lerp(_moneyAccentColor, _moneyBaseColor, s);
                    if (t >= 1f)
                    {
                        _moneyColorPhase = MoneyColorPhase.Base;
                        label.color = _moneyBaseColor;
                    }
                }
                break;
        }
    }
}
