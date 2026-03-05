using UnityEngine;

/// <summary>
/// Rotates the object around its Y axis, oscillating back and forth like a fan.
/// Uses a sine curve so it eases into and out of each end of the arc.
/// </summary>
public class DogRotator : MonoBehaviour
{
    [Tooltip("How many degrees it swings to each side of center (e.g. 45 = 45° left, 45° right).")]
    [SerializeField] float arcDegrees = 45f;

    [Tooltip("Seconds for one full cycle (swing left, swing right, back to center). Higher = slower.")]
    [SerializeField] float cycleDurationSeconds = 12f;

    float _startY;

    void Start()
    {
        _startY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (cycleDurationSeconds <= 0f) return;

        // Sine naturally eases to zero velocity at the arc ends
        float t = (2f * Mathf.PI * Time.time) / cycleDurationSeconds;
        float offset = arcDegrees * Mathf.Sin(t);
        transform.rotation = Quaternion.Euler(0f, _startY + offset, 0f);
    }
}
