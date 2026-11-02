using UnityEngine;

/// <summary>Настройки взаимодействия: кто из персонажей (Love/Blu/Rose) может взаимодействовать с объектом.</summary>
[DisallowMultipleComponent]
public class InteractionSettings : MonoBehaviour
{
    [Header("Кто может взаимодействовать")]
    [SerializeField] private bool love = true;
    [SerializeField] private bool blu = true;
    [SerializeField] private bool rose = true;

    public bool AllowLove => love;
    public bool AllowBlu => blu;
    public bool AllowRose => rose;

    public bool Allows(CharacterBody body, MergeController merge)
    {
        if (body == null || merge == null) return true;
        if (body == merge.Love) return love;
        if (body == merge.Blu) return blu;
        if (body == merge.Rose) return rose;
        return love || blu || rose;
    }
}