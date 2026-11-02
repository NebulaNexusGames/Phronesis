using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CrystalRoller : MonoBehaviour
{
    [Header("Режим (true = домашний, false = дикий)")]
    public bool Friendly;

    [Header("Цели")]
    [SerializeField] private string[] targetTags = { "Player" };

    [Header("Зоны")]
    [SerializeField] private Transform patrolCenter;
    [SerializeField] private float territoryRadius = 6f;
    [SerializeField] private float viewDistance = 7f;

    [Header("Скорости")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float alertSpeed = 5f;

    [Header("Случайность патруля")]
    [SerializeField] private float patrolIdleMin = 0.3f;
    [SerializeField] private float patrolIdleMax = 1.4f;

    [Header("Прыжок перемещения (дикий)")]
    [SerializeField] private float maxJumpHeight = 1.5f;

    [Header("Прыжок атаки (дикий)")]
    [SerializeField] private float attackJumpRange = 3f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Шипы (Spikes)")]
    [SerializeField] private Transform spikes;
    [SerializeField] private float spikeScalePatrol = 0.1f;
    [SerializeField] private float spikeScaleChase = 0.5f;
    [SerializeField] private float spikeScaleAttack = 1f;
    [SerializeField] private float spikeLerpSpeed = 14f;

    [Header("Урон")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float damageCooldown = 0.8f;
    [SerializeField] private float pauseAfterHit = 1f;
    [SerializeField] private float aggroOnContactMemory = 4f;

    [Header("Преследование (дикий)")]
    [SerializeField] private float maxChaseDistance = 12f;

    [Header("Территории (дикие)")]
    [SerializeField] private float territoryGrowthOnKill = 1.35f;
    [SerializeField] private float huntingGrowthOnKill = 1.25f;
    [SerializeField] private float rallyMemory = 6f;
    [SerializeField] private float packSeparation = 2f;

    [Header("Охота (дикие)")]
    [SerializeField] private float huntChanceRival = 0.35f;
    [SerializeField] private float huntCooldownMin = 7f;
    [SerializeField] private float huntCooldownMax = 14f;
    [SerializeField] private float huntRetryMin = 3f;
    [SerializeField] private float huntRetryMax = 6f;

    [Header("Страх (домашний)")]
    [SerializeField] private float fearSpeed = 5f;
    [SerializeField] private float fearDistance = 5f;
    [SerializeField] private float fearJumpForce = 8f;
    [SerializeField] private float staminaMax = 5f;
    [SerializeField] private float staminaDrainPerSec = 1f;
    [SerializeField] private float staminaRegenPerSec = 1f;
    [SerializeField] private float exhaustedSpeed = 1f;
    [SerializeField] private float threatMemoryTime = 3f;
    [SerializeField] private float avoidDuration = 6f;

    [Header("Препятствия")]
    [SerializeField] private float maxFallDepth = 0.8f;
    [SerializeField] private LayerMask wallMask;

    [Header("Проверка стен (двойной веер)")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private int wallRayCount = 5;
    [SerializeField] private float wallRayLength = 1.2f;
    [SerializeField] private int jumpRayCount = 5;
    [SerializeField] private float jumpRayLength = 2.4f;
    [SerializeField] private int wallHitsForTurn = 3;
    [SerializeField] private int wallHitsForJump = 2;

    [Header("Проверки")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Мирный режим")]
    [SerializeField] private float keepDistance = 1.6f;
    [SerializeField] private float petRadius = 2f;

    [Header("Поглаживание")]
    [SerializeField] private InteractionSettings interactionSettings;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string petTrigger = "Pet";
    [SerializeField] private float petDuration = 0.8f;
    [SerializeField] private float petAttractSpeed = 6f;
    [SerializeField] private float petAttractDistance = 0.5f;

    [Header("Характер (домашний)")]
    [SerializeField] private bool personalityTactile;
    [SerializeField] private bool personalitySociophobia;
    [SerializeField] private bool personalityCowardice;
    [SerializeField] private bool personalityBravery;
    [SerializeField] private bool personalityOverbravery;
    [SerializeField] private bool personalityJealousy;
    [SerializeField] private int tactileSleepPats = 4;
    [SerializeField] private float sociophobiaRadius = 5f;
    [SerializeField] private float braveryStrikeInterval = 2.2f;
    [SerializeField] private float braveryStrikeDuration = 1.1f;
    [SerializeField] private float jealousyRadius = 2.5f;
    [SerializeField] private float jealousyRetargetCooldown = 3f;
    [SerializeField] private float retaliationDuration = 5f;
    [SerializeField] private float dodgeCooldownTime = 2f;
    [SerializeField] private float dodgeBackSpeed = 3f;
    [SerializeField] private float dodgeJumpForce = 4f;
    [SerializeField] private float cowardicePatReduction = 0.5f;

    [Header("Память об игроке (домашний)")]
    [SerializeField] private float playerMemoryTime = 5f;

    [Header("Страх перед лягушками (домашний и дикий)")]
    [SerializeField] private float frogFearDistance = 10f;
    [SerializeField] private float frogFleeMemoryTime = 4f;
    [SerializeField] private float frogFleeSpeed = 6f;

    [Header("Внешний вид")]
    [SerializeField] private Transform visual;
    [SerializeField] private float rollRadius = 0.5f;

    [Header("Глаза (общие)")]
    [SerializeField] private Transform pupil;
    [SerializeField] private SpriteRenderer pupilRenderer;
    [SerializeField] private float pupilMaxOffset = 0.12f;
    [SerializeField] private float pupilMoveSpeed = 7f;

    [Header("Глаза (дикий)")]
    [SerializeField] private Sprite wildPupilNormal;
    [SerializeField] private Sprite wildPupilAggressive;
    [SerializeField] private float wildPupilSizeFear = 0.6f;

    [Header("Глаза (домашний)")]
    [SerializeField] private Sprite homePupilNormal;
    [SerializeField] private Sprite homePupilScared;
    [SerializeField] private Sprite homePupilHappy;
    [SerializeField] private float homePupilSizeNormal = 1f;
    [SerializeField] private float homePupilSizeScared = 0.6f;
    [SerializeField] private float homePupilSizeHappy = 1f;
    [SerializeField] private float homePupilTrembleAmount = 0.035f;
    [Header("Веки (домашний)")]
    [SerializeField] private Transform eyelidRoot;
    [SerializeField] private GameObject eyelidsHappyOpen;
    [SerializeField] private GameObject eyelidsHappyClosed;
    [SerializeField] private GameObject eyelidsClosed;

    [Header("Отступление и рейд (дикие)")]
    [SerializeField] private float lowHpRetreatThreshold = 1f;
    [SerializeField] private float raidDelayMin = 8f;
    [SerializeField] private float raidDelayMax = 18f;
    [SerializeField] private float raidTimeoutTime = 40f;

    [Header("Восстановление ХП")]
    [SerializeField] private float wildRegenInterval = 10f;
    [SerializeField] private float friendlyRegenInterval = 5f;

    [Header("Слои при прохождении")]
    [SerializeField] private int passSortingBoost = 2;

    [Header("Звуки")]
    [SerializeField] private AudioSource soundSource;
    [SerializeField] private AudioClip petSound;

    private static readonly List<CrystalRoller> Registry = new List<CrystalRoller>();

    public static CrystalRoller FindNearestPet(Vector2 pos, float maxDist)
    {
        CrystalRoller best = null;
        float bestSqr = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller roller = Registry[i];
            if (roller == null || !roller.Friendly || !roller.gameObject.activeInHierarchy) continue;
            float sqr = ((Vector2)roller.transform.position - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = roller;
            }
        }
        return best;
    }

    private const float MaxJumpMargin = 1.2f;

    private Rigidbody2D body;
    private Collider2D selfCollider;
    private CircleCollider2D visionTrigger;
    private Vector2 homePosition;
    private Transform player;

    private readonly List<Collider2D> inVision = new List<Collider2D>();

    private readonly List<CrystalRoller> pack = new List<CrystalRoller>();
    private float baseTerritory;
    private float baseHunt;
    private float territoryGrowth = 1f;
    private float huntGrowth = 1f;
    private Vector2? packCenterOverride;

    private Vector2 rememberedThreat;
    private float threatMemoryTimer;

    private float rollAngle;
    private float yaw;
    private int facing = 1;

    private float huntCooldownTimer;
    private int avoidDirection = 1;
    private float avoidTimer;

    private static int lastPairFrame = -1;

    private SpriteRenderer[] modelRenderers;
    private int[] baseOrders;

    private MobHealth myHealth;
    private CrystalRoller raidTarget;
    private bool raidPending;
    private float raidCountdown;
    private bool raidActive;
    private float raidTimeout;
    private bool retreating;
    private float idleTimer;
    private float wildRegenTimer;
    private float friendlyRegenTimer;
    private Quaternion pupilBaseWorldRot = Quaternion.identity;

    private Transform target;
    private bool targetIsFriendly;
    private Transform forcedTarget;
    private float forcedTimer;

    private float attackTimer;
    private float pauseTimer;
    private float damageTimer;
    private bool wasGrounded = true;
    private bool isJumping;
    private bool wasAirborne;
    private bool isCrashed;
    private float crashSleepTimer;
    private bool isAttackJump;
    private float jumpSafetyTimer;

    private int moveDirection = 1;

    private bool staying;
    private int patCount;
    private float interactRadius;
    private bool petting;
    private float petTimer;
    private CharacterBody petSource;
    private float stamina;
    private float happyBlinkTimer;
    private EyelidState eyelidState;
    private Quaternion eyelidRootBaseWorldRot = Quaternion.identity;
    private AudioSource audioSource;

    private Personality personality;
    private bool sociophobiaHitOnce;
    private float cowardiceFear = 1f;
    private float braveStrikeTimer;
    private float jealousCooldown;
    private Transform homeAggroTarget;
    private Transform attackTarget;
    private bool homeAggroOnce;
    private Transform retaliateTarget;
    private float retaliateTimer;
    private float dodgeCooldown;
    private float bravePhaseTimer;
    private bool braveAttacking;
    private bool playerScared;
    private float playerMemoryTimer;
    private Vector2 frogThreatPos;
    private float frogFleeTimer;

    [HideInInspector] public bool tongueGrabbed;

    private readonly List<Vector2> lastScanHits = new List<Vector2>();
    private readonly List<Vector2> lastJumpHits = new List<Vector2>();
    private Vector2 pitProbeOrigin;
    private bool lastPitAhead;

    private struct JumpTrajectory
    {
        public Vector2 start;
        public float vx;
        public float vy;
        public float gravity;
        public float duration;
        public Vector2 landPoint;
        public float landTime;
        public bool landFound;
        public bool active;
    }
    private JumpTrajectory trajectory;

    private Vector2 Center => packCenterOverride.HasValue
        ? packCenterOverride.Value
        : (patrolCenter != null ? (Vector2)patrolCenter.position : homePosition);

    private float PetDistance => Mathf.Max(petRadius, interactRadius);

    private bool PlayerNearForPet()
    {
        if (player == null) return false;
        return Vector2.Distance(player.position, transform.position) <= PetDistance;
    }

    private Vector2 ZoneSource() => patrolCenter != null ? (Vector2)patrolCenter.position : homePosition;

    private struct WallScanResult
    {
        public bool hit;
        public int hitCount;
        public float closestDist;
        public Collider2D hitCollider;
    }

    private enum RollerState
    {
        Patrol,
        Chase,
        Attack,
        Crashed,
        Staying
    }

    private enum EyelidState
    {
        None,
        HappyOpen,
        HappyClosed,
        Closed
    }

    [System.Flags]
    private enum Personality
    {
        None = 0,
        Tactile = 1 << 0,
        Sociophobia = 1 << 1,
        Cowardice = 1 << 2,
        Bravery = 1 << 3,
        Overbravery = 1 << 4,
        Jealousy = 1 << 5
    }
    private RollerState state = RollerState.Patrol;

    private float stuckProbeTimer;
    private Vector2 stuckProbePos;
    private bool breakingFree;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        selfCollider = GetComponent<Collider2D>();
        homePosition = transform.position;
        baseTerritory = territoryRadius;
        baseHunt = maxChaseDistance;
        interactRadius = Mathf.Max(keepDistance + 0.4f, keepDistance * 1.2f);
        stamina = staminaMax;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && soundSource != null)
            audioSource = soundSource;
        stuckProbePos = transform.position;
        myHealth = GetComponent<MobHealth>();

        ResolvePersonality();

        if (pupil != null)
            pupilBaseWorldRot = pupil.rotation;
        if (eyelidRoot != null)
            eyelidRootBaseWorldRot = eyelidRoot.rotation;

        if (eyelidsHappyOpen != null) eyelidsHappyOpen.SetActive(false);
        if (eyelidsHappyClosed != null) eyelidsHappyClosed.SetActive(false);
        if (eyelidsClosed != null) eyelidsClosed.SetActive(false);

        if (visual != null)
        {
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x);
            visual.localScale = scale;
            rollAngle = visual.localEulerAngles.z;
            yaw = 0f;
        }

        visionTrigger = gameObject.AddComponent<CircleCollider2D>();
        visionTrigger.isTrigger = true;
        visionTrigger.radius = viewDistance;

        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        if (visual != null)
            renderers.AddRange(visual.GetComponentsInChildren<SpriteRenderer>(true));
        if (spikes != null)
        {
            SpriteRenderer sr = spikes.GetComponent<SpriteRenderer>();
            if (sr != null && !renderers.Contains(sr)) renderers.Add(sr);
        }
        if (pupilRenderer != null && !renderers.Contains(pupilRenderer)) renderers.Add(pupilRenderer);
        modelRenderers = renderers.ToArray();
        baseOrders = new int[modelRenderers.Length];
        for (int i = 0; i < modelRenderers.Length; i++)
            baseOrders[i] = modelRenderers[i].sortingOrder;
    }

    private void ResolvePersonality()
    {
        Personality requested = Personality.None;
        if (personalityTactile) requested |= Personality.Tactile;
        if (personalitySociophobia) requested |= Personality.Sociophobia;
        if (personalityCowardice) requested |= Personality.Cowardice;
        if (personalityBravery) requested |= Personality.Bravery;
        if (personalityOverbravery) requested |= Personality.Overbravery;
        if (personalityJealousy) requested |= Personality.Jealousy;

        if (requested == Personality.None)
        {
            Personality[] randomTraits =
            {
                Personality.Tactile, Personality.Sociophobia, Personality.Cowardice,
                Personality.Bravery, Personality.Overbravery, Personality.Jealousy
            };
            requested = randomTraits[Random.Range(0, randomTraits.Length)];
        }

        Personality courageGroup = Personality.Cowardice | Personality.Bravery | Personality.Overbravery;
        requested = ResolveGroup(requested, courageGroup);

        if ((requested & Personality.Sociophobia) != 0 && (requested & (Personality.Tactile | Personality.Jealousy)) != 0)
            requested = ResolveGroup(requested, Personality.Sociophobia | Personality.Tactile | Personality.Jealousy);

        personality = requested;
        cowardiceFear = 1f;
        bravePhaseTimer = braveryStrikeInterval;
        braveAttacking = true;
    }

    private Personality ResolveGroup(Personality flags, Personality group)
    {
        Personality active = flags & group;
        if (active == Personality.None) return flags;
        int count = 0;
        Personality[] values = { Personality.Tactile, Personality.Sociophobia, Personality.Cowardice, Personality.Bravery, Personality.Overbravery, Personality.Jealousy };
        for (int i = 0; i < values.Length; i++)
            if ((active & values[i]) != 0) count++;
        if (count <= 1) return flags;

        Personality keep = Personality.None;
        int keepCount = 0;
        int pick = Random.Range(0, count);
        for (int i = 0; i < values.Length; i++)
        {
            if ((active & values[i]) == 0) continue;
            if (keepCount == pick)
            {
                keep = values[i];
                break;
            }
            keepCount++;
        }
        return (flags & ~group) | keep;
    }

    private bool HasPersonality(Personality p)
    {
        return (personality & p) != 0;
    }

    private void OnValidate()
    {
        if (visionTrigger != null)
            visionTrigger.radius = viewDistance;
    }
private void OnEnable() { Registry.Add(this); }

    private void OnDisable() { Registry.Remove(this); }

    private void Start()
    {
        if (!Friendly)
            RebuildTerritories();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (!inVision.Contains(other))
            inVision.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        inVision.Remove(other);
    }

    private void Update()
    {
        FindNearestPlayer();
        UpdateSpikes();
        UpdateVisual(Time.deltaTime);
        UpdatePupil(Time.deltaTime);
        UpdateEyelids(Time.deltaTime);
        StabilizeEyelidRoot();
        RegenHp(Time.deltaTime);
    }

    private void FindNearestPlayer()
    {
        Transform nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = inVision.Count - 1; i >= 0; i--)
        {
            Collider2D col = inVision[i];
            if (col == null) { inVision.RemoveAt(i); continue; }

            GameObject go = col.gameObject;
            if (!go.activeInHierarchy) continue;

            PlayerHealth hp = FindPlayerHealth(go);
            if (hp == null) continue;

            if (!HasClearPathTo((Vector2)go.transform.position)) continue;

            float sqr = ((Vector2)go.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = go.transform;
            }
        }

        if (nearest == null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, viewDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                GameObject go = hits[i].gameObject;
                if (!go.activeInHierarchy) continue;

                PlayerHealth hp = FindPlayerHealth(go);
                if (hp == null) continue;

                if (!HasClearPathTo((Vector2)go.transform.position)) continue;

                float sqr = ((Vector2)go.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = go.transform;
                }
            }
        }

        if (nearest != null)
        {
            player = nearest;
            if (Friendly)
                playerMemoryTimer = playerMemoryTime;
        }
        else if (Friendly && player != null)
        {
            playerMemoryTimer -= Time.deltaTime;
            if (playerMemoryTimer <= 0f)
                player = null;
        }
        else
        {
            player = null;
        }
    }

    private PlayerHealth FindPlayerHealth(GameObject go)
    {
        PlayerHealth hp = go.GetComponent<PlayerHealth>();
        if (hp != null) return hp;
        return go.GetComponentInParent<PlayerHealth>();
    }

    private void FixedUpdate()
    {
        if (lastPairFrame != Time.frameCount)
        {
            lastPairFrame = Time.frameCount;
            RefreshCollisionPairs();
            RefreshPlayerCollision();
            ApplyDepthLayers();
        }

        float dt = Time.fixedDeltaTime;
        bool grounded = IsGrounded();

        if (isCrashed)
        {
            if (grounded)
            {
                crashSleepTimer -= dt;
                if (crashSleepTimer <= 0f)
                    isCrashed = false;
            }
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        if (tongueGrabbed)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (isJumping)
        {
            if (!grounded)
                wasAirborne = true;

            if (wasAirborne && grounded)
            {
                bool wasAttack = isAttackJump;
                isJumping = false;
                wasAirborne = false;
                isAttackJump = false;
                attackTarget = null;
                if (wasAttack)
                {
                    body.linearVelocity = new Vector2(body.linearVelocity.x * 0.3f, body.linearVelocity.y);
                    pauseTimer = pauseAfterHit * 0.3f;
                }
            }

            jumpSafetyTimer -= dt;
            if (jumpSafetyTimer <= 0f)
            {
                isJumping = false;
                wasAirborne = false;
                isAttackJump = false;
                attackTarget = null;
            }
        }
        else
        {
            wasAirborne = !grounded;

            float vx = Friendly ? FriendlyVx(dt, grounded) : WildVx(dt, grounded);
            if (!isJumping)
                body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
        }

        wasGrounded = grounded;

        if (petting)
        {
            petTimer -= dt;
            if (petTimer <= 0f)
            {
                petting = false;
                petSource = null;
            }
        }

        if (damageTimer > 0f) damageTimer -= dt;
        if (attackTimer > 0f) attackTimer -= dt;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CrystalRoller collidedRoller = collision.collider.GetComponentInParent<CrystalRoller>();
        if (Friendly)
        {
            if (TryAttackHit(collision.collider))
                return;

            if (collidedRoller != null && !collidedRoller.Friendly)
            {
                float side = Mathf.Sign(collidedRoller.transform.position.x - transform.position.x);
                avoidDirection = side >= 0f ? -1 : 1;
                avoidTimer = avoidDuration;
            }

            return;
        }

        if (isJumping)
        {
            PlayerHealth hitPlayerHp = collision.collider.GetComponentInParent<PlayerHealth>();
            CrystalRoller hitRoller = collision.collider.GetComponentInParent<CrystalRoller>();
            if (hitRoller != null && pack.Contains(hitRoller)) return;
            bool hitTarget = hitPlayerHp != null || (hitRoller != null && hitRoller.Friendly);

            if (isAttackJump && hitTarget && damageTimer <= 0f)
            {
                if (hitRoller != null)
                {
                    MobHealth otherHp = hitRoller.GetComponent<MobHealth>();
                    if (otherHp != null && !otherHp.IsDead)
                    {
                        otherHp.TakeDamage(contactDamage);
                        hitRoller.NotifyHitBy(this);
                        damageTimer = damageCooldown;
                    }
                }
                else
                {
                    hitPlayerHp.TakeDamage(contactDamage);
                    damageTimer = damageCooldown;
                }

                pauseTimer = pauseAfterHit * 0.3f;
                BounceAway(collision.transform.position);
                return;
            }

            ContactPoint2D contact = collision.GetContact(0);
            if (contact.normal.y < 0.5f)
            {
                isJumping = false;
                isCrashed = true;
                crashSleepTimer = 2f;
                return;
            }
            return;
        }

        PlayerHealth playerHp = collision.collider.GetComponentInParent<PlayerHealth>();
        if (playerHp != null && damageTimer <= 0f)
        {
            playerHp.TakeDamage(contactDamage);
            damageTimer = damageCooldown;
            pauseTimer = pauseAfterHit * 0.5f;
            return;
        }

        CrystalRoller otherRoller = collision.collider.GetComponentInParent<CrystalRoller>();
        if (otherRoller != null)
        {
            if (otherRoller.Friendly)
            {
                ForceAggro(otherRoller.transform);
                if (damageTimer <= 0f)
                {
                    MobHealth otherHp = otherRoller.GetComponent<MobHealth>();
                    if (otherHp != null && !otherHp.IsDead)
                    {
                        otherHp.TakeDamage(contactDamage);
                        otherRoller.NotifyHitBy(this);
                        damageTimer = damageCooldown;
                    }
                }
                pauseTimer = pauseAfterHit * 0.3f;
            }
            else if (!pack.Contains(otherRoller) && damageTimer <= 0f)
            {
                MobHealth otherHp = otherRoller.GetComponent<MobHealth>();
                if (otherHp != null && !otherHp.IsDead)
                {
                    otherHp.TakeDamage(contactDamage);
                    damageTimer = damageCooldown;
                    if (otherHp.IsDead && !otherRoller.Friendly)
                        GrowTerritoryAfterKill(otherRoller);
                }
                pauseTimer = pauseAfterHit;
            }
            else if (pack.Contains(otherRoller))
            {
                pauseTimer = pauseAfterHit * 0.3f;
            }
            return;
        }

        if (!otherRoller)
        {
            bool hitWall = collision.contactCount > 0 && collision.GetContact(0).normal.y < 0.5f;
            if (hitWall && !isJumping)
            {
                moveDirection = -moveDirection;
            }
        }
    }

    private bool TryAttackHit(Collider2D hitCollider)
    {
        if (!Friendly || !isAttackJump || damageTimer > 0f) return false;
        if (homeAggroTarget == null && attackTarget == null) return false;

        PlayerHealth hitPlayerHp = hitCollider.GetComponentInParent<PlayerHealth>();
        CrystalRoller hitRoller = hitCollider.GetComponentInParent<CrystalRoller>();
        if (hitPlayerHp == null && hitRoller == null) return false;

        bool isAggroPlayer = hitPlayerHp != null &&
            (IsSameActor(homeAggroTarget, hitPlayerHp.transform) || IsSameActor(attackTarget, hitPlayerHp.transform));
        bool isAggroRoller = hitRoller != null &&
            (IsSameActor(homeAggroTarget, hitRoller.transform) || IsSameActor(attackTarget, hitRoller.transform));

        if (!isAggroPlayer && !isAggroRoller) return false;

        if (isAggroRoller)
        {
            MobHealth otherHp = hitRoller.GetComponent<MobHealth>();
            if (otherHp != null && !otherHp.IsDead)
            {
                otherHp.TakeDamage(contactDamage);
                hitRoller.NotifyHitBy(this);
                damageTimer = damageCooldown;
            }
        }
        else if (hitPlayerHp != null)
        {
            hitPlayerHp.TakeDamage(contactDamage);
            damageTimer = damageCooldown;
        }

        pauseTimer = pauseAfterHit * 0.3f;
        BounceAway(hitCollider.transform.position);
        if (homeAggroOnce)
            ClearHomeAggro();
        return true;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Friendly && isAttackJump)
            TryAttackHit(collision.collider);
    }

    private bool TryCastWall(Vector2 origin, Vector2 dir, float dist, LayerMask mask, out RaycastHit2D hit)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, dist, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.GetComponentInParent<CrystalRoller>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            hit = h;
            return true;
        }
        hit = default;
        return false;
    }

    private LayerMask ScanMask()
    {
        LayerMask mask = wallMask | groundMask | obstacleMask;
        if (mask == 0) mask = -1;
        return mask;
    }

    private bool HasClearPathTo(Vector2 targetPos)
    {
        Vector2 me = Center;
        Vector2 to = targetPos - me;
        float dist = to.magnitude;
        if (dist < 0.05f) return true;

        Vector2 dir = to / dist;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float half = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        LayerMask mask = ScanMask();

        if (TryCastWall(me, dir, dist, mask, out _))
            return false;

        int blockedSides = 0;
        if (TryCastWall(me + perp * half, dir, dist, mask, out _)) blockedSides++;
        if (TryCastWall(me - perp * half, dir, dist, mask, out _)) blockedSides++;

        return blockedSides < 2;
    }

    private Vector2 FanDirection(float dirX, int index)
    {
        float t = wallRayCount > 1 ? Mathf.Clamp01((float)index / (wallRayCount - 1)) : 0f;
        float reach = maxJumpHeight * MaxJumpMargin;
        float ratio = wallRayLength > 0.001f ? Mathf.Clamp(reach / wallRayLength, 0f, 1f) : 0f;
        float topRad = Mathf.Asin(ratio);
        float rad = topRad * t;
        return new Vector2(Mathf.Cos(rad) * dirX, Mathf.Sin(rad));
    }

    private WallScanResult ScanFan(float dirX, float length, int rayCount)
    {
        WallScanResult result = new WallScanResult();
        result.closestDist = float.MaxValue;
        if (!IsGrounded())
        {
            result.hitCount = 0;
            return result;
        }
        LayerMask mask = ScanMask();
        float halfWidth = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        Vector2 origin = wallCheck != null ? (Vector2)wallCheck.position : (Vector2)transform.position;
        origin += new Vector2(dirX * halfWidth, 0f);
        float reachAbove = maxJumpHeight * MaxJumpMargin + 0.3f;

        List<Vector2> debugList = Mathf.Approximately(length, wallRayLength) ? lastScanHits : lastJumpHits;
        debugList.Clear();

        int hitCount = 0;
        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = FanDirection(dirX, i);
            RaycastHit2D hit;
            if (TryCastWall(origin, dir, length, mask, out hit))
            {
                if (Mathf.Abs(hit.normal.y) > 0.7f) continue;
                if (hit.point.y - origin.y > reachAbove) continue;
                if ((hit.point.x - origin.x) * dirX < -0.05f) continue;
                hitCount++;
                result.hit = true;
                if (hit.distance < result.closestDist)
                {
                    result.closestDist = hit.distance;
                    result.hitCollider = hit.collider;
                }

                debugList.Add(hit.point);
            }
        }

        result.hitCount = hitCount;
        return result;
    }

    private bool ShouldJumpOver(float dirX, out bool blocked, out float distToWall)
    {
        blocked = false;
        distToWall = 0f;
        if (!IsGrounded()) return false;

        WallScanResult turn = ScanFan(dirX, wallRayLength, wallRayCount);
        WallScanResult jump = ScanFan(dirX, jumpRayLength, jumpRayCount);

        if (turn.hitCount < wallHitsForTurn && jump.hitCount < wallHitsForJump)
            return false;

        WallScanResult best = jump.closestDist <= turn.closestDist ? jump : turn;

        float targetDist;
        if (!ComputeJumpClearance(best, out targetDist))
        {
            blocked = true;
            return false;
        }
        distToWall = targetDist;
        return true;
    }

    private bool ComputeJumpClearance(WallScanResult scan, out float targetDist)
    {
        targetDist = scan.closestDist;
        if (scan.hitCollider == null) return false;

        float g = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        if (g <= 0.001f) g = 9.81f;
        float riseTime = Mathf.Sqrt(2f * (maxJumpHeight + 0.1f) / g);
        float flightTime = Mathf.Max(2f * riseTime, 0.1f);

        float halfWidth = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        float halfHeight = selfCollider != null ? selfCollider.bounds.extents.y : 0.4f;

        Bounds b = scan.hitCollider.bounds;
        float obstacleTop = b.max.y;
        float feetY = transform.position.y - halfHeight;

        float needed = obstacleTop - feetY;
        if (needed > maxJumpHeight * 1.05f) return false;

        float thickness = Mathf.Abs(b.size.x);
        float centerX = scan.closestDist + thickness * 0.5f;
        float landDist = scan.closestDist + thickness + halfWidth + 0.3f;
        targetDist = Mathf.Min(Mathf.Max(2f * centerX, landDist), flightTime * 8f);
        return true;
    }

    private void LaunchJump(float dirX, float targetDist)
    {
        if (isJumping || !IsGrounded() || wasAirborne || body.linearVelocity.y > 0.01f) return;
        float g = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        if (g <= 0.001f) g = 9.81f;

        float clearHeight = maxJumpHeight + 0.1f;
        float riseTime = Mathf.Sqrt(2f * clearHeight / g);
        float flightTime = Mathf.Max(2f * riseTime, 0.1f);
        float vx = Mathf.Clamp(targetDist / flightTime, 0f, 8f) * dirX;
        float vy = riseTime * g;

        RecordJumpTrajectory(vx, vy, flightTime);
        body.linearVelocity = new Vector2(vx, vy);
        isJumping = true;
        wasAirborne = false;
        isAttackJump = false;
        jumpSafetyTimer = flightTime + 0.5f;
    }

    private void DodgeHop(float dirX)
    {
        if (isJumping || !IsGrounded() || wasAirborne || body.linearVelocity.y > 0.01f) return;
        body.linearVelocity = new Vector2(dirX * dodgeBackSpeed, dodgeJumpForce);
        isJumping = true;
        wasAirborne = false;
        isAttackJump = false;
        jumpSafetyTimer = 0.7f;
    }

    private void BounceAway(Vector2 fromPos)
    {
        float dirX = transform.position.x >= fromPos.x ? 1f : -1f;
        if (Mathf.Abs(fromPos.x - transform.position.x) < 0.15f)
            dirX = Random.value < 0.5f ? 1f : -1f;
        body.linearVelocity = new Vector2(dirX * dodgeBackSpeed * 1.3f, dodgeJumpForce);
        isJumping = true;
        wasAirborne = false;
        isAttackJump = false;
        attackTarget = null;
        jumpSafetyTimer = 0.7f;
    }

    private void RecordJumpTrajectory(float vx, float vy, float duration)
    {
        if (!IsGrounded()) return;
        float g = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        if (g <= 0.001f) g = 9.81f;

        trajectory.start = body.position;
        trajectory.vx = vx;
        trajectory.vy = vy;
        trajectory.gravity = g;
        trajectory.duration = duration;
        trajectory.active = true;
        trajectory.landFound = SimulateLanding(body.position, vx, vy, g, out trajectory.landPoint, out trajectory.landTime);
    }

    private bool SimulateLanding(Vector2 start, float vx, float vy, float g, out Vector2 landPoint, out float landTime)
    {
        landPoint = start;
        landTime = 0f;

        LayerMask mask = groundMask | obstacleMask | wallMask;
        if (mask == 0) mask = -1;
        float halfHeight = selfCollider != null ? selfCollider.bounds.extents.y : 0.4f;

        const float simDt = 0.05f;
        const float maxT = 4f;
        int steps = Mathf.CeilToInt(maxT / simDt);

        for (int s = 1; s <= steps; s++)
        {
            float t = s * simDt;
            float x = start.x + vx * t;
            float y = start.y + vy * t - 0.5f * g * t * t;

            if (vy - g * t >= 0f) continue;

            RaycastHit2D hit;
            if (TryCastWall(new Vector2(x, y), Vector2.down, halfHeight + 0.2f, mask, out hit))
            {
                landPoint = new Vector2(x, hit.point.y);
                landTime = t;
                return true;
            }

            if (y < start.y - 3f) break;
        }

        float endT = steps * simDt;
        landPoint = new Vector2(start.x + vx * endT, start.y - 2f);
        landTime = endT;
        return false;
    }

    private void ObstacleJump(float dirX, float targetDist)
    {
        LaunchJump(dirX, targetDist);
    }

    private bool CheckPit(float dirX)
    {
        float halfWidth = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        float halfHeight = selfCollider != null ? selfCollider.bounds.extents.y : 0.4f;
        Vector2 origin = (Vector2)transform.position + new Vector2(dirX * (halfWidth + 0.05f), -halfHeight - 0.05f);
        LayerMask mask = ScanMask();

        RaycastHit2D floor;
        bool floorHit = TryCastWall(origin, Vector2.down, maxFallDepth + 0.3f, groundMask, out floor);
        if (!floorHit)
            TryCastWall(origin, Vector2.down, maxFallDepth + 0.3f, mask, out floor);

        pitProbeOrigin = origin;
        lastPitAhead = !floorHit || floor.distance > maxFallDepth;
        return lastPitAhead;
    }

    private float WildVx(float dt, bool grounded)
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= dt;
            return 0f;
        }

        if (CrystalFrog.TryFrogThreat((Vector2)transform.position, frogFearDistance, out Vector3 wildFrogThreat))
        {
            frogThreatPos = wildFrogThreat;
            frogFleeTimer = frogFleeMemoryTime;
        }
        if (frogFleeTimer > 0f)
        {
            frogFleeTimer -= dt;
            return FrogFleeVx(dt, grounded);
        }

        UpdateTargeting(dt);

        if (retreating)
            return RetreatVx(dt, grounded);

        if (raidActive && raidTarget != null && raidTarget.gameObject.activeInHierarchy)
            return RaidVx(grounded);

        if (target != null)
        {
            float dx = target.position.x - transform.position.x;
            float dirX = dx > 0.15f ? 1f : (dx < -0.15f ? -1f : (float)moveDirection);
            float absDx = Mathf.Abs(dx);

            bool targetIsRoller = target.GetComponentInParent<CrystalRoller>() != null;
            if (attackTimer <= 0f && absDx <= attackJumpRange && grounded && (!targetIsRoller || targetIsFriendly))
            {
                Vector2 aim = target.position;
                Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
                if (targetBody != null)
                    aim += (Vector2)targetBody.linearVelocity * Mathf.Clamp(absDx / alertSpeed, 0.3f, 0.7f);
                AttackJump(aim);
                return dirX * alertSpeed;
            }

            return PursueVx(dirX, grounded);
        }

        TryMoveJump(grounded);
        UpdateStuckTracking(dt);
        if (breakingFree)
            return PatrolVxFree(grounded);
        float avoidVx;
        if (TryPackAvoidanceVx(out avoidVx))
            return avoidVx;
        return PatrolVx(dt, grounded);
    }

    private float RaidVx(bool grounded)
    {
        float dirX = raidTarget.Center.x - transform.position.x;
        if (Mathf.Abs(dirX) < 0.25f) dirX = moveDirection;
        else dirX = Mathf.Sign(dirX);
        moveDirection = (int)Mathf.Sign(dirX);

        if (grounded)
        {
            bool blocked;
            float distToWall;
            if (ShouldJumpOver(dirX, out blocked, out distToWall))
                ObstacleJump(dirX, distToWall);
            else if (CheckPit(dirX))
                ObstacleJump(dirX, maxFallDepth * 2f + 0.8f);
        }

        return dirX * alertSpeed;
    }

    private float RetreatVx(float dt, bool grounded)
    {
        Vector2 home = Center;
        if (Vector2.Distance(transform.position, home) <= 2f)
        {
            retreating = false;
            return RetreatPatrolVx(grounded);
        }

        float dirX = home.x - transform.position.x;
        if (Mathf.Abs(dirX) < 0.2f) dirX = 1f;
        else dirX = Mathf.Sign(dirX);
        moveDirection = (int)Mathf.Sign(dirX);

        if (grounded)
        {
            bool blocked;
            float distToWall;
            if (ShouldJumpOver(dirX, out blocked, out distToWall))
                ObstacleJump(dirX, distToWall);
            else if (CheckPit(dirX))
                ObstacleJump(dirX, maxFallDepth * 2f + 0.8f);
        }

        return dirX * fearSpeed;
    }

    private float RetreatPatrolVx(bool grounded)
    {
        if (grounded && ObstacleAhead(moveDirection))
            moveDirection = -moveDirection;

        Vector2 center = Center;
        if (transform.position.x > center.x + territoryRadius) moveDirection = -1;
        else if (transform.position.x < center.x - territoryRadius) moveDirection = 1;

        if (idleTimer <= 0f)
        {
            if (Random.value < Time.deltaTime * 0.1f)
                idleTimer = Random.Range(patrolIdleMin, patrolIdleMax);
        }
        else
        {
            idleTimer -= Time.deltaTime;
            return 0f;
        }

        return moveDirection * fearSpeed;
    }

    private void RegenHp(float dt)
    {
        if (myHealth == null || myHealth.IsDead) return;

        if (!Friendly)
        {
            bool resting = target == null && !raidActive && !raidPending &&
                           Vector2.Distance((Vector2)transform.position, Center) <= territoryRadius;
            if (!resting)
            {
                wildRegenTimer = 0f;
                return;
            }
            wildRegenTimer += dt;
            if (wildRegenTimer >= wildRegenInterval)
            {
                wildRegenTimer = 0f;
                myHealth.Heal(1);
            }
        }
        else
        {
            bool nearPlayer = player != null &&
                              Vector2.Distance(player.position, transform.position) <= PetDistance;
            if (!nearPlayer)
            {
                friendlyRegenTimer = 0f;
                return;
            }
            friendlyRegenTimer += dt;
            if (friendlyRegenTimer >= friendlyRegenInterval)
            {
                friendlyRegenTimer = 0f;
                myHealth.Heal(1);
            }
        }
    }

    private bool PackMateNear(float range)
    {
        for (int i = 0; i < pack.Count; i++)
        {
            CrystalRoller p = pack[i];
            if (p == null || p == this || !p.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance((Vector2)p.transform.position, (Vector2)transform.position) < range)
                return true;
        }
        return false;
    }

    private void UpdateStuckTracking(float dt)
    {
        float moved = Vector2.Distance(transform.position, stuckProbePos);

        if (breakingFree)
        {
            if (moved > 3f)
            {
                breakingFree = false;
                stuckProbeTimer = 0f;
                stuckProbePos = transform.position;
            }
            return;
        }

        stuckProbeTimer += dt;
        if (stuckProbeTimer >= 2.5f)
        {
            if (moved < 0.6f)
                breakingFree = true;
            stuckProbeTimer = 0f;
            stuckProbePos = transform.position;
        }
        else if (moved > 1f)
        {
            stuckProbeTimer = 0f;
            stuckProbePos = transform.position;
        }
    }

    private float PatrolVxFree(bool grounded)
    {
        if (grounded && ObstacleAhead(moveDirection))
            moveDirection = -moveDirection;
        return moveDirection * patrolSpeed;
    }

    private float PursueVx(float dirX, bool grounded)
    {
        if (grounded)
        {
            bool blocked;
            float distToWall;
            if (ShouldJumpOver(dirX, out blocked, out distToWall))
            {
                ObstacleJump(dirX, distToWall);
                return 0f;
            }
            if (blocked)
                return -dirX * alertSpeed * 0.6f;
        }
        return dirX * alertSpeed;
    }

    private void TryMoveJump(bool grounded)
    {
        if (!grounded || isJumping) return;

        float dirX = moveDirection;

        if (CheckPit(dirX))
        {
            ObstacleJump(dirX, maxFallDepth * 2f + 0.8f);
            return;
        }

        bool blocked;
        float distToWall;
        if (ShouldJumpOver(dirX, out blocked, out distToWall))
            ObstacleJump(dirX, distToWall);
        else if (blocked)
            moveDirection = -moveDirection;
    }

    private void AttackJump(Vector2 targetPos)
    {
        if (isJumping || !IsGrounded() || wasAirborne || body.linearVelocity.y > 0.01f) return;
        Vector2 start = body.position;
        Vector2 delta = targetPos - start;
        float dirX = delta.x >= 0f ? 1f : -1f;
        float absDist = Mathf.Abs(delta.x);

        float g = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        if (g <= 0.001f) g = 9.81f;

        float flightTime = Mathf.Clamp(absDist / alertSpeed, 0.3f, 0.7f);
        float vx = (absDist / flightTime) * dirX;
        float vy = (delta.y + 0.5f * g * flightTime * flightTime) / flightTime;

        RecordJumpTrajectory(vx, vy, flightTime);
        body.linearVelocity = new Vector2(vx, vy);
        isJumping = true;
        wasAirborne = false;
        isAttackJump = true;
        attackTarget = homeAggroTarget;
        attackTimer = attackCooldown;
        jumpSafetyTimer = flightTime + 0.5f;
    }

    private void UpdateTargeting(float dt)
    {
        forcedTimer -= dt;

        if (LowOnHealth())
        {
            CrystalRoller seenRival;
            if (FindRival(out seenRival))
            {
                if (raidActive) EndRaid();
                raidPending = false;
                retreating = false;
                target = seenRival.transform;
                targetIsFriendly = false;
                return;
            }

            if (raidActive) EndRaid();
            target = null;
            targetIsFriendly = false;
            forcedTarget = null;
            forcedTimer = 0f;
            retreating = true;
            return;
        }
        retreating = false;

        if (!raidActive && TargetOutOfHuntingZone())
        {
            if (target != null && !targetIsFriendly && IsWildRival(target))
                TrackRivalEscape(target.GetComponentInParent<CrystalRoller>());
            target = null;
            targetIsFriendly = false;
            forcedTarget = null;
            forcedTimer = 0f;
        }

        if (forcedTimer > 0f && forcedTarget != null && forcedTarget.gameObject.activeInHierarchy)
        {
            target = forcedTarget;
            CrystalRoller fc = forcedTarget.GetComponentInParent<CrystalRoller>();
            targetIsFriendly = fc != null && fc.Friendly;
            return;
        }

        if (target != null && !target.gameObject.activeInHierarchy)
        {
            if (raidTarget != null && target == raidTarget.transform)
                EndRaid();
            target = null;
            targetIsFriendly = false;
            forcedTarget = null;
            forcedTimer = 0f;
        }

        if (raidActive)
        {
            raidTimeout -= dt;
            if (raidTimeout <= 0f || raidTarget == null || !raidTarget.gameObject.activeInHierarchy || raidTarget.Friendly)
            {
                EndRaid();
            }
            else
            {
                target = raidTarget.transform;
                targetIsFriendly = false;
                return;
            }
        }

        if (raidPending)
        {
            if (raidTarget == null || !raidTarget.gameObject.activeInHierarchy || raidTarget.Friendly)
            {
                raidPending = false;
                raidTarget = null;
            }
            else
            {
                raidCountdown -= dt;
                if (raidCountdown <= 0f)
                    StartRaid();
            }
        }

        if (target != null)
            return;

        if (player != null)
        {
            target = player;
            targetIsFriendly = false;
            return;
        }

        CrystalRoller inZone = FindFriendlyInTerritory();
        if (inZone != null)
        {
            target = inZone.transform;
            targetIsFriendly = true;
            RallyPackTo(target, target.position);
            return;
        }

        CrystalRoller rival;
        if (FindRival(out rival))
        {
            target = rival.transform;
            targetIsFriendly = false;
            RallyPackTo(target, target.position);
            return;
        }
    }

    private bool IsWildRival(Transform t)
    {
        if (t == null) return false;
        CrystalRoller c = t.GetComponentInParent<CrystalRoller>();
        return c != null && c != this && !c.Friendly && !pack.Contains(c);
    }

    private bool Sees(CrystalRoller r)
    {
        if (r == null || r.selfCollider == null) return false;
        return inVision.Contains(r.selfCollider);
    }

    private bool LowOnHealth()
    {
        if (myHealth == null) return false;
        return myHealth.Hp <= lowHpRetreatThreshold && myHealth.Hp > 0;
    }

    private void TrackRivalEscape(CrystalRoller rival)
    {
        if (rival == null || !rival.gameObject.activeInHierarchy) return;

        float atCenter = Vector2.Distance((Vector2)rival.transform.position, Center);
        if (atCenter > maxChaseDistance + 4f) return;

        if (raidTarget == null && !raidPending)
        {
            raidTarget = rival;
            raidPending = true;
            raidCountdown = Random.Range(raidDelayMin, raidDelayMax);
        }
    }

    private void StartRaid()
    {
        raidPending = false;
        raidActive = true;
        raidTimeout = raidTimeoutTime;
        BroadcastRaid();
    }

    private void BroadcastRaid()
    {
        for (int i = 0; i < pack.Count; i++)
        {
            CrystalRoller p = pack[i];
            if (p == null || !p.gameObject.activeInHierarchy) continue;
            if (p.gameObject.activeSelf && raidTarget != null)
            {
                p.raidPending = false;
                p.raidActive = true;
                p.raidTarget = raidTarget;
                p.raidTimeout = raidTimeout;
                p.target = raidTarget.transform;
                p.targetIsFriendly = false;
            }
        }
        if (raidTarget != null)
            RallyPackTo(raidTarget.transform, raidTarget.Center);
    }

    private void EndRaid()
    {
        if (raidTarget != null && target == raidTarget.transform)
        {
            target = null;
            targetIsFriendly = false;
        }
        raidActive = false;
        raidPending = false;
        raidTarget = null;
        retreating = false;
    }

    private CrystalRoller FindFriendlyInTerritory()
    {
        float terrSqr = territoryRadius * territoryRadius;
        CrystalRoller best = null;
        float bestSqr = float.MaxValue;
        Vector2 me = (Vector2)transform.position;

        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this || !r.Friendly) continue;
            if (!r.gameObject.activeInHierarchy) continue;

            if (((Vector2)r.transform.position - Center).sqrMagnitude > terrSqr) continue;

            float toMeSqr = ((Vector2)r.transform.position - me).sqrMagnitude;
            if (toMeSqr < bestSqr)
            {
                bestSqr = toMeSqr;
                best = r;
            }
        }
        return best;
    }

    private bool HasHuntTarget()
    {
        if (target == null) return false;
        if (targetIsFriendly) return true;
        return target.GetComponentInParent<CrystalRoller>() != null;
    }

    private bool PackHasHunter()
    {
        for (int i = 0; i < pack.Count; i++)
        {
            CrystalRoller p = pack[i];
            if (p == null || p == this || !p.gameObject.activeInHierarchy) continue;
            if (p.HasHuntTarget()) return true;
        }
        return false;
    }

    private bool TargetOutOfHuntingZone()
    {
        if (raidActive) return false;
        if (target == null) return false;
        if (Vector2.Distance((Vector2)transform.position, Center) > maxChaseDistance) return true;

        float toTarget = Vector2.Distance(target.position, Center);
        if (targetIsFriendly)
        {
            bool inPatrol = toTarget <= territoryRadius;
            bool inHunt = toTarget <= maxChaseDistance;
            return !inPatrol && !inHunt;
        }
        return toTarget > maxChaseDistance;
    }

    private void RebuildTerritories()
    {
        pack.Clear();
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this || r.Friendly) continue;

            float homeDist = Vector2.Distance(homePosition, r.homePosition);
            if (homeDist <= baseTerritory + r.baseTerritory)
            {
                if (!pack.Contains(r)) pack.Add(r);
            }
        }

        if (pack.Count > 0)
        {
            Vector2 sum = ZoneSource();
            for (int i = 0; i < pack.Count; i++)
                sum += pack[i].ZoneSource();
            Vector2 sharedCenter = sum / (pack.Count + 1f);

            float radius = baseTerritory * territoryGrowth;
            float hunt = baseHunt * huntGrowth;
            for (int i = 0; i < pack.Count; i++)
            {
                float d = Vector2.Distance(sharedCenter, pack[i].ZoneSource());
                radius = Mathf.Max(radius, d + pack[i].baseTerritory * pack[i].territoryGrowth);
                hunt = Mathf.Max(hunt, d + pack[i].baseHunt * pack[i].huntGrowth);
            }

            packCenterOverride = sharedCenter;
            territoryRadius = radius;
            maxChaseDistance = hunt;
            for (int i = 0; i < pack.Count; i++)
            {
                pack[i].packCenterOverride = sharedCenter;
                pack[i].territoryRadius = radius;
                pack[i].maxChaseDistance = hunt;
            }
        }
        else
        {
            packCenterOverride = null;
            territoryRadius = baseTerritory * territoryGrowth;
            maxChaseDistance = baseHunt * huntGrowth;
        }
    }

    private bool FindRival(out CrystalRoller rival)
    {
        rival = null;
        float bestDist = float.MaxValue;
        Vector2 me = (Vector2)transform.position;

        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this || r.Friendly) continue;
            if (pack.Contains(r)) continue;
            if (!Sees(r)) continue;
            if (!HasClearPathTo((Vector2)r.transform.position)) continue;

            float d = Vector2.Distance(me, r.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                rival = r;
            }
        }
        return rival != null;
    }

    private void RallyPackTo(Transform threat, Vector2 threatPos)
    {
        for (int i = 0; i < pack.Count; i++)
        {
            CrystalRoller p = pack[i];
            if (p == null || p == this || !p.gameObject.activeInHierarchy) continue;
            if (!p.gameObject.activeSelf) continue;

            if (Vector2.Distance((Vector2)p.transform.position, threatPos) <= p.maxChaseDistance)
            {
                p.forcedTimer = Mathf.Max(p.forcedTimer, rallyMemory);
                p.forcedTarget = threat;
            }
        }
    }

    private void GrowTerritoryAfterKill(CrystalRoller victim)
    {
        territoryGrowth *= territoryGrowthOnKill;
        huntGrowth *= huntingGrowthOnKill;

        if (victim != null)
        {
            float meanDist = Vector2.Distance(Center, victim.Center);
            float needTerr = meanDist + victim.territoryRadius;
            if (baseTerritory > 0.001f)
                territoryGrowth = Mathf.Max(territoryGrowth, needTerr / baseTerritory);
            float needHunt = meanDist + victim.maxChaseDistance;
            if (baseHunt > 0.001f)
                huntGrowth = Mathf.Max(huntGrowth, needHunt / baseHunt);
        }

        RebuildTerritories();
    }

    private void ForceAggro(Transform newTarget)
    {
        forcedTarget = newTarget;
        forcedTimer = aggroOnContactMemory;
        target = newTarget;
        CrystalRoller fc = newTarget.GetComponentInParent<CrystalRoller>();
        targetIsFriendly = fc != null && fc.Friendly;
    }

    private float FriendlyVx(float dt, bool grounded)
    {
        if (petting) return PetPullVx();
        if (staying) return 0f;

        playerScared = false;

        if (CrystalFrog.TryFrogThreat((Vector2)transform.position, frogFearDistance, out Vector3 frogThreat))
        {
            frogThreatPos = frogThreat;
            frogFleeTimer = frogFleeMemoryTime;
        }
        if (frogFleeTimer > 0f)
        {
            frogFleeTimer -= dt;
            return FrogFleeVx(dt, grounded);
        }

        if (retaliateTimer > 0f && retaliateTarget != null)
        {
            retaliateTimer -= dt;
            if (!retaliateTarget.gameObject.activeInHierarchy || retaliateTimer <= 0f)
                retaliateTarget = null;
            else
            {
                StartHomeAggro(retaliateTarget, false);
                return HomeAggroVx(dt, grounded);
            }
        }

        if (dodgeCooldown > 0f) dodgeCooldown -= dt;

        if (grounded && !isJumping && dodgeCooldown <= 0f)
        {
            CrystalRoller incoming = FindIncomingAttacker();
            if (incoming != null)
            {
                float dist = Vector2.Distance((Vector2)transform.position, (Vector2)incoming.transform.position);
                if (dist <= attackJumpRange + 0.6f)
                {
                    float awayDir = incoming.transform.position.x >= transform.position.x ? -1f : 1f;
                    DodgeHop(awayDir);
                    dodgeCooldown = dodgeCooldownTime;
                    return 0f;
                }
            }
        }

        if (HasPersonality(Personality.Bravery))
        {
            bravePhaseTimer -= dt;
            if (bravePhaseTimer <= 0f)
            {
                braveAttacking = !braveAttacking;
                bravePhaseTimer = braveAttacking ? braveryStrikeInterval : braveryStrikeDuration;
            }
        }

        if (HasPersonality(Personality.Jealousy))
        {
            Transform jealousTarget = FindJealousyTarget();
            if (homeAggroTarget == null)
            {
                jealousCooldown -= dt;
                if (jealousTarget != null && jealousCooldown <= 0f)
                {
                    StartHomeAggro(jealousTarget, false);
                    jealousCooldown = jealousyRetargetCooldown;
                }
            }
            else if (!(isJumping && isAttackJump && homeAggroTarget != null) &&
                     (jealousTarget == null || homeAggroTarget != jealousTarget))
            {
                ClearHomeAggro();
            }
        }

        if (homeAggroTarget != null)
        {
            bool stop = false;
            CrystalRoller aggroRoller = homeAggroTarget.GetComponentInParent<CrystalRoller>();
            if (!homeAggroTarget.gameObject.activeInHierarchy)
                stop = true;
            else if (homeAggroOnce && aggroRoller == null &&
                     Vector2.Distance((Vector2)transform.position, (Vector2)homeAggroTarget.position) > maxChaseDistance)
                stop = true;
            else if (HasPersonality(Personality.Bravery) && !braveAttacking && aggroRoller != null && !aggroRoller.Friendly)
                stop = true;
            if (stop)
                ClearHomeAggro();
            else
                return HomeAggroVx(dt, grounded);
        }

        Transform fearFrom = FindFearSource();
        if (fearFrom != null)
        {
            rememberedThreat = (Vector2)fearFrom.position;
            threatMemoryTimer = threatMemoryTime;

            if (HasPersonality(Personality.Overbravery))
            {
                StartHomeAggro(fearFrom, false);
                return HomeAggroVx(dt, grounded);
            }

            if (HasPersonality(Personality.Bravery))
            {
                if (braveAttacking)
                {
                    StartHomeAggro(fearFrom, false);
                    return HomeAggroVx(dt, grounded);
                }
                return FleeFromThreat(dt, grounded, false);
            }

            float dirX = transform.position.x - rememberedThreat.x;
            if (Mathf.Abs(dirX) > 0.1f)
                moveDirection = dirX >= 0f ? 1 : -1;
            return FleeFromThreat(dt, grounded, false);
        }
        if (homeAggroTarget != null && (HasPersonality(Personality.Bravery) || HasPersonality(Personality.Overbravery)))
            ClearHomeAggro();

        if (HasPersonality(Personality.Cowardice) && player != null && cowardiceFear > 0f)
        {
            float fearDist = Vector2.Distance((Vector2)transform.position, (Vector2)player.position);
            if (fearDist <= fearDistance)
            {
                playerScared = true;
                rememberedThreat = (Vector2)player.position;
                threatMemoryTimer = threatMemoryTime;
                return FleeFromThreat(dt, grounded, false);
            }
        }

        if (HasPersonality(Personality.Sociophobia))
        {
            Transform nearby = FindNearbyLiving();
            if (nearby != null)
            {
                playerScared = true;
                float d = Vector2.Distance((Vector2)transform.position, (Vector2)nearby.position);
                if (d <= PetDistance)
                {
                    rememberedThreat = (Vector2)nearby.position;
                    threatMemoryTimer = threatMemoryTime;
                    return FleeFromThreat(dt, grounded, false);
                }
                return 0f;
            }
        }

        if (threatMemoryTimer > 0f)
        {
            threatMemoryTimer -= dt;
            return FleeFromThreat(dt, grounded, true);
        }

        stamina = Mathf.Min(staminaMax, stamina + dt * staminaRegenPerSec);

        if (player != null)
        {
            float dx = player.position.x - transform.position.x;
            float dirX = dx >= 0f ? 1f : -1f;
            if (Mathf.Abs(dx) <= interactRadius) return 0f;

            return PursueVx(dirX, grounded);
        }

        if (avoidTimer > 0f)
        {
            avoidTimer -= dt;
            if (grounded && ObstacleAhead(avoidDirection))
                avoidDirection = -avoidDirection;
            moveDirection = avoidDirection;
        }

        TryMoveJump(grounded);
        return moveDirection * patrolSpeed;
    }

    private bool IsSameActor(Transform a, Transform b)
    {
        if (a == null || b == null) return false;
        return a == b || a.IsChildOf(b) || b.IsChildOf(a);
    }

    private void StartHomeAggro(Transform target, bool once)
    {
        if (target == null) return;
        homeAggroTarget = target;
        homeAggroOnce = once;
    }

    private void ClearHomeAggro()
    {
        homeAggroTarget = null;
        homeAggroOnce = false;
    }

    private void NotifyHitBy(CrystalRoller attacker)
    {
        if (!Friendly || attacker == null || attacker == this) return;
        retaliateTarget = attacker.transform;
        retaliateTimer = retaliationDuration;
    }

    private CrystalRoller FindIncomingAttacker()
    {
        float closest = attackJumpRange + 1f;
        CrystalRoller found = null;
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this) continue;
            if (!r.gameObject.activeInHierarchy) continue;

            bool aimsAtMe = r.isAttackJump &&
                ((r.attackTarget != null && IsSameActor(r.attackTarget, transform)) ||
                 (r.target != null && IsSameActor(r.target, transform)));
            bool targetsMe = r.homeAggroTarget != null && IsSameActor(r.homeAggroTarget, transform);
            if (!aimsAtMe && !targetsMe) continue;

            float d = Vector2.Distance((Vector2)transform.position, (Vector2)r.transform.position);
            if (d < closest)
            {
                closest = d;
                found = r;
            }
        }
        return found;
    }

    private Transform FindJealousyTarget()
    {
        if (player == null) return null;
        Transform closest = null;
        float closestDist = float.MaxValue;
        Vector2 playerPos = (Vector2)player.position;
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this || !r.Friendly) continue;
            if (!r.gameObject.activeInHierarchy) continue;
            float dPlayer = Vector2.Distance((Vector2)r.transform.position, playerPos);
            if (dPlayer > jealousyRadius) continue;
            float d = Vector2.Distance((Vector2)transform.position, (Vector2)r.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = r.transform;
            }
        }
        return closest;
    }

    private Transform FindNearbyLiving()
    {
        Transform closest = null;
        float closestDist = float.MaxValue;
        Vector2 pos = (Vector2)transform.position;

        if (player != null)
        {
            float d = Vector2.Distance(pos, (Vector2)player.position);
            if (d <= sociophobiaRadius)
            {
                closestDist = d;
                closest = player;
            }
        }

        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this) continue;
            if (!r.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance(pos, (Vector2)r.transform.position);
            if (d <= sociophobiaRadius && d < closestDist)
            {
                closestDist = d;
                closest = r.transform;
            }
        }
        return closest;
    }

    private float HomeAggroVx(float dt, bool grounded)
    {
        if (homeAggroTarget == null)
        {
            ClearHomeAggro();
            return 0f;
        }

        float dx = homeAggroTarget.position.x - transform.position.x;
        float dirX = dx >= 0f ? 1f : -1f;
        float absDx = Mathf.Abs(dx);

        if (grounded && attackTimer <= 0f && absDx <= attackJumpRange)
        {
            Vector2 aim = homeAggroTarget.position;
            Rigidbody2D targetBody = homeAggroTarget.GetComponent<Rigidbody2D>();
            if (targetBody != null)
                aim += (Vector2)targetBody.linearVelocity * Mathf.Clamp(absDx / alertSpeed, 0.3f, 0.7f);
            AttackJump(aim);
            return dirX * alertSpeed;
        }

        bool blocked = false;
        float distToWall = 0f;
        if (grounded && ShouldJumpOver(dirX, out blocked, out distToWall))
        {
            ObstacleJump(dirX, distToWall);
            return 0f;
        }
        if (blocked && grounded)
            return -dirX * alertSpeed * 0.6f;
        return dirX * alertSpeed;
    }

    private float FleeFromThreat(float dt, bool grounded, bool mayStopOnDistance)
    {
        float dx = transform.position.x - rememberedThreat.x;
        float dirX = dx >= 0f ? 1f : -1f;

        float dist = Vector2.Distance(transform.position, rememberedThreat);
        if (mayStopOnDistance && dist > fearDistance)
            return 0f;

        stamina = Mathf.Max(0f, stamina - dt * staminaDrainPerSec);
        float runSpeed = stamina > 0f ? fearSpeed : exhaustedSpeed;

        if (grounded)
        {
            bool blocked;
            float distToWall;
            if (ShouldJumpOver(dirX, out blocked, out distToWall) && !wasAirborne && body.linearVelocity.y <= 0.01f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x * 0.5f, fearJumpForce);
                isJumping = true;
                wasAirborne = false;
                jumpSafetyTimer = 1f;
                return 0f;
            }

            bool pathBlocked = blocked || CheckPit(dirX);
            if (pathBlocked)
            {
                ObstacleJump(-dirX, Mathf.Min(Mathf.Max(dist + 1.5f, 3f), 4f));
                return 0f;
            }
        }

        return dirX * runSpeed;
    }

    private float FrogFleeVx(float dt, bool grounded)
    {
        float dx = transform.position.x - frogThreatPos.x;
        float dirX = dx >= 0f ? 1f : -1f;

        if (grounded)
        {
            bool blocked;
            float distToWall;
            if (ShouldJumpOver(dirX, out blocked, out distToWall) && !wasAirborne && body.linearVelocity.y <= 0.01f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x * 0.5f, fearJumpForce);
                isJumping = true;
                wasAirborne = false;
                jumpSafetyTimer = 1f;
                return 0f;
            }
        }

        return dirX * frogFleeSpeed;
    }

    private Transform FindFearSource()
    {
        Transform closest = null;
        float closestDist = float.MaxValue;
        Transform selfTr = transform;

        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller r = Registry[i];
            if (r == null || r == this || r.Friendly) continue;
            if (r.target != selfTr || !r.targetIsFriendly) continue;
            if (!HasClearPathTo((Vector2)r.transform.position)) continue;

            float dist = Vector2.Distance(transform.position, r.transform.position);
            if (dist <= fearDistance && dist < closestDist)
            {
                closestDist = dist;
                closest = r.transform;
            }
        }
        return closest;
    }

    private float PatrolVx(float dt, bool grounded)
    {
        if (grounded && ObstacleAhead(moveDirection))
            moveDirection = -moveDirection;

        Vector2 center = Center;
        float bound = HasPatrolRival() ? maxChaseDistance : territoryRadius;
        if (transform.position.x > center.x + bound) moveDirection = -1;
        else if (transform.position.x < center.x - bound) moveDirection = 1;

        if (idleTimer <= 0f)
        {
            if (Random.value < dt * 0.25f)
            {
                idleTimer = Random.Range(patrolIdleMin, patrolIdleMax);
            }
            else if (Random.value < dt * 0.04f && !PackMateNear(packSeparation))
            {
                moveDirection = -moveDirection;
            }
        }
        else
        {
            idleTimer -= dt;
            return 0f;
        }

        return moveDirection * patrolSpeed;
    }

    private bool HasPatrolRival()
    {
        CrystalRoller dummy;
        return FindRival(out dummy);
    }

    private bool TryPackAvoidanceVx(out float vx)
    {
        vx = 0f;
        float bestD = packSeparation;
        CrystalRoller nearest = null;
        Vector2 pos = (Vector2)transform.position;

        for (int i = 0; i < pack.Count; i++)
        {
            CrystalRoller p = pack[i];
            if (p == null || p == this || !p.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance((Vector2)p.transform.position, pos);
            if (d < bestD)
            {
                bestD = d;
                nearest = p;
            }
        }

        if (nearest == null) return false;

        float dx = transform.position.x - nearest.transform.position.x;
        float dy = Mathf.Abs(transform.position.y - nearest.transform.position.y);
        if (dy > 1.5f) return false;
        if (Mathf.Abs(dx) < 0.25f) return false;

        float dir = dx >= 0f ? 1f : -1f;
        vx = dir * (patrolSpeed + (packSeparation - bestD) * 3f);
        return true;
    }

    private void UpdateSpikes()
    {
        if (spikes == null) return;

        RollerState newState = CurrentRollerState();
        if (newState != state)
            state = newState;

        float targetScale;
        switch (state)
        {
            case RollerState.Attack:
                targetScale = spikeScaleAttack;
                break;
            case RollerState.Chase:
                targetScale = spikeScaleChase;
                break;
            default:
                targetScale = spikeScalePatrol;
                break;
        }

        float current = spikes.localScale.x;
        float next = Mathf.Lerp(current, targetScale, spikeLerpSpeed * Time.deltaTime);
        spikes.localScale = new Vector3(next, next, next);
    }

    private RollerState CurrentRollerState()
    {
        if (isAttackJump) return RollerState.Attack;
        if (target != null || homeAggroTarget != null) return RollerState.Chase;
        if (isCrashed) return RollerState.Crashed;
        if (Friendly && staying) return RollerState.Staying;
        return RollerState.Patrol;
    }

    private bool ObstacleAhead(int dirX)
    {
        float halfWidth = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        float halfHeight = selfCollider != null ? selfCollider.bounds.extents.y : 0.4f;
        Vector2 position = transform.position;
        LayerMask mask = ScanMask();

        Vector2 wallOrigin = position + new Vector2(dirX * (halfWidth + 0.05f), 0f);
        RaycastHit2D wall;
        if (TryCastWall(wallOrigin, Vector2.right * dirX, 0.3f, mask, out wall))
            return true;

        Vector2 pitOrigin = position + new Vector2(dirX * halfWidth, -halfHeight - 0.05f);
        RaycastHit2D pit;
        if (!TryCastWall(pitOrigin, Vector2.down, maxFallDepth + 0.1f, groundMask, out pit))
            return true;
        return pit.distance > maxFallDepth;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(groundCheck.position, Vector2.down, groundCheckDistance, groundMask);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            Transform t = h.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<CrystalRoller>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            return true;
        }
        return false;
    }

    public bool TryBeginPet(CharacterBody body)
    {
        if (!Friendly || body == null) return false;
        if (!BodyNearForPet(body)) return false;

        if (interactionSettings != null && !interactionSettings.Allows(body, MergeController.Instance))
            return false;

        if (HasPersonality(Personality.Sociophobia))
        {
            if (!sociophobiaHitOnce)
            {
                sociophobiaHitOnce = true;
                StartHomeAggro(body.transform, true);
            }
            return false;
        }

        if (audioSource != null && petSound != null)
            audioSource.PlayOneShot(petSound);
        happyBlinkTimer = 1f;

        if (HasPersonality(Personality.Cowardice))
            cowardiceFear = Mathf.Max(0f, cowardiceFear - cowardicePatReduction);

        if (staying)
        {
            staying = false;
            patCount = 0;
        }
        else
        {
            patCount++;
            int sleepPats = HasPersonality(Personality.Tactile) ? tactileSleepPats : 2;
            if (patCount >= sleepPats)
            {
                staying = true;
                patCount = 0;
            }
        }

        petSource = body;
        petting = true;
        petTimer = petDuration;

        if (characterAnimator != null)
            characterAnimator.SetTrigger(petTrigger);

        return true;
    }

    private bool BodyNearForPet(CharacterBody body)
    {
        if (body == null) return false;
        return Vector2.Distance(body.Position, transform.position) <= PetDistance;
    }

    private float PetPullVx()
    {
        if (petSource == null || !petSource.gameObject.activeInHierarchy)
        {
            petting = false;
            petSource = null;
            return 0f;
        }
        float dx = petSource.Position.x - transform.position.x;
        if (Mathf.Abs(dx) <= petAttractDistance) return 0f;
        return Mathf.Sign(dx) * petAttractSpeed;
    }

    private void UpdateVisual(float dt)
    {
        if (visual == null) return;

        float vx = body.linearVelocity.x;
        int newFacing = facing;
        if (vx > 0.05f) newFacing = 1;
        else if (vx < -0.05f) newFacing = -1;

        if (newFacing != facing)
        {
            yaw += 180f;
            facing = newFacing;
        }

        if (rollRadius > 0.001f && Mathf.Abs(vx) > 0.05f)
        {
            float worldDeg = (-(vx / rollRadius) * Mathf.Rad2Deg) * dt;
            if (facing < 0) worldDeg = -worldDeg;
            rollAngle += worldDeg;
        }

        visual.localRotation = Quaternion.Euler(0f, yaw, rollAngle);
    }

    private void UpdatePupil(float dt)
    {
        if (pupil == null) return;

        pupil.rotation = pupilBaseWorldRot;

        Vector2 aimWorld = ComputePupilAim();
        Vector2 offset = aimWorld.sqrMagnitude > 0.0001f ? aimWorld * pupilMaxOffset : Vector2.zero;

        Quaternion parentRot = pupil.parent != null ? pupil.parent.rotation : transform.rotation;
        Vector3 localTarget = Quaternion.Inverse(parentRot) * (Vector3)offset;

        pupil.localPosition = Vector2.Lerp((Vector2)pupil.localPosition, (Vector2)localTarget, pupilMoveSpeed * dt);

        if (pupilRenderer == null) return;

        bool scared = Friendly && (IsFleeing() || playerScared || homeAggroTarget != null || frogFleeTimer > 0f);
        bool aggressive = !Friendly && target != null;
        bool happy = Friendly && player != null && !scared && !staying;

        float targetScale = Friendly
            ? (scared ? homePupilSizeScared : happy ? homePupilSizeHappy : homePupilSizeNormal)
            : (LowOnHealth() || frogFleeTimer > 0f ? wildPupilSizeFear : 1f);

        float scale = Mathf.Lerp(pupilRenderer.transform.localScale.x, targetScale, pupilMoveSpeed * dt);
        pupilRenderer.transform.localScale = new Vector3(scale, scale, scale);

        Sprite desired = null;
        if (Friendly)
        {
            if (scared && homePupilScared != null) desired = homePupilScared;
            else if (happy && homePupilHappy != null) desired = homePupilHappy;
            else desired = homePupilNormal;
        }
        else
        {
            if (aggressive && wildPupilAggressive != null) desired = wildPupilAggressive;
            else desired = wildPupilNormal;
        }
        if (desired != null && pupilRenderer.sprite != desired)
            pupilRenderer.sprite = desired;

        if (scared && homePupilTrembleAmount > 0f)
        {
            float tx = Mathf.Sin(Time.time * 42f) * homePupilTrembleAmount;
            float ty = Mathf.Sin(Time.time * 51f + 1.7f) * homePupilTrembleAmount;
            Vector2 p = (Vector2)pupil.localPosition + new Vector2(tx, ty);
            pupil.localPosition = Vector2.ClampMagnitude(p, pupilMaxOffset);
        }
    }

    private Vector2 ComputePupilAim()
    {
        Vector2 me = (Vector2)transform.position;

        if (isJumping && trajectory.active)
        {
            Vector2 land = trajectory.landFound ? trajectory.landPoint : me + body.linearVelocity;
            Vector2 d = land - me;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        if (target != null)
        {
            Vector2 d = (Vector2)target.position - me;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        if (Friendly && homeAggroTarget != null)
        {
            Vector2 d = (Vector2)homeAggroTarget.position - me;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        if (Friendly && player != null)
        {
            Vector2 d = (Vector2)player.position - me;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        if (Mathf.Abs(body.linearVelocity.x) > 0.05f)
            return new Vector2(Mathf.Sign(body.linearVelocity.x), 0f);

        return Vector2.zero;
    }

    private bool IsFleeing()
    {
        if (!Friendly) return false;
        Transform fearFrom = FindFearSource();
        return fearFrom != null &&
               Vector2.Distance(transform.position, fearFrom.position) <= fearDistance;
    }

    private void UpdateEyelids(float dt)
    {
        EyelidState desired = EyelidState.None;

        if (Friendly)
        {
            if (staying)
                desired = EyelidState.Closed;
            else if (happyBlinkTimer > 0f)
            {
                happyBlinkTimer -= dt;
                desired = EyelidState.HappyClosed;
            }
            else if (PlayerNearForPet())
                desired = EyelidState.HappyOpen;
        }

        ApplyEyelidState(desired);
    }

    private void ApplyEyelidState(EyelidState desired)
    {
        if (eyelidState == desired) return;
        eyelidState = desired;

        SetEyelid(desired == EyelidState.None ? null :
                  desired == EyelidState.HappyOpen ? eyelidsHappyOpen :
                  desired == EyelidState.HappyClosed ? eyelidsHappyClosed :
                  eyelidsClosed);
    }

    private void SetEyelid(GameObject active)
    {
        if (eyelidsHappyOpen != null && eyelidsHappyOpen != active)
            eyelidsHappyOpen.SetActive(false);
        if (eyelidsHappyClosed != null && eyelidsHappyClosed != active)
            eyelidsHappyClosed.SetActive(false);
        if (eyelidsClosed != null && eyelidsClosed != active)
            eyelidsClosed.SetActive(false);

        if (active != null)
            active.SetActive(true);
    }

    private void StabilizeEyelidRoot()
    {
        if (eyelidRoot == null) return;
        eyelidRoot.rotation = eyelidRootBaseWorldRot;
    }

    private void RefreshCollisionPairs()
    {
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller a = Registry[i];
            if (a == null || a.selfCollider == null) continue;
            for (int j = i + 1; j < Registry.Count; j++)
            {
                CrystalRoller b = Registry[j];
                if (b == null || b.selfCollider == null) continue;

                bool hostile = a.IsHostileWith(b) || b.IsHostileWith(a);
                hostile = hostile || a.HasAggroOn(b) || b.HasAggroOn(a);
                Physics2D.IgnoreCollision(a.selfCollider, b.selfCollider, !hostile);
            }
        }
    }

    private bool HasAggroOn(CrystalRoller other)
    {
        if (other == null || other == this) return false;
        if (homeAggroTarget != null && IsSameActor(homeAggroTarget, other.transform)) return true;
        if (isAttackJump && attackTarget != null && IsSameActor(attackTarget, other.transform)) return true;
        return false;
    }

    private void RefreshPlayerCollision()
    {
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller c = Registry[i];
            if (c == null || c.selfCollider == null) continue;

            for (int k = c.inVision.Count - 1; k >= 0; k--)
            {
                Collider2D col = c.inVision[k];
                if (col == null) { c.inVision.RemoveAt(k); continue; }

                PlayerHealth hp = c.FindPlayerHealth(col.gameObject);
                if (hp == null) continue;

                bool ignore = c.Friendly;
                if (c.Friendly && c.staying) ignore = false;
                if (c.Friendly && c.homeAggroTarget != null && IsSameActor(c.homeAggroTarget, col.transform)) ignore = false;

                Physics2D.IgnoreCollision(c.selfCollider, col, ignore);
            }
        }
    }

    private bool IsHostileWith(CrystalRoller other)
    {
        if (other == null || other == this) return false;

        if (Friendly && other.Friendly) return false;

        if (!Friendly && !other.Friendly)
            return !pack.Contains(other);

        return true;
    }

    private void ApplyDepthLayers()
    {
        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller a = Registry[i];
            if (a == null) continue;
            a.ResetDepthBase();
        }

        for (int i = 0; i < Registry.Count; i++)
        {
            CrystalRoller a = Registry[i];
            if (a == null || a.selfCollider == null) continue;
            for (int j = i + 1; j < Registry.Count; j++)
            {
                CrystalRoller b = Registry[j];
                if (b == null || b.selfCollider == null) continue;

                bool hostile = a.IsHostileWith(b) || b.IsHostileWith(a);
                if (hostile) continue;
                if (!Overlapping(a, b)) continue;

                CrystalRoller front = a.transform.position.y >= b.transform.position.y ? a : b;
                front.ApplyFrontBoost();
            }
        }
    }

    private void ResetDepthBase()
    {
        if (modelRenderers != null)
            for (int i = 0; i < modelRenderers.Length; i++)
                if (modelRenderers[i] != null)
                    modelRenderers[i].sortingOrder = baseOrders[i];
    }

    private void ApplyFrontBoost()
    {
        if (modelRenderers != null)
            for (int i = 0; i < modelRenderers.Length; i++)
                if (modelRenderers[i] != null)
                    modelRenderers[i].sortingOrder += passSortingBoost;
    }

    private bool Overlapping(CrystalRoller a, CrystalRoller b)
    {
        float halfA = a.selfCollider.bounds.extents.x;
        float halfB = b.selfCollider.bounds.extents.x;
        float dist = Vector2.Distance(a.transform.position, b.transform.position);
        return dist < halfA + halfB;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying
            ? Center
            : (Vector2)(patrolCenter != null ? patrolCenter.position : transform.position);

        if (!Friendly)
        {
            Gizmos.color = Color.yellow;
            DrawWireCircle(center, territoryRadius);

            Gizmos.color = Color.red;
            DrawWireCircle(transform.position, attackJumpRange);

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
            DrawWireCircle(center, maxChaseDistance);
        }

        Gizmos.color = Color.cyan;
        DrawWireCircle(transform.position, viewDistance);

        float dir = Application.isPlaying ? moveDirection : 1f;
        Vector2 wCheckPos = wallCheck != null ? (Vector2)wallCheck.position : (Vector2)transform.position;

        for (int i = 0; i < wallRayCount; i++)
        {
            Vector2 rayDir = FanDirection(dir, i);
            Gizmos.color = i >= wallRayCount - 1 ? Color.green : Color.white;
            Gizmos.DrawLine(wCheckPos, wCheckPos + rayDir * wallRayLength);
            Gizmos.DrawLine(wCheckPos, wCheckPos + new Vector2(-rayDir.x, rayDir.y) * wallRayLength);
        }

        for (int i = 0; i < jumpRayCount; i++)
        {
            Vector2 rayDir = FanDirection(dir, i);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(wCheckPos, wCheckPos + rayDir * jumpRayLength);
            Gizmos.DrawLine(wCheckPos, wCheckPos + new Vector2(-rayDir.x, rayDir.y) * jumpRayLength);
        }

        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
        Gizmos.DrawLine(wCheckPos, wCheckPos + new Vector2(0f, maxJumpHeight));

        if (groundCheck != null)
        {
            bool grounded = IsGrounded();
            Gizmos.color = grounded ? Color.green : Color.red;
            Vector3 start = groundCheck.position;
            Vector3 end = start + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(start, Vector3.one * 0.12f);
            Gizmos.DrawWireSphere(end, 0.1f);
        }

        if (Application.isPlaying)
        {
            if (trajectory.active)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
                const int pts = 24;
                float dur = trajectory.landFound ? trajectory.landTime : trajectory.duration;
                Vector2 prev = trajectory.start;
                Vector2 last = prev;
                for (int i = 1; i <= pts; i++)
                {
                    float t = i / (float)pts * dur;
                    float x = trajectory.start.x + trajectory.vx * t;
                    float y = trajectory.start.y + trajectory.vy * t - 0.5f * trajectory.gravity * t * t;
                    last = new Vector2(x, y);
                    Gizmos.DrawLine(prev, last);
                    prev = last;
                }

                Gizmos.color = new Color(1f, 0f, 1f, 1f);
                Gizmos.DrawWireSphere(trajectory.landPoint, 0.18f);
                Gizmos.DrawLine(last, trajectory.landPoint);
            }

            if (lastPitAhead)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(pitProbeOrigin, pitProbeOrigin + Vector2.down * (maxFallDepth + 0.3f));
                Gizmos.DrawWireSphere(pitProbeOrigin, 0.1f);
            }

            Gizmos.color = Color.red;
            for (int k = 0; k < lastScanHits.Count; k++)
                Gizmos.DrawWireSphere(lastScanHits[k], 0.08f);

            Gizmos.color = new Color(1f, 0.55f, 0f);
            for (int k = 0; k < lastJumpHits.Count; k++)
                Gizmos.DrawWireSphere(lastJumpHits[k], 0.08f);
        }

        if (Friendly)
        {
            Gizmos.color = Color.magenta;
            DrawWireCircle(transform.position, interactRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            DrawWireCircle(transform.position, petRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            DrawWireCircle(transform.position, fearDistance);
        }
    }

    private static void DrawWireCircle(Vector2 center, float radius)
    {
        const int segments = 40;
        Vector3 previous = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 point = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
