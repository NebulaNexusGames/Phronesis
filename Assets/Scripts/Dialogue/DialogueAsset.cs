using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    [Header("Кто говорит")]
    public string npcName = "NPC";
    public string playerName = "Player";

    [Header("Портреты по умолчанию")]
    public Sprite npcPortrait;
    public Sprite playerPortrait;

    [Header("Голоса по умолчанию")]
    public AudioClip npcVoice;
    public AudioClip playerVoice;

    [Header("Фоны окна по умолчанию")]
    public Sprite npcWindowBackground;
    public Sprite playerWindowBackground;

    [Header("Реплики (nextLineIndex = -1 значит конец диалога)")]
    public int startIndex;
    public List<DialogueLine> lines = new List<DialogueLine>();
}

[Serializable]
public class DialogueLine
{
    public const int AutoNextLine = -2;
    public const int EndDialogue = -1;

    [Tooltip("Говорит игрок (иначе — персонаж)")]
    public bool isPlayerSpeaking;

    [Tooltip("Реплика покажется и диалог сразу на ней закончится")]
    public bool endDialogueOnThisLine;

    [Tooltip("-2 = авто (следующая строка по порядку), -1 = конец диалога, 0+ = перейти к строке с этим индексом")]
    public int nextLineIndex = AutoNextLine;

    [HideInInspector] public string nodeId;
    [HideInInspector] public Vector2 nodePosition;

    [TextArea(2, 6)] public string text;

    public Sprite npcPortraitOverride;
    public Sprite playerPortraitOverride;

    [Tooltip("Пусто = дефолтное имя из настроек ассета")]
    public string npcNameOverride;

    [Tooltip("Пусто = дефолтное имя из настроек ассета")]
    public string playerNameOverride;

    [Tooltip("Пусто = дефолтный голос говорящего из ассета")]
    public AudioClip npcVoiceOverride;

    [Tooltip("Пусто = дефолтный голос говорящего из ассета")]
    public AudioClip playerVoiceOverride;

    [Tooltip("Пусто = реплика продолжается по кнопке/клику, иначе в конце появятся кнопки выбора")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)] public string choiceText;
    public int nextLineIndex = -1;
}
