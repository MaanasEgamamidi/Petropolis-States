using UnityEngine;

public class NPCProximity : MonoBehaviour
{
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Animator[] npcAnimators;

    private static readonly int PlayerNearby = Animator.StringToHash("PlayerNearby");
    private Transform _player;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        if (npcAnimators == null || npcAnimators.Length == 0)
            npcAnimators = GetComponentsInChildren<Animator>(true);
    }

    private void Update()
    {
        if (_player == null)
            return;

        foreach (Animator anim in npcAnimators)
        {
            if (anim == null) continue;

            float dist = Vector3.Distance(_player.position, anim.transform.position);
            anim.SetBool(PlayerNearby, dist <= detectionRange);
        }
    }
}
