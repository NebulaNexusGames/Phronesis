using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private enum Half { None, Blu, Rose }

    [Header("Персонажи")]
    [SerializeField] private MergeController merge;

    [Header("ХП")]
    [SerializeField] private int maxHp = 3;

    [Header("Урон по половинкам")]
    [SerializeField] [Range(0.5f, 1f)] private float sameHalfBias = 0.7f;
    private Half lastDamagedHalf;

    [Header("Трупы")]
    [SerializeField] private GameObject bluCorpse;
    [SerializeField] private GameObject roseCorpse;
    [SerializeField] private float corpseDelay = 1f;
    [SerializeField] private float reviveHoldTime = 1.5f;

    [Header("Шкала тактильности")]
    [SerializeField] private float tactilityFillSpeed = 0.15f;
    [SerializeField] private float tactilityDrainSpeed = 0.35f;
    [SerializeField] private float tactilityLerpSpeed = 2f;
    [SerializeField] private float healDuration = 2f;

    [Header("Интерфейс — ячейки HP")]
    [SerializeField] private GameObject[] bluHpCells;
    [SerializeField] private GameObject[] roseHpCells;
    [SerializeField] private Slider tactilitySlider;

    [Header("Экран смерти")]
    [SerializeField] private GameObject deathScreen;

    [Header("Щит бессмертия")]
    [SerializeField] private float shieldAfterHit = 1.2f;
    private float shieldTimer;

    private int bluHp;
    private int roseHp;
    private float tactility;
    private bool healing;
    private float healTimer;
    private float corpseTimer;
    private CharacterBody deadBody;
    private GameObject activeCorpse;
    private float reviveTimer;
    private bool reviving;

    public int BluHp => bluHp;
    public int RoseHp => roseHp;
    public bool BothDead => bluHp <= 0 && roseHp <= 0;

    private void Start()
    {
        bluHp = maxHp;
        roseHp = maxHp;
        if (deathScreen != null) deathScreen.SetActive(false);
        UpdateUI();
    }

    private void Update()
    {
        if (shieldTimer > 0f) shieldTimer -= Time.deltaTime;

        if (BothDead)
        {
            if (deathScreen != null) deathScreen.SetActive(true);
            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (healing)
        {
            HealTick();
            return;
        }

        HandleTactilityFill();
        HandleTactilityUse();
        HandleCorpseSpawn();
        HandleRevive();
    }

    public void TakeDamage(int amount)
    {
        if (shieldTimer > 0f) return;

        if (merge.IsMerged)
        {
            if (Random.value < 0.5f)
                bluHp = Mathf.Max(0, bluHp - amount);
            else
                roseHp = Mathf.Max(0, roseHp - amount);
        }
        else
        {
            if (merge.Blu != null && merge.Blu.gameObject.activeInHierarchy)
                bluHp = Mathf.Max(0, bluHp - amount);
            else if (merge.Rose != null && merge.Rose.gameObject.activeInHierarchy)
                roseHp = Mathf.Max(0, roseHp - amount);
        }

        UpdateUI();

        if (merge.IsMerged)
        {
            if (bluHp <= 0 || roseHp <= 0)
                merge.Split();
        }

        if (!merge.IsMerged)
        {
            if (bluHp <= 0 && merge.Blu != null && merge.Blu.gameObject.activeInHierarchy)
                KillCharacter(merge.Blu);
            if (roseHp <= 0 && merge.Rose != null && merge.Rose.gameObject.activeInHierarchy)
                KillCharacter(merge.Rose);
        }

        shieldTimer = shieldAfterHit;
    }

    public bool TakeDamageBoth(int amount)
    {
        bool bluWasAlive = bluHp > 0;
        bool roseWasAlive = roseHp > 0;

        if (bluHp > 0 && roseHp > 0)
        {
            bool pickBlu;
            if (lastDamagedHalf == Half.Blu)
                pickBlu = Random.value < sameHalfBias;
            else if (lastDamagedHalf == Half.Rose)
                pickBlu = Random.value >= sameHalfBias;
            else
                pickBlu = Random.value < 0.5f;

            if (pickBlu)
            {
                bluHp = Mathf.Max(0, bluHp - amount);
                lastDamagedHalf = Half.Blu;
            }
            else
            {
                roseHp = Mathf.Max(0, roseHp - amount);
                lastDamagedHalf = Half.Rose;
            }
        }
        else if (bluHp > 0)
        {
            bluHp = Mathf.Max(0, bluHp - amount);
            lastDamagedHalf = Half.Blu;
        }
        else if (roseHp > 0)
        {
            roseHp = Mathf.Max(0, roseHp - amount);
            lastDamagedHalf = Half.Rose;
        }

        UpdateUI();

        bool bluDied = bluWasAlive && bluHp <= 0;
        bool roseDied = roseWasAlive && roseHp <= 0;

        if (bluDied || roseDied)
        {
            if (merge.IsMerged)
                merge.Split();

            if (bluDied && merge.Blu != null && merge.Blu.gameObject.activeInHierarchy)
                KillCharacter(merge.Blu);
            if (roseDied && merge.Rose != null && merge.Rose.gameObject.activeInHierarchy)
                KillCharacter(merge.Rose);
        }

        shieldTimer = shieldAfterHit;
        return bluDied || roseDied;
    }

    private void KillCharacter(CharacterBody body)
    {
        deadBody = body;
        body.gameObject.SetActive(false);
        corpseTimer = corpseDelay;

        CharacterBody other = body == merge.Blu ? merge.Rose : merge.Blu;
        if (other != null && other.gameObject.activeInHierarchy)
        {
            FollowCamera cam = other.GetComponent<FollowCamera>();
            if (cam != null && cam.GetComponent<Camera>() != Camera.main)
                cam.SetTemporaryMode(FollowCamera.BorderMode.MoveCamera);
        }
    }

    private void HandleCorpseSpawn()
    {
        if (corpseTimer <= 0f) return;

        corpseTimer -= Time.deltaTime;
        if (corpseTimer > 0f) return;

        if (deadBody == null) return;

        Vector2 pos = deadBody.Position;

        GameObject corpse = deadBody == merge.Blu ? bluCorpse : roseCorpse;
        if (corpse == null) return;

        corpse.transform.position = new Vector3(pos.x, pos.y, corpse.transform.position.z);
        corpse.SetActive(true);
        activeCorpse = corpse;
    }

    private void HandleRevive()
    {
        if (activeCorpse == null || !activeCorpse.activeInHierarchy) return;

        CharacterBody survivor = null;
        if (merge.Blu != null && merge.Blu.gameObject.activeInHierarchy && deadBody == merge.Rose)
            survivor = merge.Blu;
        else if (merge.Rose != null && merge.Rose.gameObject.activeInHierarchy && deadBody == merge.Blu)
            survivor = merge.Rose;

        if (survivor == null) return;

        float dist = Vector2.Distance(survivor.Position, activeCorpse.transform.position);
        bool inRange = dist <= 1.5f;

        bool holding = false;
        if (merge.IsMerged)
            holding = PlayerInput.InteractHeld(ControlLayout.Player1Standard) || PlayerInput.InteractHeld(ControlLayout.Player2Arrows);
        else
        {
            if (survivor == merge.Blu)
                holding = PlayerInput.InteractHeld(ControlLayout.Player1Standard);
            else
                holding = PlayerInput.InteractHeld(ControlLayout.Player2Arrows);
        }

        if (inRange && holding)
        {
            reviveTimer += Time.deltaTime;
            if (reviveTimer >= reviveHoldTime)
                Revive();
        }
        else
        {
            reviveTimer = 0f;
        }
    }

    private void Revive()
    {
        if (activeCorpse != null) activeCorpse.SetActive(false);
        activeCorpse = null;

        CharacterBody revived = deadBody;

        if (revived == merge.Blu)
            bluHp = 1;
        else
            roseHp = 1;

        revived.gameObject.SetActive(true);
        revived.Teleport(merge.IsMerged ? merge.Love.Position : FindSurvivor().Position);

        CharacterBody other = revived == merge.Blu ? merge.Rose : merge.Blu;
        if (other != null && other.gameObject.activeInHierarchy)
        {
            FollowCamera otherCam = other.GetComponent<FollowCamera>();
            if (otherCam != null)
                otherCam.RestoreMode();
        }

        FollowCamera revivedCam = revived.GetComponent<FollowCamera>();
        if (revivedCam != null)
            revivedCam.SetTemporaryMode(FollowCamera.BorderMode.MoveCamera);

        deadBody = null;
        reviveTimer = 0f;

        UpdateUI();
    }

    private CharacterBody FindSurvivor()
    {
        if (merge.Blu != null && merge.Blu.gameObject.activeInHierarchy) return merge.Blu;
        if (merge.Rose != null && merge.Rose.gameObject.activeInHierarchy) return merge.Rose;
        return null;
    }

    private void HandleTactilityFill()
    {
        if (merge.IsMerged) return;
        if (bluHp <= 0 || roseHp <= 0) return;

        CharacterBody blu = merge.Blu;
        CharacterBody rose = merge.Rose;
        if (blu == null || rose == null) return;
        if (!blu.gameObject.activeInHierarchy || !rose.gameObject.activeInHierarchy) return;

        bool bluFacesRose = (rose.Position.x > blu.Position.x && blu.FacingDirection > 0f) ||
                            (rose.Position.x < blu.Position.x && blu.FacingDirection < 0f);
        bool roseFacesBlu = (blu.Position.x > rose.Position.x && rose.FacingDirection > 0f) ||
                            (blu.Position.x < rose.Position.x && rose.FacingDirection < 0f);

        if (bluFacesRose && roseFacesBlu)
            tactility = Mathf.Clamp01(tactility + tactilityFillSpeed * Time.deltaTime);

        UpdateUI();
    }

    private void HandleTactilityUse()
    {
        if (!merge.IsMerged) return;
        if (tactility < 1f) return;
        if (healing) return;

        if (PlayerInput.HealPressed())
        {
            healing = true;
            healTimer = healDuration;
        }
    }

    private void HealTick()
    {
        merge.Love.axisInput = 0f;
        merge.Love.Body.linearVelocity = new Vector2(0f, merge.Love.Body.linearVelocity.y);

        healTimer -= Time.deltaTime;
        tactility = Mathf.Max(0f, tactility - tactilityDrainSpeed * Time.deltaTime);

        if (healTimer <= 0f || tactility <= 0f)
        {
            bluHp = maxHp;
            roseHp = maxHp;
            healing = false;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (bluHpCells != null)
        {
            for (int i = 0; i < bluHpCells.Length; i++)
            {
                if (bluHpCells[i] != null)
                    bluHpCells[i].SetActive(i < bluHp);
            }
        }

        if (roseHpCells != null)
        {
            for (int i = 0; i < roseHpCells.Length; i++)
            {
                if (roseHpCells[i] != null)
                    roseHpCells[i].SetActive(i < roseHp);
            }
        }

        if (tactilitySlider != null)
        {
            float targetValue = Mathf.Lerp(tactilitySlider.minValue, tactilitySlider.maxValue, tactility);
            tactilitySlider.value = Mathf.MoveTowards(tactilitySlider.value, targetValue, tactilityLerpSpeed * Time.deltaTime);
        }
    }
}
