using UnityEngine;

/// <summary>
/// Attach to a trigger collider that represents a named area (e.g. Hospital, Supermarket, Park).
/// When the player enters the trigger, it pulls down the shared AreaEnter notification with
/// the message "You entered the &lt;area&gt;!", holds for <see cref="displaySeconds"/>, then
/// slides it back up.
///
/// The target notification is resolved (in order):
///   1. The serialized <see cref="notification"/> reference.
///   2. A GameObject named <see cref="defaultNotificationName"/> ("AreaEnter") in the scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AreaEnterZone : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Tooltip("Name inserted into the sentence 'You entered the ___!'. If left blank, the " +
             "GameObject's name will be used (PascalCase is auto-split, e.g. 'PetStore' -> 'pet store').")]
    [SerializeField] string areaDisplayName = "";

    [Tooltip("How long the notification stays on-screen before sliding back up.")]
    [SerializeField, Min(0f)] float displaySeconds = 1.5f;

    [Tooltip("Explicit notification reference. If null, the scene is searched for a GameObject " +
             "named 'AreaEnter' with a NotificationController component.")]
    [SerializeField] NotificationController notification;

    [SerializeField] string defaultNotificationName = "AreaEnter";

    static NotificationController _cachedNotification;

    bool _playerInside;

    void Awake()
    {
        if (gameObject.isStatic)
            gameObject.isStatic = false;

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (_playerInside) return;
        _playerInside = true;

        NotificationController target = ResolveNotification();
        if (target == null) return;

        string niceName = !string.IsNullOrWhiteSpace(areaDisplayName)
            ? areaDisplayName.Trim()
            : HumanizeName(gameObject.name);

        target.ShowMessage($"You entered the {niceName}!", displaySeconds);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerInside = false;
    }

    NotificationController ResolveNotification()
    {
        if (notification != null) return notification;

        if (_cachedNotification != null) return _cachedNotification;

        GameObject go = GameObject.Find(defaultNotificationName);
        if (go != null)
            _cachedNotification = go.GetComponent<NotificationController>();

        return _cachedNotification;
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        Transform root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    /// <summary>
    /// "Hospital" -> "hospital"; "PetStore" -> "pet store"; "SuperMarket" -> "super market".
    /// </summary>
    static string HumanizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        var sb = new System.Text.StringBuilder(raw.Length + 4);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(raw[i - 1]))
                sb.Append(' ');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }
}
