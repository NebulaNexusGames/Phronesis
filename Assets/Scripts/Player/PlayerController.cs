using UnityEngine;

[RequireComponent(typeof(MergeController))]
public class PlayerController : MonoBehaviour
{
    [Header("Ссылка на скрипт слияния")]
    [SerializeField] private MergeController merge;

    [Header("Раскладки персонажей")]
    [SerializeField] private ControlLayout loveLayout = ControlLayout.Player1Standard;
    [SerializeField] private ControlLayout bluLayout = ControlLayout.Player1Standard;
    [SerializeField] private ControlLayout roseLayout = ControlLayout.Player2Arrows;

    [Header("Движение")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;

    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float maxJumpHoldTime = 0.25f;
    [SerializeField] private float jumpHoldForce = 30f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Дэш")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Проверки лучей")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask ceilingMask;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float roofCheckDistance = 0.5f;

    [Header("Поглаживание кристаликата")]
    [SerializeField] private float petRange = 2.2f;
    [SerializeField] private float petLockDuration = 1f;

    private PlayerAnimator loveAnim;
    private PlayerAnimator bluAnim;
    private PlayerAnimator roseAnim;

    private void Awake()
    {
        if (merge == null) merge = GetComponent<MergeController>();
        loveAnim = merge.Love == null ? null : merge.Love.GetComponent<PlayerAnimator>();
        bluAnim = merge.Blu == null ? null : merge.Blu.GetComponent<PlayerAnimator>();
        roseAnim = merge.Rose == null ? null : merge.Rose.GetComponent<PlayerAnimator>();
    }

    private PlayerAnimator AnimatorFor(CharacterBody b)
    {
        if (b == merge.Love) return loveAnim;
        if (b == merge.Blu) return bluAnim;
        if (b == merge.Rose) return roseAnim;
        return null;
    }

    private void Update()
    {
        if (merge.IsMerged)
        {
            Poll(merge.Love, loveLayout);
        }
        else
        {
            Poll(merge.Blu, bluLayout);
            Poll(merge.Rose, roseLayout);
        }
    }

    private void Poll(CharacterBody body, ControlLayout layout)
    {
        body.axisInput = PlayerInput.Horizontal(layout);
        body.runHeld = PlayerInput.RunHeld(layout);
        body.crouchHeld = PlayerInput.CrouchHeld(layout);
        body.jumpHeld = PlayerInput.JumpHeld(layout);
        body.interactHeld = PlayerInput.InteractHeld(layout);
        if (PlayerInput.JumpPressed(layout)) body.jumpQueued = true;
        if (PlayerInput.DashPressed(layout)) body.dashQueued = true;
        if (PlayerInput.InteractPressed(layout)) body.interactPressed = true;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (merge.IsMerged)
        {
            Drive(merge.Love, dt);
        }
        else
        {
            Drive(merge.Blu, dt);
            Drive(merge.Rose, dt);
        }
    }

    private void Drive(CharacterBody b, float dt)
    {
        if (b == null || !b.gameObject.activeInHierarchy) return;

        PlayerAnimator pa = AnimatorFor(b);
        if (pa != null && pa.IsInteracting)
        {
            b.jumpQueued = false;
            b.dashQueued = false;
            b.interactPressed = false;
            b.coyoteCounter = 0f;
            b.jumpHoldTimer = 0f;
            b.Body.linearVelocity = new Vector2(0f, b.Body.linearVelocity.y);
            return;
        }

        if (b.tongueFrozen)
        {
            if (b.dashQueued && !b.dashing)
            {
                b.dashing = true;
                b.dashTimer = dashDuration;
                b.dashCooldownTimer = dashCooldown;
            }
            b.dashQueued = false;
            b.Body.linearVelocity = Vector2.zero;
            return;
        }

        if (!b.gravityCached)
        {
            b.baseGravityScale = b.Body.gravityScale;
            b.gravityCached = true;
        }

        bool grounded = b.IsGrounded(groundCheckDistance, groundMask);
        b.groundedNow = grounded;
        b.coyoteCounter = grounded ? coyoteTime : b.coyoteCounter - dt;

        if (b.crouchHeld && !b.IsCrouched)
        {
            b.SetCrouched(true);
        }
        else if (!b.crouchHeld && b.IsCrouched && !b.HasCeilingAbove(roofCheckDistance, ceilingMask))
        {
            b.SetCrouched(false);
        }

        if (b.IsCrouched)
        {
            b.jumpQueued = false;
            b.dashQueued = false;
            b.coyoteCounter = 0f;
        }

        if (b.interactPressed && pa != null)
        {
            CrystalRoller roller = CrystalRoller.FindNearestPet(b.Position, petRange);
            if (roller != null && roller.TryBeginPet(b))
            {
                pa.BeginPet(petLockDuration);
            }
        }

        if (b.dashQueued && !b.dashing && !b.IsCrouched && b.dashCooldownTimer <= 0f)
        {
            b.dashing = true;
            b.dashTimer = dashDuration;
            b.dashCooldownTimer = dashCooldown;
        }
        if (b.dashCooldownTimer > 0f) b.dashCooldownTimer -= dt;

        if (!b.dashing && b.axisInput != 0f)
        {
            b.FacingDirection = Mathf.Sign(b.axisInput);
        }

        float vy = b.Body.linearVelocity.y;

        if (b.jumpQueued && !b.dashing && !b.IsCrouched && b.coyoteCounter > 0f)
        {
            vy = jumpForce;
            b.coyoteCounter = 0f;
            b.jumpHoldTimer = maxJumpHoldTime;
        }
        if (b.jumpHeld && b.jumpHoldTimer > 0f && vy > 0f)
        {
            vy += jumpHoldForce * dt;
            b.jumpHoldTimer -= dt;
        }
        else
        {
            b.jumpHoldTimer = 0f;
        }

        float vx;
        if (b.dashing)
        {
            vx = b.FacingDirection * dashSpeed;
            vy = 0f;
            b.dashTimer -= dt;
            if (b.dashTimer <= 0f) b.dashing = false;
        }
        else
        {
            float speed = b.IsCrouched ? crouchSpeed : b.runHeld ? runSpeed : walkSpeed;
            vx = b.axisInput * speed;
        }

        float gs = b.baseGravityScale;
        if (!b.dashing)
        {
            if (vy < 0f) gs *= fallMultiplier;
            else if (vy > 0f && !b.jumpHeld) gs *= lowJumpMultiplier;
        }
        else
        {
            gs = 0f;
        }

        b.Body.gravityScale = gs;
        b.Body.linearVelocity = new Vector2(vx, vy);
        b.ApplyFacing();

        b.jumpQueued = false;
        b.dashQueued = false;
        b.interactPressed = false;
    }
}
