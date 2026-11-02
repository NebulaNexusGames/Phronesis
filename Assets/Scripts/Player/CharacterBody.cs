using UnityEngine;

[DisallowMultipleComponent]
public class CharacterBody : MonoBehaviour
{
    [Header("Модель")]
    [SerializeField] private Transform model;
    [SerializeField] private bool flipModelByFacing = true;

    [Header("Коллайдеры")]
    [SerializeField] private Collider2D normalCollider;
    [SerializeField] private Collider2D crouchCollider;

    [Header("Точки проверки")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform roofCheck;

    private Rigidbody2D body;

    private void Awake()
    {
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ApplyColliderState(IsCrouched);
    }

    [HideInInspector] public float axisInput;
    [HideInInspector] public bool jumpHeld;
    [HideInInspector] public bool crouchHeld;
    [HideInInspector] public bool runHeld;
    [HideInInspector] public bool jumpQueued;
    [HideInInspector] public bool dashQueued;
    [HideInInspector] public bool interactHeld;
    [HideInInspector] public bool interactPressed;
    [HideInInspector] public bool tongueFrozen;
    [HideInInspector] public bool groundedNow;

    [HideInInspector] public float coyoteCounter;
    [HideInInspector] public float jumpHoldTimer;
    [HideInInspector] public float dashTimer;
    [HideInInspector] public float dashCooldownTimer;
    [HideInInspector] public bool dashing;
    [HideInInspector] public float baseGravityScale;
    [HideInInspector] public bool gravityCached;

    public float FacingDirection { get; set; } = 1f;
    public bool IsCrouched { get; private set; }

    public Rigidbody2D Body
    {
        get
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            return body;
        }
    }

    public Vector2 Position => Body.position;
    public Vector2 Velocity => Body.linearVelocity;

    private Collider2D ActiveCollider => IsCrouched && crouchCollider != null ? crouchCollider : normalCollider;

    public void SetActiveState(bool active)
    {
        gameObject.SetActive(active);
    }

    public void Teleport(Vector2 position)
    {
        Body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        Body.linearVelocity = Vector2.zero;
    }

    public void SetCrouched(bool crouched)
    {
        IsCrouched = crouched;
        ApplyColliderState(crouched);
        Rigidbody2D rb = Body;
        rb.position += Vector2.up * 0.001f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.WakeUp();
    }

    private void ApplyColliderState(bool crouched)
    {
        if (crouchCollider != null)
            crouchCollider.enabled = crouched;
        if (normalCollider != null)
            normalCollider.enabled = !crouched;
    }

    public bool IsGrounded(float checkDistance, LayerMask groundMask)
    {
        if (groundCheck == null) return false;
        return Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, groundMask);
    }

    public bool HasCeilingAbove(float checkDistance, LayerMask ceilingMask)
    {
        if (roofCheck == null) return false;
        return Physics2D.Raycast(roofCheck.position, Vector2.up, checkDistance, ceilingMask);
    }

    public bool Touches(CharacterBody other, float tolerance)
    {
        Collider2D mine = ActiveCollider;
        Collider2D theirs = other == null ? null : other.ActiveCollider;
        if (mine == null || theirs == null) return false;
        if (!mine.enabled || !theirs.enabled) return false;
        ColliderDistance2D dist = mine.Distance(theirs);
        return dist.isOverlapped || dist.distance <= tolerance;
    }

    public void ApplyFacing()
    {
        if (!flipModelByFacing || model == null) return;
        Vector3 scale = model.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(FacingDirection);
        model.localScale = scale;
    }

    public void SetModelVisible(bool visible)
    {
        if (model != null) model.gameObject.SetActive(visible);
    }
}
