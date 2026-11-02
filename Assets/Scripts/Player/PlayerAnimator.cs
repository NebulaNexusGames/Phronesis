using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBody))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Аниматор")]
    [SerializeField] private Animator animator;

    [Header("Имена параметров")]
    [SerializeField] private string groundParam = "Grounded";
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string crouchParam = "Crouch";
    [SerializeField] private string dashParam = "Dash";
    [SerializeField] private string verticalParam = "VerticalSpeed";

    [Header("Названия триггеров")]
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string landTrigger = "Land";
    [SerializeField] private string hardLandTrigger = "HardLand";
    [SerializeField] private string mergeTrigger = "Merge";
    [SerializeField] private string splitTrigger = "Split";

    [Header("Интеракции")]
    [SerializeField] private float mergeDuration = 0.8f;
    [SerializeField] private float splitDuration = 0.8f;

    [Header("Пороги")]
    [SerializeField] private float hardLandThreshold = 9f;
    [SerializeField] private float jumpTriggerVy = 0.2f;

    private CharacterBody body;
    private bool wasGrounded;
    private float lastVy;

    private bool interactLock;
    private float lockTimer;

    private bool dialLock;
    private bool modelHidden;
    private float hideModelTimer;

    public bool IsInteracting => interactLock || dialLock;

    private void Awake()
    {
        body = GetComponent<CharacterBody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (body == null) return;

        float vy = body.Velocity.y;
        bool grounded = body.groundedNow;

        if (animator != null)
        {
            SetBool(groundParam, grounded);
            SetFloat(speedParam, Mathf.Abs(body.Velocity.x));
            SetBool(crouchParam, body.IsCrouched);
            SetBool(dashParam, body.dashing);
            SetFloat(verticalParam, vy);

            if (grounded && !wasGrounded)
            {
                if (lastVy <= -hardLandThreshold)
                    SetTriggerSafe(hardLandTrigger);
                else
                    SetTriggerSafe(landTrigger);
            }
            else if (!grounded && wasGrounded && vy >= jumpTriggerVy)
            {
                SetTriggerSafe(jumpTrigger);
            }
        }

        wasGrounded = grounded;
        lastVy = vy;

        if (!dialLock && interactLock)
        {
            lockTimer -= Time.deltaTime;
            if (lockTimer <= 0f)
                interactLock = false;
        }

        if (hideModelTimer > 0f)
        {
            hideModelTimer -= Time.deltaTime;
            if (hideModelTimer <= 0f)
                RestoreModelIfSafe();
        }
    }

    public void PlayMerge() { LockFor(mergeDuration); SetTriggerSafe(mergeTrigger); }
    public void PlaySplit() { LockFor(splitDuration); SetTriggerSafe(splitTrigger); }

    public void BeginPet(float duration)
    {
        LockFor(duration);
        hideModelTimer = Mathf.Max(hideModelTimer, duration);
        SetModelHidden(true);
    }

    public void BeginDialogueHide()
    {
        dialLock = true;
        SetModelHidden(true);
    }

    public void EndDialogueHide()
    {
        if (!dialLock) return;
        dialLock = false;
        interactLock = false;
        lockTimer = 0f;
        hideModelTimer = 0f;
        RestoreModelIfSafe();
    }

    public bool IsDialogueHideActive => dialLock;

    private void LockFor(float duration)
    {
        interactLock = true;
        lockTimer = Mathf.Max(lockTimer, duration);
    }

    private void SetModelHidden(bool value)
    {
        if (modelHidden == value) return;
        modelHidden = value;
        if (body != null) body.SetModelVisible(!value);
    }

    private void RestoreModelIfSafe()
    {
        if (modelHidden && !dialLock)
            SetModelHidden(false);
    }

    private void SetBool(string param, bool value)
    {
        if (animator != null) animator.SetBool(param, value);
    }

    private void SetFloat(string param, float value)
    {
        if (animator != null) animator.SetFloat(param, value);
    }

    private void SetTriggerSafe(string param)
    {
        if (animator != null) animator.SetTrigger(param);
    }
}