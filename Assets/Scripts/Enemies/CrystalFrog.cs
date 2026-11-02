using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CrystalFrog : MonoBehaviour
{
    [Header("Режим (true = Матерь кристаляшек)")]
    [SerializeField] private bool isMother;

    [Header("Модель (для разворота)")]
    [SerializeField] private Transform model;

    [Header("Прыжок")]
    [SerializeField] private float jumpInterval = 3f;
    [SerializeField] private float motherJumpInterval = 2f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpDistance = 3.2f;
    [SerializeField] private float maxHopSpeed = 8f;
    [SerializeField] private float retreatHopDist = 1.4f;
    [SerializeField] private float backOffTime = 0.9f;
    [SerializeField] private float motherHeightMult = 1.6f;
    [SerializeField] private float motherDistanceMult = 1.5f;

    [Header("Зрение и дальность")]
    [SerializeField] private float senseRange = 14f;
    [SerializeField] private float tongueRange = 7f;

    [Header("Язык (LineRenderer)")]
    [SerializeField] private LineRenderer tongueLine;
    [SerializeField] private Transform mouth;
    [SerializeField] private float tongueSpeed = 13f;
    [SerializeField] private float tongueWidth = 0.04f;
    [SerializeField] private float tongueAccelTime = 0.12f;
    [SerializeField] private float tongueRetractSlowdown = 0.4f;
    [SerializeField] private float tongueRadius = 0.15f;
    [SerializeField] private float tongueMaxLength = 9f;
    [SerializeField] private float tongueLifetime = 2f;
    [SerializeField] private float channelTime = 1f;
    [SerializeField] private float tongueCooldownTime = 3f;
    [SerializeField] private float pullSpeed = 2.2f;
    [SerializeField] private float pullRate = 5f;
    [SerializeField] private int playerBiteDamage = 1;

    [Header("Хват рта (урон наносит MouthDamageTrigger)")]
    [SerializeField] private Collider2D grabGhostCollider;
    [SerializeField] private bool requireMouthAnimation = true;
    [SerializeField] private float releaseDuration = 1.5f;
    [SerializeField] private float openMouthDur = 0.4f;
    [SerializeField] private bool debugLog = false;

    [Header("Притягивание жертвы")]
    [SerializeField] private float pullReachRange = 0.5f;
    [SerializeField] private float pullTimeout = 1.5f;

    [Header("Отпускание по суммарному урону")]
    [SerializeField] private int releaseAfterTotalDamage = 2;

    [Header("Урон при приземлении (Матерь)")]
    [SerializeField] private int motherLandingDamage = 1;

    [Header("Спавн кристаляшек (Матерь)")]
    [SerializeField] private float spawnInterval = 60f;
    [SerializeField] private float spawnAttackInterval = 30f;
    [SerializeField] private int spawnCount = 2;
    [SerializeField] private float spawnRadius = 1.5f;
    [SerializeField] private float spawnRetryDelay = 0.5f;
    [SerializeField] private GameObject normalFrogPrefab;

    [Header("Проверки")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float maxFallDepth = 0.9f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float blockedEscapeTime = 1.5f;

    [Header("Остановка после прыжка")]
    [SerializeField] private float minPostJumpPause = 5f;
    [SerializeField] private float maxPostJumpPause = 10f;
    private float nextPostJumpPause;

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tongueSound;

    [Header("Анимации (имена параметров аниматора)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string jumpAnimParam = "Jump";
    [SerializeField] private string fallAnimParam = "Fall";
    [SerializeField] private string landAnimParam = "Land";
    [SerializeField] private string openMouthAnimParam = "OpenMouth";
    [SerializeField] private string closeMouthAnimParam = "CloseMouth";
    [SerializeField] private string idleAnimParam = "Idle";
    [SerializeField] private string idleOpenMouthAnimParam = "IdleOpenMouth";
    [SerializeField] private string birthAnimParam = "Birth";

    private enum TongueState
    {
        Ready,
        Extending,
        Pulling,
        Retracting
    }

    private static readonly List<CrystalFrog> FrogRegistry = new List<CrystalFrog>();

    private Rigidbody2D body;
    private Collider2D selfCollider;

    private float jumpTimer;
    private bool isJumping;
    private bool wasAirborne;
    private bool landingDamageDone;
    private bool facingRight = true;

    private Transform target;

    private bool channeling;
    private float channelTimer;
    private Vector2 channelAim;
    private float tongueCooldown;
    private float senseTimer;
    private float blockedEscapeTimer;
    private float spawnTimer;
    private int moveDirection = 1;

    private bool trjActive;
    private Vector2 trjStart;
    private float trjVx;
    private float trjVy;
    private float trjG;
    private float trjDuration;
    private bool trjLandFound;
    private float trjLandTime;
    private Vector2 trjLandPoint;
    private float plannedHopDist;

    private TongueState tongueState = TongueState.Ready;
    private bool homingTongue;
    private Transform homingTarget;
    private Vector2 tongueAim;
    private Vector2 tongueRoot;
    private Vector2 tongueTip;
    private Vector2 tongueDir;
    private float tongueTimer;

    private bool grabbedIsPlayer;
    private PlayerHealth grabbedHp;
    private CharacterBody grabbedBody;
    private CrystalRoller grabbedRoller;
    private Rigidbody2D grabbedRb;
    private Collider2D grabbedCollider;
    private float grabbedGravityScale;

    private bool needsCollisionRestore;
    private Collider2D pendingRestoreCollider;

    private bool mouthOpen;
    private float releaseCooldown;
    private float pullStuckTimer;
    private int grabDamageDealt;
    private bool releasedThisGrab;
    private float openMouthTimer;

    private PlayerHealth pendingHp;
    private CrystalRoller pendingRoller;
    private Collider2D pendingCollider;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        selfCollider = grabGhostCollider != null ? grabGhostCollider : GetComponent<Collider2D>();
        jumpTimer = JumpInterval() * Random.Range(0.3f, 0.7f);
        spawnTimer = spawnInterval;
        nextPostJumpPause = minPostJumpPause;

        if (tongueLine == null)
            tongueLine = GetComponentInChildren<LineRenderer>();
        InitTongueLine();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        FrogRegistry.Add(this);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        IgnoreAllFrogCollisions();
    }

    private void OnDisable()
    {
        FrogRegistry.Remove(this);
    }

    public static bool TryFrogThreat(Vector2 from, float maxDist, out Vector3 threatPos)
    {
        threatPos = Vector3.zero;
        CrystalFrog best = null;
        float bestSqr = maxDist * maxDist;
        for (int i = 0; i < FrogRegistry.Count; i++)
        {
            CrystalFrog f = FrogRegistry[i];
            if (f == null || !f.gameObject.activeInHierarchy) continue;

            float dSqr = ((Vector2)f.transform.position - from).sqrMagnitude;
            if (dSqr < bestSqr)
            {
                bestSqr = dSqr;
                best = f;
            }
        }
        if (best == null) return false;
        threatPos = best.transform.position;
        return true;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool grounded = IsGrounded();

        if (isJumping)
        {
            if (!grounded) wasAirborne = true;
            if (wasAirborne && grounded)
            {
                isJumping = false;
                wasAirborne = false;
                trjActive = false;
                jumpTimer = nextPostJumpPause;
                nextPostJumpPause = nextPostJumpPause >= maxPostJumpPause ? minPostJumpPause : nextPostJumpPause + 1f;
                if (!landingDamageDone)
                {
                    landingDamageDone = true;
                    AnimTrigger(landAnimParam);
                    ApplyMotherLandingDamage();
                }
            }
        }
        else
        {
            wasAirborne = !grounded;
            landingDamageDone = false;
        }

        bool tongueBusy = tongueState != TongueState.Ready;
        if (tongueBusy)
            UpdateTongue(dt);

        if (tongueCooldown > 0f) tongueCooldown -= dt;
        if (jumpTimer > 0f) jumpTimer -= dt;

        if (releaseCooldown > 0f)
        {
            releaseCooldown -= dt;
            if (releaseCooldown <= 0f)
                releasedThisGrab = false;
        }

        senseTimer -= dt;
        if (blockedEscapeTimer > 0f)
            blockedEscapeTimer -= dt;
        if (senseTimer <= 0f)
        {
            senseTimer = 0.25f;
            SenseTarget();
        }

        float vx = 0f;

        if (target != null && TargetAlive())
        {
            float dist = Vector2.Distance((Vector2)transform.position, (Vector2)target.position);
            float reach = TongueReach();

            if (dist <= reach)
            {
                if (!tongueBusy && !channeling && tongueCooldown <= 0f && releaseCooldown <= 0f
                    && grounded && !isJumping)
                {
                    channeling = true;
                    channelTimer = channelTime;
                    channelAim = (Vector2)target.position;
                }

                if (channeling)
                {
                    channelTimer -= dt;
                    if (channelTimer <= 0f)
                    {
                        StartTongue();
                        channeling = false;
                    }
                }
                else if (tongueBusy)
                {
                    vx = 0f;
                }
                else if (grounded && !isJumping && jumpTimer <= 0f)
                {
                    float hopDir = target.position.x >= transform.position.x ? 1f : -1f;
                    HandleWallAndHop(hopDir, JumpDistance());
                    jumpTimer = JumpInterval();
                }
            }
            else
            {
                channeling = false;
                if (tongueBusy)
                {
                    vx = 0f;
                }
                else if (grounded && !isJumping && jumpTimer <= 0f)
                {
                    float dirX = target.position.x >= transform.position.x ? 1f : -1f;
                    HandleWallAndHop(dirX, JumpDistance());
                    jumpTimer = JumpInterval();
                }
                else if (!isJumping)
                {
                    vx = TargetDirX() * (isMother ? 1.5f : 1f);
                }
            }
        }
        else
        {
            channeling = false;
            target = null;

            if (!tongueBusy && grounded && !isJumping)
            {
                WallAction act = CheckJumpAhead(moveDirection);
                if (act == WallAction.Turn)
                {
                    EscapeFromWall(moveDirection);
                    jumpTimer = JumpInterval();
                }
                else if (act == WallAction.Jump)
                {
                    Hop(moveDirection, plannedHopDist);
                    jumpTimer = JumpInterval();
                }
                else if (act == WallAction.Back)
                {
                    RetreatFromWall(moveDirection);
                    jumpTimer = JumpInterval();
                }
                else if (ObstacleAhead(moveDirection))
                {
                    HandleWallAndHop(moveDirection, JumpDistance());
                    jumpTimer = JumpInterval();
                }
                else if (jumpTimer <= 0f)
                {
                    Hop(moveDirection, plannedHopDist);
                    jumpTimer = JumpInterval();
                }
                else
                {
                    vx = moveDirection * 0.5f;
                }
            }
        }

        if (jumpTimer > 0f)
            vx = 0f;

        if (!isJumping)
            body.linearVelocity = new Vector2(tongueBusy ? 0f : vx, body.linearVelocity.y);

        float faceX = vx;
        if (!IsPulling())
        {
            if (target != null && TargetAlive())
                faceX = target.position.x >= transform.position.x ? 1f : -1f;
            else if (grabbedBody != null)
                faceX = grabbedBody.transform.position.x >= transform.position.x ? 1f : -1f;
            else if (grabbedRoller != null)
                faceX = grabbedRoller.transform.position.x >= transform.position.x ? 1f : -1f;
        }

        EnsureFacing(faceX);

        bool airborne = !grounded;
        bool falling = airborne && body.linearVelocity.y < -0.01f;
        bool jumping = airborne && body.linearVelocity.y > 0.01f;
        UpdateAnimations(grounded, jumping, falling);

        if (!tongueBusy && tongueLine != null && tongueLine.enabled)
        {
            tongueLine.enabled = false;
        }

        if (needsCollisionRestore && pendingRestoreCollider != null)
        {
            if (selfCollider.Distance(pendingRestoreCollider).distance > 0f)
            {
                Physics2D.IgnoreCollision(selfCollider, pendingRestoreCollider, false);
                needsCollisionRestore = false;
                pendingRestoreCollider = null;
            }
        }
        else if (needsCollisionRestore)
        {
            needsCollisionRestore = false;
            pendingRestoreCollider = null;
        }

        if (isMother)
        {
            bool inAttack = channeling || tongueBusy || IsAttackingTarget();
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                if (CanGiveBirth())
                {
                    spawnTimer = inAttack ? spawnAttackInterval : spawnInterval;
                    SpawnChildren();
                }
                else
                {
                    spawnTimer = spawnRetryDelay;
                }
            }
        }
    }

    private float JumpInterval() { return isMother ? motherJumpInterval : jumpInterval; }
    private float JumpHeight() { return jumpHeight * (isMother ? motherHeightMult : 1f); }
    private float JumpDistance() { return jumpDistance * (isMother ? motherDistanceMult : 1f); }
    private float TongueReach() { return Mathf.Max(tongueRange, tongueMaxLength); }

    private bool IsPulling()
    {
        return tongueState == TongueState.Pulling;
    }

    private float TargetDirX()
    {
        if (target == null) return moveDirection;
        return target.position.x >= transform.position.x ? 1f : -1f;
    }

    private void EnsureFacing(float vx)
    {
        if (vx > 0.01f && !facingRight) Flip();
        else if (vx < -0.01f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Transform t = model != null ? model : transform;
        Vector3 s = t.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        t.localScale = s;
    }

    private bool HasParam(string name)
    {
        if (animator == null || string.IsNullOrEmpty(name))
            return false;
        AnimatorControllerParameter[] ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == name)
                return true;
        }
        return false;
    }

    private void AnimBool(string name, bool value)
    {
        if (HasParam(name) && animator.GetBool(name) != value)
            animator.SetBool(name, value);
    }

    private void AnimTrigger(string name)
    {
        if (HasParam(name))
            animator.SetTrigger(name);
    }

    private void AnimFloat(string name, float value)
    {
        if (HasParam(name))
            animator.SetFloat(name, value);
    }

    private void UpdateAnimations(bool grounded, bool jumping, bool falling)
    {
        if (animator == null) return;
        AnimBool(jumpAnimParam, jumping);
        AnimBool(fallAnimParam, falling);

        bool idleOpen = tongueState == TongueState.Pulling ||
                        tongueState == TongueState.Extending ||
                        tongueState == TongueState.Retracting;
        bool idle = !idleOpen && grounded && !jumping && !falling;
        AnimBool(idleOpenMouthAnimParam, idleOpen);
        AnimBool(idleAnimParam, idle);
    }

public void OnMouthOpened()
    {
        mouthOpen = true;
    }

    public void OnMouthClosed()
    {
        mouthOpen = false;
    }

    public bool IsReleasing()
    {
        return releaseCooldown > 0f;
    }

    public bool IsPlayerVictim(PlayerHealth hp)
    {
        return grabbedIsPlayer && grabbedHp == hp;
    }

    public bool IsRollerVictim(MobHealth mh)
    {
        if (grabbedIsPlayer || grabbedRoller == null || mh == null) return false;
        MobHealth grabbedMobHp = grabbedRoller.GetComponent<MobHealth>();
        return grabbedMobHp != null && grabbedMobHp == mh;
    }

    public void NotifyPlayerDamageDealt(PlayerHealth hp, int amount)
    {
        if (amount <= 0 || !IsPlayerVictim(hp) || tongueState != TongueState.Pulling) return;
        grabDamageDealt += amount;
        if (debugLog)
            Debug.Log("[Frog] total damage " + grabDamageDealt + "/" + releaseAfterTotalDamage);
        if (grabDamageDealt >= releaseAfterTotalDamage)
            ReleaseFromMouth();
    }

    public void ReleaseFromMouth()
    {
        if (tongueState != TongueState.Pulling) return;
        ReleaseGrab(true);
        tongueState = TongueState.Ready;
        tongueCooldown = tongueCooldownTime;
        mouthOpen = false;
        AnimTrigger(closeMouthAnimParam);
        StartReleaseCooldown();
        UpdateTongueLine(false);
    }

    private void StartReleaseCooldown()
    {
        releasedThisGrab = true;
        releaseCooldown = releaseDuration;
    }

    private void HandleWallAndHop(float dirX, float dist)
    {
        if (IsPitAhead(dirX))
        {
            target = null;
            EscapeFromWall(dirX);
            return;
        }

        WallAction act = CheckJumpAhead(dirX);
        if (act == WallAction.Turn)
        {
            target = null;
            EscapeFromWall(dirX);
        }
        else if (act == WallAction.Jump)
        {
            Hop(dirX, plannedHopDist);
        }
        else if (act == WallAction.Back)
        {
            target = null;
            RetreatFromWall(dirX);
        }
        else
        {
            Hop(dirX, plannedHopDist);
        }
    }

    private void EscapeFromWall(float blockedDirX)
    {
        int cand = -Mathf.RoundToInt(blockedDirX);
        if (IsPitAhead(cand))
            cand = -cand;

        if (CheckJumpAhead(cand) == WallAction.Turn)
        {
            blockedEscapeTimer = blockedEscapeTime;
            return;
        }

        moveDirection = cand;
        EnsureFacing(moveDirection);
        blockedEscapeTimer = blockedEscapeTime;
        Hop(moveDirection, plannedHopDist);
    }

    private void RetreatFromWall(float blockedDirX)
    {
        moveDirection = -Mathf.RoundToInt(blockedDirX);
        EnsureFacing(moveDirection);
        blockedEscapeTimer = backOffTime;
        Hop(moveDirection, retreatHopDist);
    }

    private void Hop(float dirX, float dist)
    {
        if (isJumping || !IsGrounded() || body.linearVelocity.y > 0.01f) return;

        float g = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        if (g <= 0.001f) g = 9.81f;

        float riseTime = Mathf.Sqrt(2f * JumpHeight() / g);
        float flight = Mathf.Max(2f * riseTime, 0.1f);

        float reachable = Mathf.Clamp(dist, 0f, maxHopSpeed * flight);
        float vx = (reachable / flight) * Mathf.Sign(dirX);
        float vy = riseTime * g;

        body.linearVelocity = new Vector2(vx, vy);
        isJumping = true;
        landingDamageDone = false;
        EnsureFacing(vx);

        RecordJumpTrajectory(vx, vy, g, flight);
    }

    private void RecordJumpTrajectory(float vx, float vy, float g, float duration)
    {
        trjStart = (Vector2)transform.position;
        trjVx = vx;
        trjVy = vy;
        trjG = g;
        trjDuration = duration;
        trjActive = true;
        trjLandFound = SimulateLanding(trjStart, vx, vy, g, out trjLandPoint, out trjLandTime);
    }

    private bool SimulateLanding(Vector2 start, float vx, float vy, float g, out Vector2 landPoint, out float landTime)
    {
        landPoint = start;
        landTime = 0f;

        LayerMask mask = ScanMask();
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

        landTime = steps * simDt;
        landPoint = new Vector2(start.x + vx * landTime, start.y - 2f);
        return false;
    }

    private bool ArcClearsObstacle(Vector2 start, float halfH, Collider2D obstacle, float nearD, float farD, float targetDist)
    {
        if (obstacle == null || farD <= nearD) return true;

        float g = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        if (g <= 0.001f) g = 9.81f;
        float riseTime = Mathf.Sqrt(2f * JumpHeight() / g);
        float flight = Mathf.Max(2f * riseTime, 0.1f);

        float reachable = Mathf.Clamp(targetDist, 0f, maxHopSpeed * flight);
        float vx = reachable / flight;
        if (vx <= 0.001f) return false;
        float vy = riseTime * g;
        float top = obstacle.bounds.max.y;

        for (float d = nearD; d <= farD + 0.05f; d += 0.1f)
        {
            float t = d / vx;
            float height = vy * t - 0.5f * g * t * t;
            float feet = start.y + height - halfH;
            if (feet < top + 0.08f) return false;
        }
        return true;
    }

    private Vector2 MouthWorld()
    {
        if (mouth != null)
            return (Vector2)mouth.position;

        Vector2 pos = (Vector2)transform.position;
        float upOff = 0.25f;
        if (model != null)
            upOff = Mathf.Max(model.localScale.y * 0.5f, 0.25f);
        else if (selfCollider != null)
            upOff = selfCollider.bounds.extents.y * 1.4f;
        return pos + Vector2.up * Mathf.Max(upOff, 0.25f);
    }

    private void InitTongueLine()
    {
        if (tongueLine == null) return;
        tongueLine.useWorldSpace = true;
        tongueLine.positionCount = 2;
        tongueLine.startWidth = tongueWidth;
        tongueLine.endWidth = tongueWidth;
        tongueLine.enabled = false;
    }

    private void UpdateTongueLine(bool active)
    {
        if (tongueLine == null) return;
        tongueLine.enabled = active;
        if (active)
        {
            tongueLine.SetPosition(0, tongueRoot);
            tongueLine.SetPosition(1, tongueTip);
        }
    }

    private void StartTongue()
    {
        homingTongue = target != null;
        homingTarget = target;
        tongueAim = channelAim;

        tongueRoot = MouthWorld();
        tongueTip = tongueRoot + Vector2.up * 0.1f;

        Vector2 d = tongueAim - tongueTip;
        tongueDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;

        tongueTimer = 0f;
        tongueState = TongueState.Extending;
        mouthOpen = false;
        openMouthTimer = openMouthDur;
        AnimTrigger(openMouthAnimParam);

        if (audioSource != null && tongueSound != null)
            audioSource.PlayOneShot(tongueSound);

        UpdateTongueLine(true);
    }

    private void UpdateTongue(float dt)
    {
        tongueTimer += dt;

            if (tongueState == TongueState.Extending)
            {
                if (!mouthOpen && !requireMouthAnimation)
                    mouthOpen = true;
                if (!mouthOpen && openMouthTimer > 0f)
                {
                    openMouthTimer -= dt;
                    if (openMouthTimer <= 0f)
                        mouthOpen = true;
                }

                if (homingTongue && homingTarget != null && homingTarget.gameObject.activeInHierarchy)
                {
                    Vector2 dir = (Vector2)homingTarget.position - tongueTip;
                    tongueDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : tongueDir;
                }

                float accelFactor = tongueAccelTime > 0.01f
                    ? Mathf.Clamp01(tongueTimer / tongueAccelTime)
                    : 1f;

                tongueTip += tongueDir * tongueSpeed * accelFactor * dt;

                bool maxReach = Vector2.Distance(tongueRoot, tongueTip) >= tongueMaxLength;
                bool timedOut = tongueTimer >= tongueLifetime;
                bool lostTarget = homingTarget == null || !homingTarget.gameObject.activeInHierarchy;

                CheckTongueHit();

                ProcessPendingGrab();

                if (tongueState != TongueState.Extending)
                    return;

                if (maxReach || timedOut || lostTarget)
                {
                    StartRetract();
                    UpdateTongueLine(true);
                    return;
                }

                UpdateTongueLine(true);
            }
        else if (tongueState == TongueState.Retracting)
        {
            tongueRoot = MouthWorld();
            float dist = Vector2.Distance(tongueTip, tongueRoot);
            float slowFactor = Mathf.Lerp(tongueRetractSlowdown, 1f, Mathf.Clamp01(dist / 0.5f));
            tongueTip = Vector2.MoveTowards(tongueTip, tongueRoot, pullSpeed * 3f * slowFactor * dt);
            UpdateTongueLine(true);

            if (dist < 0.05f)
            {
                tongueState = TongueState.Ready;
                tongueCooldown = tongueCooldownTime;
                AnimTrigger(closeMouthAnimParam);
                UpdateTongueLine(false);
            }
        }
        else if (tongueState == TongueState.Pulling)
        {
            UpdatePull(dt);
        }
    }

    private void CheckTongueHit()
    {
        Vector2 seg = tongueTip - tongueRoot;
        float segLen = seg.magnitude;
        if (segLen < 0.01f)
            return;

        Vector2 dir = seg / segLen;
        int samples = Mathf.Max(2, Mathf.CeilToInt(segLen / (tongueRadius * 2f)) + 1);
        float startT = 0.6f;

        for (int s = 0; s < samples; s++)
        {
            float t = startT + (1f - startT) * ((float)s / Mathf.Max(samples - 1, 1));
            Vector2 p = tongueRoot + dir * (segLen * t);
            Collider2D hit = FilterVictim(p);
            if (hit == null) continue;

            PlayerHealth hp = hit.GetComponentInParent<PlayerHealth>();
            CrystalRoller cr = hit.GetComponentInParent<CrystalRoller>();

            if (releaseCooldown > 0f)
            {
                ClearPending();
                StartRetract();
                return;
            }

            if (hp != null && hp.gameObject.activeInHierarchy)
            {
                HandlePendingGrab(hp, hit);
                return;
            }
            if (cr != null && cr.gameObject.activeInHierarchy)
            {
                HandlePendingGrab(cr, hit);
                return;
            }
        }
    }

    private void HandlePendingGrab(PlayerHealth hp, Collider2D hit)
    {
        if (requireMouthAnimation && !mouthOpen)
        {
            pendingHp = hp;
            pendingRoller = null;
            pendingCollider = hit;
            return;
        }
        OnTongueHitPlayer(hp, hit);
    }

    private void HandlePendingGrab(CrystalRoller roller, Collider2D hit)
    {
        if (requireMouthAnimation && !mouthOpen)
        {
            pendingHp = null;
            pendingRoller = roller;
            pendingCollider = hit;
            return;
        }
        OnTongueHitRoller(roller, hit);
    }

    private void ProcessPendingGrab()
    {
        if (pendingRoller != null)
        {
            if (releaseCooldown > 0f)
            {
                ClearPending();
                StartRetract();
                return;
            }
            if (requireMouthAnimation && !mouthOpen) return;
            CrystalRoller r = pendingRoller;
            Collider2D c = pendingCollider;
            ClearPending();
            OnTongueHitRoller(r, c);
            return;
        }
        if (pendingHp != null)
        {
            if (releaseCooldown > 0f)
            {
                ClearPending();
                StartRetract();
                return;
            }
            if (requireMouthAnimation && !mouthOpen) return;
            PlayerHealth h = pendingHp;
            Collider2D c = pendingCollider;
            ClearPending();
            OnTongueHitPlayer(h, c);
            return;
        }
    }

    private void ClearPending()
    {
        pendingHp = null;
        pendingRoller = null;
        pendingCollider = null;
    }

    private Collider2D FilterVictim(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, tongueRadius);
        Collider2D best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D h = hits[i];
            if (h == null || h.isTrigger) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (selfCollider != null && h == selfCollider) continue;

            if (h.GetComponentInParent<PlayerHealth>() == null &&
                h.GetComponentInParent<CrystalRoller>() == null)
                continue;

            float dSqr = ((Vector2)h.transform.position - point).sqrMagnitude;
            if (dSqr < bestSqr)
            {
                bestSqr = dSqr;
                best = h;
            }
        }
        return best;
    }

    private void OnTongueHitPlayer(PlayerHealth hp, Collider2D hitCollider)
    {
        if (isMother)
        {
            hp.TakeDamage(playerBiteDamage);
            StartRetract();
            return;
        }

        CharacterBody cb = hitCollider.GetComponentInParent<CharacterBody>();
        if (cb != null)
            cb.tongueFrozen = true;

        grabbedIsPlayer = true;
        grabbedHp = hp;
        grabbedBody = cb;
        grabbedCollider = hitCollider;
        grabbedRb = hitCollider.GetComponentInParent<Rigidbody2D>();
        pullStuckTimer = 0f;
        grabDamageDealt = 0;
        FreezeGrabBody();

        if (selfCollider != null && grabbedCollider != null)
            Physics2D.IgnoreCollision(selfCollider, grabbedCollider, true);

        tongueState = TongueState.Pulling;

        if (debugLog)
            Debug.Log("[Frog] GRAB player, hp=" + (grabbedHp != null)
                + " rb=" + (grabbedRb != null) + " mouthOpen=" + mouthOpen);
    }

    private void OnTongueHitRoller(CrystalRoller roller, Collider2D hitCollider)
    {
        if (isMother)
        {
            MobHealth mh = roller.GetComponent<MobHealth>();
            if (mh != null && !mh.IsDead)
                mh.TakeDamage(999);
            StartRetract();
            return;
        }

        roller.tongueGrabbed = true;
        grabbedIsPlayer = false;
        grabbedRoller = roller;
        grabbedCollider = hitCollider;
        grabbedRb = hitCollider.GetComponentInParent<Rigidbody2D>();
        pullStuckTimer = 0f;
        FreezeGrabBody();

        if (selfCollider != null && grabbedCollider != null)
            Physics2D.IgnoreCollision(selfCollider, grabbedCollider, true);

        tongueState = TongueState.Pulling;
    }

    private void UpdatePull(float dt)
    {
        if (grabbedRb == null || !GrabbedAlive())
        {
            ReleaseGrab(true);
            tongueState = TongueState.Ready;
            tongueCooldown = tongueCooldownTime;
            UpdateTongueLine(false);
            return;
        }

        if (grabbedIsPlayer && grabbedBody != null && grabbedBody.dashing)
        {
            if (debugLog)
                Debug.Log("[Frog] weapon/dash strike -> release");
            ReleaseFromMouth();
            return;
        }

        Vector2 mouthPos = MouthWorld();
        tongueRoot = mouthPos;

        GameObject victimGo = grabbedIsPlayer && grabbedBody != null ? grabbedBody.gameObject
                            : grabbedRoller != null ? grabbedRoller.gameObject : null;

        Vector2 pullTarget = mouthPos;
        if (victimGo != null)
        {
            Collider2D vc = grabbedCollider != null ? grabbedCollider
                          : victimGo.GetComponentInChildren<Collider2D>();
            if (vc != null)
                pullTarget = mouthPos - Vector2.up * vc.bounds.extents.y;
        }

        Vector2 from = grabbedRb.position;
        Vector2 toTarget = pullTarget - from;
        float dist = toTarget.magnitude;

        float t = 1f - Mathf.Exp(-pullRate * dt);
        grabbedRb.MovePosition(Vector2.Lerp(from, pullTarget, t));
        grabbedRb.linearVelocity = Vector2.zero;

        tongueTip = grabbedRb.position;
        if (dist > 0.05f)
            tongueTip += toTarget.normalized * 0.25f;
        UpdateTongueLine(true);

        if (dist <= pullReachRange)
        {
            pullStuckTimer = 0f;
        }
        else
        {
            pullStuckTimer += dt;
            if (pullStuckTimer >= pullTimeout)
            {
                StartRetract();
                if (debugLog)
                    Debug.Log("[Frog] pull stuck -> retract");
            }
        }
    }

    private void FreezeGrabBody()
    {
        grabbedGravityScale = 1f;
        if (grabbedRb != null)
        {
            grabbedGravityScale = grabbedRb.gravityScale;
            grabbedRb.gravityScale = 0f;
            grabbedRb.linearVelocity = Vector2.zero;
        }
    }

    private void RestoreGrabBody()
    {
        if (grabbedRb != null)
        {
            grabbedRb.gravityScale = grabbedGravityScale;
            grabbedRb.linearVelocity = Vector2.zero;
        }
    }

    private bool GrabbedAlive()
    {
        if (grabbedIsPlayer)
        {
            if (grabbedBody != null && !grabbedBody.gameObject.activeInHierarchy)
                return false;
            return grabbedHp != null && grabbedHp.gameObject.activeInHierarchy;
        }
        return grabbedRoller != null && grabbedRoller.gameObject.activeInHierarchy;
    }

    private void ReleaseGrab(bool victimDied)
    {
        pullStuckTimer = 0f;
        RestoreGrabBody();
        if (grabbedBody != null)
            grabbedBody.tongueFrozen = false;
        if (grabbedRoller != null)
            grabbedRoller.tongueGrabbed = false;

        if (selfCollider != null && grabbedCollider != null)
        {
            if (selfCollider.Distance(grabbedCollider).distance <= 0f)
            {
                needsCollisionRestore = true;
                pendingRestoreCollider = grabbedCollider;
            }
            else
            {
                Physics2D.IgnoreCollision(selfCollider, grabbedCollider, false);
            }
        }

        grabbedIsPlayer = false;
        grabbedHp = null;
        grabbedBody = null;
        grabbedRoller = null;
        grabbedRb = null;
        grabbedCollider = null;
    }

    private void StartRetract()
    {
        ClearPending();
        ReleaseGrab(false);
        tongueRoot = MouthWorld();
        tongueState = TongueState.Retracting;
    }

    private void ApplyMotherLandingDamage()
    {
        if (!isMother) return;
        Vector2 feet = (Vector2)transform.position + Vector2.down * 0.25f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(feet, 0.6f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D h = hits[i];
            if (h == null || h.isTrigger) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;

            PlayerHealth hp = h.GetComponentInParent<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(motherLandingDamage);
                continue;
            }

            CrystalRoller cr = h.GetComponentInParent<CrystalRoller>();
            if (cr != null && cr.gameObject.activeInHierarchy)
            {
                MobHealth mh = cr.GetComponent<MobHealth>();
                if (mh != null && !mh.IsDead)
                    mh.TakeDamage(999);
            }
        }
    }

    private void SenseTarget()
    {
        if (blockedEscapeTimer > 0f)
            return;

        target = null;

        Vector2 me = (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(me, senseRange);

        Transform best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D h = hits[i];
            if (h == null || h.isTrigger) continue;
            GameObject go = h.gameObject;
            if (!go.activeInHierarchy) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;

            PlayerHealth hp = go.GetComponentInParent<PlayerHealth>();
            if (hp != null)
            {
                CharacterBody cb = null;
                CharacterBody[] bodies = hp.GetComponentsInChildren<CharacterBody>(true);
                for (int b = 0; b < bodies.Length; b++)
                {
                    if (bodies[b].gameObject.activeInHierarchy)
                    {
                        cb = bodies[b];
                        break;
                    }
                }
                Transform aim = cb != null ? cb.transform : hp.transform;
                if (!HasClearPathTo((Vector2)aim.position)) continue;
                float dSqr = ((Vector2)aim.position - me).sqrMagnitude;
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    best = aim;
                }
                continue;
            }

            CrystalRoller cr = go.GetComponentInParent<CrystalRoller>();
            if (cr != null && cr.gameObject.activeInHierarchy)
            {
                if (!HasClearPathTo((Vector2)cr.transform.position)) continue;
                float dSqr = ((Vector2)cr.transform.position - me).sqrMagnitude;
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    best = cr.transform;
                }
            }
        }

        target = best;
    }

    private bool TargetAlive()
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;
        Transform root = target.root;
        return root == null || root.gameObject.activeInHierarchy;
    }

    private bool HasClearPathTo(Vector2 targetPos)
    {
        Vector2 me = (Vector2)transform.position;
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

    private bool TryCastWall(Vector2 origin, Vector2 dir, float dist, LayerMask mask, out RaycastHit2D hit)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, dist, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<CrystalRoller>() != null) continue;
            if (h.collider.GetComponentInParent<CrystalFrog>() != null) continue;
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
        if (mask == 0) mask = ~0;
        return mask;
    }

    private enum WallAction : byte { Clear, Jump, Turn, Back }

    private WallAction CheckJumpAhead(float dirX)
    {
        plannedHopDist = JumpDistance();
        if (!IsGrounded())
            return WallAction.Clear;
        if (IsPitAhead(dirX))
            return WallAction.Turn;

        LayerMask mask = ScanMask();
        Vector2 center = (Vector2)transform.position;
        float halfW = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        float halfH = selfCollider != null ? selfCollider.bounds.extents.y : 0.5f;
        Vector2 dir = new Vector2(Mathf.Sign(dirX), 0f);
        Vector2 feetOffset = Vector2.up * (-halfH + 0.08f);
        Vector2 headOffset = Vector2.up * (halfH - 0.08f);
        Vector2 front = center + dir * halfW;
        float length = JumpDistance();

        if (TryCastWall(front + headOffset, dir, length, mask, out _))
            return WallAction.Turn;

        bool feetBlocked = TryCastWall(front + feetOffset, dir, length, mask, out RaycastHit2D feetHit);
        if (feetBlocked && feetHit.collider != null
            && feetHit.collider.bounds.max.y > center.y + halfH - 0.05f)
            return WallAction.Turn;

        if (feetBlocked)
        {
            if (feetHit.collider != null)
            {
                float nearD = halfW + feetHit.distance;
                float farD = nearD + feetHit.collider.bounds.size.x;
                float cand = Mathf.Clamp(farD + halfW + 0.1f, 0f, JumpDistance());
                if (!ArcClearsObstacle(center, halfH, feetHit.collider, nearD, farD, cand))
                    return WallAction.Back;
                if (!LandingPointSolid(center, dir, cand, halfW, halfH, mask))
                    return WallAction.Back;
                plannedHopDist = cand;
            }
            return WallAction.Jump;
        }

        const int stations = 3;
        for (int s = 0; s < stations; s++)
        {
            float p = stations <= 1 ? 0f : (float)s / (stations - 1);
            Vector2 station = front + feetOffset + dir * (length * p);
            float remaining = Mathf.Max(0.05f, length * (1f - p));

            bool leftHit = TryCastWall(station + Vector2.left * halfW, dir, remaining, mask, out _);
            bool rightHit = TryCastWall(station + Vector2.right * halfW, dir, remaining, mask, out _);
            if ((leftHit ? 1 : 0) + (rightHit ? 1 : 0) >= 2)
                return WallAction.Turn;
        }

        float extent = MobNav.FloorExtent(center + feetOffset, dirX, length, maxFallDepth + 0.4f, mask);
        if (extent < length - 0.05f)
        {
            if (extent <= halfW + 0.2f)
                return WallAction.Turn;
            plannedHopDist = Mathf.Clamp(extent - halfW - 0.15f, 0.8f, length);
        }

        return WallAction.Clear;
    }

    private bool LandingPointSolid(Vector2 center, Vector2 dir, float dist, float halfW, float halfH, LayerMask mask)
    {
        Vector2 feetLevel = center + dir * dist;
        Vector2 basePoint = feetLevel + Vector2.down * (halfH - 0.05f);
        float depth = maxFallDepth + 0.4f;

        if (!MobNav.HasFloor(basePoint, depth, mask)) return false;
        if (!MobNav.HasFloor(basePoint + Vector2.left * halfW, depth, mask)) return false;
        if (!MobNav.HasFloor(basePoint + Vector2.right * halfW, depth, mask)) return false;
        return true;
    }

    private bool ObstacleAhead(float dir)
    {
        float halfWidth = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        float halfHeight = selfCollider != null ? selfCollider.bounds.extents.y : 0.5f;
        Vector2 position = (Vector2)transform.position;
        LayerMask mask = ScanMask();

        Vector2 wallOrigin = position + new Vector2(dir * (halfWidth + 0.05f), 0f);
        RaycastHit2D wall;
        if (TryCastWall(wallOrigin, Vector2.right * Mathf.Sign(dir), 0.35f, mask, out wall))
            return true;

        LayerMask pitMask = groundMask != 0 ? groundMask : mask;
        Vector2 pitOrigin = position + new Vector2(dir * halfWidth, -halfHeight - 0.05f);
        RaycastHit2D pit;
        if (!TryCastWall(pitOrigin, Vector2.down, maxFallDepth + 0.3f, pitMask, out pit))
            return true;
        return pit.distance > maxFallDepth;
    }

    private bool IsPitAhead(float dir)
    {
        float halfWidth = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
        float halfHeight = selfCollider != null ? selfCollider.bounds.extents.y : 0.5f;
        Vector2 position = (Vector2)transform.position;
        LayerMask pitMask = groundMask != 0 ? groundMask : ScanMask();

        Vector2 pitOrigin = position + new Vector2(dir * halfWidth, -halfHeight - 0.05f);
        RaycastHit2D pit;
        if (!TryCastWall(pitOrigin, Vector2.down, maxFallDepth + 0.3f, pitMask, out pit))
            return true;
        return pit.distance > maxFallDepth;
    }

    private bool IsGrounded()
    {
        Vector2 down = Vector2.down;
        Vector2 origin;
        float dist = groundCheckDistance;

        if (groundCheck != null)
        {
            origin = (Vector2)groundCheck.position;
        }
        else
        {
            float halfH = selfCollider != null ? selfCollider.bounds.extents.y : 0.5f;
            origin = (Vector2)transform.position + down * halfH;
            if (dist < 0.1f) dist = 0.25f;
        }

        LayerMask mask = groundMask;
        if (mask == 0)
            mask = ~0;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, down, dist, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;

            Transform t = h.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<CrystalRoller>() != null) continue;
            if (h.collider.GetComponentInParent<CrystalFrog>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            return true;
        }
        return false;
    }

    private void SpawnChildren()
    {
        if (normalFrogPrefab == null) return;
        if (!CanGiveBirth()) return;

        AnimTrigger(birthAnimParam);

        for (int i = 0; i < spawnCount; i++)
        {
            float offset = (i - (spawnCount - 1) * 0.5f) * spawnRadius;
            Vector2 pos = (Vector2)transform.position + new Vector2(offset, -0.3f);
            GameObject child = Instantiate(normalFrogPrefab, pos, Quaternion.identity);
            IgnoreChildCollisions(child);
        }
    }

    private bool CanGiveBirth()
    {
        if (isJumping) return false;
        if (!IsGrounded()) return false;
        return body == null || body.linearVelocity.y <= 0.1f;
    }

    private bool IsAttackingTarget()
    {
        if (target == null || !TargetAlive()) return false;
        return Vector2.Distance((Vector2)transform.position, (Vector2)target.position) <= TongueReach();
    }

    private void IgnoreChildCollisions(GameObject child)
    {
        Collider2D[] childColliders = child.GetComponentsInChildren<Collider2D>(true);
        Collider2D[] motherColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D mc in motherColliders)
        {
            if (mc == null) continue;
            foreach (Collider2D cc in childColliders)
            {
                if (cc == null) continue;
                Physics2D.IgnoreCollision(mc, cc, true);
            }
        }
    }

    private void IgnoreAllFrogCollisions()
    {
        for (int i = 0; i < FrogRegistry.Count; i++)
        {
            CrystalFrog f = FrogRegistry[i];
            if (f == null || f == this) continue;
            IgnoreCollisionsBetween(this, f);
        }
    }

    private void IgnoreCollisionsBetween(CrystalFrog a, CrystalFrog b)
    {
        Collider2D[] ac = a.GetComponentsInChildren<Collider2D>(true);
        Collider2D[] bc = b.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D ca in ac)
        {
            if (ca == null) continue;
            foreach (Collider2D cb in bc)
            {
                if (cb == null) continue;
                Physics2D.IgnoreCollision(ca, cb, true);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, senseRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, tongueRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, tongueMaxLength);

        if (mouth != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.6f);
            Gizmos.DrawWireSphere(mouth.position, tongueRadius);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        float dir = Application.isPlaying ? moveDirection : 1f;
            Vector2 center = (Vector2)transform.position;
            float halfH = selfCollider != null ? selfCollider.bounds.extents.y : 0.5f;
            float halfW = selfCollider != null ? selfCollider.bounds.extents.x : 0.4f;
            float len = JumpDistance();
            Vector2 fwd = new Vector2(dir * len, 0f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(center + new Vector2(0f, -halfH + 0.08f), center + new Vector2(0f, -halfH + 0.08f) + fwd);
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            Gizmos.DrawLine(center + new Vector2(-halfW, -halfH + 0.08f), center + new Vector2(-halfW, -halfH + 0.08f) + fwd);
            Gizmos.DrawLine(center + new Vector2(halfW, -halfH + 0.08f), center + new Vector2(halfW, -halfH + 0.08f) + fwd);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(center + new Vector2(0f, halfH - 0.08f), center + new Vector2(0f, halfH - 0.08f) + fwd);

            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawLine(center, center + new Vector2(0f, JumpHeight()));

            if (Application.isPlaying && plannedHopDist > 0.01f && plannedHopDist < len - 0.05f)
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
                Gizmos.DrawLine(center + new Vector2(0f, -halfH + 0.08f),
                                center + new Vector2(dir * plannedHopDist, -halfH + 0.08f));
                Gizmos.DrawWireSphere(center + new Vector2(dir * plannedHopDist, -halfH + 0.08f), 0.12f);
            }

        DrawJumpTrajectory();
    }

    private void DrawJumpTrajectory()
    {
        if (!trjActive) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        float dur = trjLandFound ? trjLandTime : trjDuration;
        const int pts = 24;
        Vector2 prev = trjStart;
        for (int i = 1; i <= pts; i++)
        {
            float t = dur * i / pts;
            float x = trjStart.x + trjVx * t;
            float y = trjStart.y + trjVy * t - 0.5f * trjG * t * t;
            Vector2 cur = new Vector2(x, y);
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }

        Gizmos.color = new Color(1f, 0f, 1f, 1f);
        Gizmos.DrawWireSphere(trjLandPoint, 0.18f);
    }
}
