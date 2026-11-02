using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DialogueChoiceButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    public event UnityAction<int> Clicked;

    private int nextLineIndex = -1;
    private bool armed;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
        GetComponent<Button>().onClick.AddListener(HandleClick);
    }

    public void Setup(string text, int targetLineIndex)
    {
        if (label != null) label.text = text;
        nextLineIndex = targetLineIndex;
        armed = true;
    }

    private void HandleClick()
    {
        if (!armed) return;
        armed = false;
        Clicked?.Invoke(nextLineIndex);
    }
}
