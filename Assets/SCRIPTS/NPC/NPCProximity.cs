using UnityEngine;

public class NPCProximity : MonoBehaviour
{
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Animator[] npcAnimators;

    private static readonly int PlayerNearby = Animator.StringToHash("PlayerNearby");
    private Transform _player;

    /// <summary>
    /// Per-animator cache for whether the assigned controller actually has the "PlayerNearby" bool.
    /// Silences the "Parameter 'Hash ...' does not exist" warning Unity spams every frame when an
    /// animator in <see cref="npcAnimators"/> uses a controller without that parameter (for example
    /// if someone slotted a Male/Female idle controller on the pet or a non-NPC prop).
    /// </summary>
    private System.Collections.Generic.Dictionary<Animator, bool> _hasPlayerNearby;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        if (npcAnimators == null || npcAnimators.Length == 0)
            npcAnimators = GetComponentsInChildren<Animator>(false);

        _hasPlayerNearby = new System.Collections.Generic.Dictionary<Animator, bool>(npcAnimators != null ? npcAnimators.Length : 0);
    }

    private void Update()
    {
        if (_player == null)
            return;

        foreach (Animator anim in npcAnimators)
        {
            if (anim == null || !anim.isActiveAndEnabled || anim.runtimeAnimatorController == null)
                continue;

            if (!AnimatorHasPlayerNearby(anim))
                continue;

            float dist = Vector3.Distance(_player.position, anim.transform.position);
            anim.SetBool(PlayerNearby, dist <= detectionRange);
        }
    }

    private bool AnimatorHasPlayerNearby(Animator anim)
    {
        if (_hasPlayerNearby.TryGetValue(anim, out bool cached))
            return cached;

        bool has = false;
        AnimatorControllerParameter[] parameters = anim.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == PlayerNearby)
            {
                has = true;
                break;
            }
        }

        _hasPlayerNearby[anim] = has;
        return has;
    }
}
