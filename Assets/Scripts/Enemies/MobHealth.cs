using UnityEngine;

public class MobHealth : MonoBehaviour
{
    [Header("ХП")]
    [SerializeField] private int maxHp = 3;

    private int hp;

    public int Hp => hp;
    public bool IsDead => hp <= 0;

    private void Start()
    {
        hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        hp = Mathf.Max(0, hp - amount);
        if (IsDead) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        hp = Mathf.Min(maxHp, hp + amount);
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        int displayHp = Application.isPlaying ? hp : maxHp;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.8f,
            $"HP: {displayHp}/{maxHp}"
        );
    }
}
