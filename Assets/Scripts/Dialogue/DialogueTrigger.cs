using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Диалог")]
    [SerializeField] private DialogueAsset dialogue;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce;

    [Header("Кто может взаимодействовать")]
    [SerializeField] private InteractionSettings settings;

    private bool playerInside;
    private bool used;
    private CharacterBody insideBody;
    private PlayerAnimator hiddenAnimator;

    private void Update()
    {
        if (hiddenAnimator != null && (DialogueUI.Instance == null || !DialogueUI.Instance.IsOpen))
        {
            hiddenAnimator.EndDialogueHide();
            hiddenAnimator = null;
        }

        if (!playerInside || used) return;
        if (dialogue == null || DialogueUI.Instance == null) return;
        if (DialogueUI.Instance.IsOpen) return;

        bool pressed = Input.GetKeyDown(KeyCode.E) ||
                       PlayerInput.InteractPressed(ControlLayout.Player1Standard) ||
                       PlayerInput.InteractPressed(ControlLayout.Player2Arrows);
        if (!pressed) return;

        if (insideBody == null) return;
        if (settings != null && !settings.Allows(insideBody, MergeController.Instance)) return;

        DialogueUI.Instance.StartDialogue(dialogue);
        if (triggerOnce) used = true;

        if (insideBody != null)
        {
            PlayerAnimator pa = insideBody.GetComponent<PlayerAnimator>();
            if (pa != null && !pa.IsDialogueHideActive)
            {
                pa.BeginDialogueHide();
                hiddenAnimator = pa;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        CharacterBody cb = other.GetComponent<CharacterBody>();
        if (cb == null) cb = other.GetComponentInParent<CharacterBody>();
        if (cb != null) insideBody = cb;
        playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        CharacterBody cb = other.GetComponent<CharacterBody>();
        if (cb == null) cb = other.GetComponentInParent<CharacterBody>();
        if (cb != null && cb == insideBody) insideBody = null;
        playerInside = false;
    }
}