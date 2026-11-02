using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("Окно диалога (изначально выключенный объект)")]
    [SerializeField] private GameObject window;

    [Tooltip("Image фона окна; спрайт переключается по говорящему")]
    [SerializeField] private Image background;

    [Header("Портреты")]
    [SerializeField] private Image npcPortrait;
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Color activePortraitColor = Color.white;
    [SerializeField] private Color inactivePortraitColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Фон вариантов ответа (подстраивается под окно игрока)")]
    [SerializeField] private Image choicesBackground;

    [Header("Масштаб портретов")]
    [Tooltip("Во сколько раз портрет говорящего больше молчащего")]
    [SerializeField, Min(1f)] private float speakingPortraitScale = 1.15f;
    [SerializeField, Min(0f)] private float portraitScaleLerpSpeed = 12f;

    [Tooltip("Сдвиг говорящего портрета к окну текста и чуть вверх (в пикселях). Знак X задаёт направление")]
    [SerializeField] private Vector2 npcSpeakingOffset = new Vector2(40f, 25f);
    [SerializeField] private Vector2 playerSpeakingOffset = new Vector2(-40f, 25f);

    private Vector3 npcBaseScale = Vector3.one;
    private Vector3 playerBaseScale = Vector3.one;
    private float npcScaleTarget = 1f;
    private float playerScaleTarget = 1f;

    private RectTransform npcRect;
    private RectTransform playerRect;
    private Vector2 npcBaseAnchoredPos;
    private Vector2 playerBaseAnchoredPos;

    [Header("Текст")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Скорость появления текста (символов в секунду)")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 40f;

    [Header("Голос (блип на каждую букву)")]
    [SerializeField] private AudioSource voiceSource;

    [Tooltip("Сколько секунд звучит блип одного символа")]
    [SerializeField, Min(0.01f)] private float voiceBlipDuration = 0.25f;

    private AudioClip activeVoiceClip;

    [Header("Варианты ответа")]
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private DialogueChoiceButton choiceButtonTemplate;
    [Tooltip("Расстояние между кнопками вариантов")]
    [SerializeField, Min(0f)] private float choiceSpacing = 10f;
    [Tooltip("Объект, который включается, когда диалог доходит до вариантов ответа")]
    [SerializeField] private GameObject choicesSection;

    [Header("Продолжение диалога без выборов")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space;
    [SerializeField] private bool continueOnMouseClick = true;

    [Header("События")]
    public UnityEvent onDialogueStarted;
    public UnityEvent onDialogueEnded;

    private DialogueAsset currentDialogue;
    private int currentIndex;
    private readonly List<GameObject> spawnedChoices = new List<GameObject>();
    private Coroutine typingRoutine;
    private float choicesShownAt = -10f;
    private string currentRichText = string.Empty;

    private static readonly Regex ColorOpenRegex = new Regex(@"\[([0-9a-fA-F]{3}|[0-9a-fA-F]{6})\]");
    private static readonly Regex ColorCloseRegex = new Regex(@"\[/#\]");

    public bool IsOpen => window != null && window.activeSelf;
    public bool IsTyping { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (window != null) window.SetActive(false);
        if (choiceButtonTemplate != null) choiceButtonTemplate.gameObject.SetActive(false);
        if (choicesSection != null) choicesSection.SetActive(false);

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
        }

        if (npcPortrait != null)
        {
            npcBaseScale = npcPortrait.transform.localScale;
            npcRect = npcPortrait.transform as RectTransform;
            if (npcRect != null) npcBaseAnchoredPos = npcRect.anchoredPosition;
        }
        if (playerPortrait != null)
        {
            playerBaseScale = playerPortrait.transform.localScale;
            playerRect = playerPortrait.transform as RectTransform;
            if (playerRect != null) playerBaseAnchoredPos = playerRect.anchoredPosition;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;

        UpdatePortraitScales();

        bool pressed = Input.GetKeyDown(continueKey) || (continueOnMouseClick && Input.GetMouseButtonDown(0));
        if (!pressed) return;

        if (IsTyping)
        {
            CompleteTyping();
            return;
        }

        DialogueLine line = GetCurrentLine();
        if (line == null)
        {
            EndDialogue();
            return;
        }
        if (line.choices.Count > 0) return;

        if (line.endDialogueOnThisLine) EndDialogue();
        else Advance();
    }

    public void StartDialogue(DialogueAsset dialogue)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Count == 0) return;
        StopTyping();
        ClearChoices();
        currentDialogue = dialogue;
        currentIndex = Mathf.Clamp(dialogue.startIndex, 0, dialogue.lines.Count - 1);
        if (window != null) window.SetActive(true);
        onDialogueStarted.Invoke();
        ShowLine();
    }

    public void EndDialogue()
    {
        StopTyping();
        if (voiceSource != null) voiceSource.Stop();
        ClearChoices();
        currentDialogue = null;
        if (window != null) window.SetActive(false);
        onDialogueEnded.Invoke();
    }

    private DialogueLine GetCurrentLine()
    {
        if (currentDialogue == null || currentIndex < 0 || currentIndex >= currentDialogue.lines.Count) return null;
        return currentDialogue.lines[currentIndex];
    }

    private void ShowLine()
    {
        ClearChoices();
        if (choicesSection != null) choicesSection.SetActive(false);
        DialogueLine line = GetCurrentLine();
        if (line == null)
        {
            EndDialogue();
            return;
        }

        ApplyPortrait(npcPortrait, ResolveSprite(line.npcPortraitOverride, currentDialogue.npcPortrait), !line.isPlayerSpeaking);
        ApplyPortrait(playerPortrait, ResolveSprite(line.playerPortraitOverride, currentDialogue.playerPortrait), line.isPlayerSpeaking);

        npcScaleTarget = line.isPlayerSpeaking ? 1f : speakingPortraitScale;
        playerScaleTarget = line.isPlayerSpeaking ? speakingPortraitScale : 1f;

        Sprite windowBackground = line.isPlayerSpeaking
            ? currentDialogue.playerWindowBackground
            : currentDialogue.npcWindowBackground;
        if (background != null && windowBackground != null) background.sprite = windowBackground;
        if (choicesBackground != null && currentDialogue.playerWindowBackground != null)
        {
            choicesBackground.sprite = currentDialogue.playerWindowBackground;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = line.isPlayerSpeaking
                ? ConvertColorTags(ResolveName(line.playerNameOverride, currentDialogue.playerName))
                : ConvertColorTags(ResolveName(line.npcNameOverride, currentDialogue.npcName));
        }

        activeVoiceClip = line.isPlayerSpeaking
            ? (line.playerVoiceOverride != null ? line.playerVoiceOverride : currentDialogue.playerVoice)
            : (line.npcVoiceOverride != null ? line.npcVoiceOverride : currentDialogue.npcVoice);

        StartTyping(ConvertColorTags(line.text));
    }

    private static string ConvertColorTags(string source)
    {
        if (string.IsNullOrEmpty(source)) return source ?? string.Empty;
        string result = ColorOpenRegex.Replace(source, ExpandHex);
        return ColorCloseRegex.Replace(result, "</color>");
    }

    private static string ExpandHex(Match match)
    {
        string hex = match.Groups[1].Value;
        if (hex.Length == 3)
        {
            hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
        }
        return "<#" + hex + ">";
    }

    private void UpdatePortraitScales()
    {
        float dt = Time.unscaledDeltaTime;
        float t = portraitScaleLerpSpeed <= 0f ? 1f : Mathf.Min(1f, portraitScaleLerpSpeed * dt);

        float npcAmount = Mathf.InverseLerp(1f, speakingPortraitScale, npcScaleTarget);
        float playerAmount = Mathf.InverseLerp(1f, speakingPortraitScale, playerScaleTarget);

        if (npcPortrait != null)
        {
            Vector3 goalScale = Vector3.Scale(npcBaseScale, new Vector3(npcScaleTarget, npcScaleTarget, 1f));
            npcPortrait.transform.localScale = Vector3.Lerp(npcPortrait.transform.localScale, goalScale, t);
            if (npcRect != null)
            {
                Vector2 goalPos = npcBaseAnchoredPos + npcSpeakingOffset * npcAmount;
                npcRect.anchoredPosition = Vector2.Lerp(npcRect.anchoredPosition, goalPos, t);
            }
        }
        if (playerPortrait != null)
        {
            Vector3 goalScale = Vector3.Scale(playerBaseScale, new Vector3(playerScaleTarget, playerScaleTarget, 1f));
            playerPortrait.transform.localScale = Vector3.Lerp(playerPortrait.transform.localScale, goalScale, t);
            if (playerRect != null)
            {
                Vector2 goalPos = playerBaseAnchoredPos + playerSpeakingOffset * playerAmount;
                playerRect.anchoredPosition = Vector2.Lerp(playerRect.anchoredPosition, goalPos, t);
            }
        }
    }

    private static Sprite ResolveSprite(Sprite overrideSprite, Sprite fallback)
    {
        return overrideSprite != null ? overrideSprite : fallback;
    }

    private static string ResolveName(string overrideName, string fallback)
    {
        return string.IsNullOrEmpty(overrideName) ? fallback : overrideName;
    }

    private void ApplyPortrait(Image portrait, Sprite sprite, bool isSpeaking)
    {
        if (portrait == null) return;
        portrait.gameObject.SetActive(sprite != null);
        if (sprite == null) return;
        portrait.sprite = sprite;
        portrait.color = isSpeaking ? activePortraitColor : inactivePortraitColor;
    }

    private void StartTyping(string text)
    {
        StopTyping();
        typingRoutine = StartCoroutine(TypeRoutine(text ?? string.Empty));
    }

    private void PlayVoiceBlip()
    {
        if (activeVoiceClip == null || voiceSource == null) return;

        voiceSource.Stop();
        voiceSource.clip = activeVoiceClip;
        voiceSource.timeSamples = activeVoiceClip.samples / 2;
        voiceSource.Play();
        voiceSource.SetScheduledEndTime(AudioSettings.dspTime + voiceBlipDuration);
    }

    private IEnumerator TypeRoutine(string text)
    {
        IsTyping = true;
        currentRichText = text;
        if (dialogueText != null) dialogueText.text = string.Empty;
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f / charactersPerSecond);
        StringBuilder shown = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<')
            {
                int close = text.IndexOf('>', i);
                if (close != -1)
                {
                    shown.Append(text, i, close - i + 1);
                    if (dialogueText != null) dialogueText.text = shown.ToString();
                    i = close;
                    continue;
                }
            }

            shown.Append(text[i]);
            if (dialogueText != null) dialogueText.text = shown.ToString();
            if (!char.IsWhiteSpace(text[i])) PlayVoiceBlip();
            yield return wait;
        }

        FinishLine();
    }

    private void CompleteTyping()
    {
        StopTyping();
        if (dialogueText != null) dialogueText.text = currentRichText;
        FinishLine();
    }

    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
        IsTyping = false;
    }

    private void FinishLine()
    {
        IsTyping = false;
        DialogueLine line = GetCurrentLine();
        if (line == null || line.endDialogueOnThisLine || line.choices.Count == 0) return;

        SpawnChoices(line.choices);
    }

    private void SpawnChoices(List<DialogueChoice> choices)
    {
        if (choiceButtonTemplate == null || choicesContainer == null) return;

        RectTransform templateRect = (RectTransform)choiceButtonTemplate.transform;
        Vector2 origin = templateRect.anchoredPosition;
        float step = templateRect.rect.height + choiceSpacing;

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];
            DialogueChoiceButton button = Instantiate(choiceButtonTemplate, choicesContainer);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = templateRect.anchorMin;
            rect.anchorMax = templateRect.anchorMax;
            rect.pivot = templateRect.pivot;
            rect.sizeDelta = templateRect.sizeDelta;
            rect.anchoredPosition = origin - new Vector2(0f, step * i);
            button.Setup(choice.choiceText, choice.nextLineIndex);
            button.Clicked += HandleChoiceClicked;
            button.gameObject.SetActive(true);
            spawnedChoices.Add(button.gameObject);
        }

        if (choicesSection != null) choicesSection.SetActive(true);
        choicesShownAt = Time.unscaledTime;
    }

    private void HandleChoiceClicked(int nextLineIndex)
    {
        if (Time.unscaledTime - choicesShownAt < 0.15f) return;
        ClearChoices();
        JumpTo(nextLineIndex);
    }

    private void Advance()
    {
        DialogueLine line = GetCurrentLine();
        if (line == null)
        {
            EndDialogue();
            return;
        }
        if (line.nextLineIndex == DialogueLine.AutoNextLine) JumpTo(currentIndex + 1);
        else JumpTo(line.nextLineIndex);
    }

    private void JumpTo(int lineIndex)
    {
        if (currentDialogue == null || lineIndex < 0 || lineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }
        currentIndex = lineIndex;
        ShowLine();
    }

    private void ClearChoices()
    {
        for (int i = 0; i < spawnedChoices.Count; i++)
        {
            if (spawnedChoices[i] != null) Destroy(spawnedChoices[i]);
        }
        spawnedChoices.Clear();
        if (choicesSection != null) choicesSection.SetActive(false);
    }
}
