using UnityEngine;

public class MergeController : MonoBehaviour
{
    [Header("Персонажи")]
    [SerializeField] private CharacterBody love;
    [SerializeField] private CharacterBody blu;
    [SerializeField] private CharacterBody rose;

    [Header("Здоровье")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Настройки слияния")]
    [SerializeField] private KeyCode mergeKey = KeyCode.F;
    [SerializeField] private float splitSeparation = 0.35f;
    [SerializeField] private float touchTolerance = 0.05f;

    public bool IsMerged { get; private set; } = true;

    public static MergeController Instance { get; private set; }

    /// <summary>Слияние запрещено, если хоть одна половинка мертва (0 ХП).</summary>
    public bool CanMerge
    {
        get
        {
            if (playerHealth == null) return true;
            return playerHealth.BluHp > 0 && playerHealth.RoseHp > 0;
        }
    }

    public CharacterBody Love => love;
    public CharacterBody Blu => blu;
    public CharacterBody Rose => rose;

    private void Start()
    {
        Instance = this;
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        ApplyState(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(mergeKey)) return;

        if (IsMerged)
        {
            Split();
        }
        else if (CanMerge && blu.Touches(rose, touchTolerance))
        {
            Merge();
        }
    }

    public void Split()
    {
        Vector2 origin = love.Position;
        Vector2 velocity = love.Velocity;
        ApplyState(false);
        blu.Teleport(origin + Vector2.left * splitSeparation);
        rose.Teleport(origin + Vector2.right * splitSeparation);
        blu.Body.linearVelocity = velocity;
        rose.Body.linearVelocity = velocity;
        TriggerSplit();
    }

    private void Merge()
    {
        if (!CanMerge) return;

        Vector2 middle = (blu.Position + rose.Position) * 0.5f;
        Vector2 velocity = (blu.Velocity + rose.Velocity) * 0.5f;
        love.Teleport(middle);
        love.Body.linearVelocity = velocity;
        ApplyState(true);
        TriggerMerge();
    }

    private void TriggerSplit()
    {
        PlaySplitOn(blu);
        PlaySplitOn(rose);
    }

    private void TriggerMerge()
    {
        PlayMergeOn(love);
    }

    private static void PlaySplitOn(CharacterBody body)
    {
        PlayerAnimator animator = body == null ? null : body.GetComponent<PlayerAnimator>();
        if (animator != null) animator.PlaySplit();
    }

    private static void PlayMergeOn(CharacterBody body)
    {
        PlayerAnimator animator = body == null ? null : body.GetComponent<PlayerAnimator>();
        if (animator != null) animator.PlayMerge();
    }

    private void ApplyState(bool merged)
    {
        IsMerged = merged;
        love.SetActiveState(merged);
        blu.SetActiveState(!merged);
        rose.SetActiveState(!merged);
    }
}
