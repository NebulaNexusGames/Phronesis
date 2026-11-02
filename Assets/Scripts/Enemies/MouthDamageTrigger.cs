using UnityEngine;

public class MouthDamageTrigger : MonoBehaviour
{
    [Header("Коллайдер рта (если пусто — свой/детский)")]
    [SerializeField] private Collider2D mouthColliderOverride;

    [Header("Лягушка (для отпускания)")]
    [SerializeField] private CrystalFrog frog;

    [Header("Урон")]
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private int damagePerTick = 1;

    [Header("Ненадолго отпустить после смерти")]
    [SerializeField] private float releaseDuration = 1.5f;

    private Collider2D mouthCollider;
    private float playerTimer;
    private float mobTimer;
    private float releaseTimer;
    private bool released;

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void Awake()
    {
        mouthCollider = mouthColliderOverride != null ? mouthColliderOverride : GetComponent<Collider2D>();
        if (mouthCollider == null)
            mouthCollider = GetComponentInChildren<Collider2D>();
    }

    private void Update()
    {
        if (released)
        {
            releaseTimer -= Time.deltaTime;
            if (releaseTimer <= 0f)
            {
                released = false;
                playerTimer = 0f;
                mobTimer = 0f;
            }
            return;
        }

        if (frog != null && frog.IsReleasing()) return;
        if (mouthCollider == null) return;

        Bounds mb = mouthCollider.bounds;
        float radius = Mathf.Max(mb.extents.x, mb.extents.y);
        if (radius <= 0.01f) radius = 0.5f;
        radius += 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(mb.center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D h = hits[i];
            if (h == null || h.isTrigger) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h == mouthCollider) continue;

            PlayerHealth hp = h.GetComponentInParent<PlayerHealth>();
            if (hp != null && hp.gameObject.activeInHierarchy)
            {
                TickPlayer(hp);
                return;
            }

            MobHealth mh = h.GetComponentInParent<MobHealth>();
            if (mh != null && mh.gameObject.activeInHierarchy && !mh.IsDead)
            {
                TickMob(mh);
                return;
            }
        }
    }

    private void TickPlayer(PlayerHealth hp)
    {
        playerTimer -= Time.deltaTime;
        if (playerTimer > 0f) return;
        playerTimer = damageInterval;

        bool death = hp.TakeDamageBoth(damagePerTick);

        if (frog != null)
        {
            frog.NotifyPlayerDamageDealt(hp, damagePerTick);
            if (death && frog.IsPlayerVictim(hp))
                StartRelease();
        }
    }

    private void TickMob(MobHealth mh)
    {
        mobTimer -= Time.deltaTime;
        if (mobTimer > 0f) return;
        mobTimer = damageInterval;

        mh.TakeDamage(damagePerTick);
        if (mh.IsDead && frog != null && frog.IsRollerVictim(mh))
            StartRelease();
    }

    private void StartRelease()
    {
        released = true;
        releaseTimer = releaseDuration;
        playerTimer = 0f;
        mobTimer = 0f;
        if (frog != null)
            frog.ReleaseFromMouth();
    }
}