using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DistanceFadeAudio : MonoBehaviour
{
    [Header("Слушатель")]
    [SerializeField] private Transform listener;

    [Header("Дистанции (в юнитах мира)")]
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 20f;

    [Header("Громкость")]
    [SerializeField, Range(0f, 1f)] private float multiplier = 1f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (listener == null)
            listener = FindAudioListener();
    }

    private void Update()
    {
        if (source == null) return;
        if (listener == null)
        {
            listener = FindAudioListener();
            if (listener == null)
            {
                source.volume = source.volume;
                return;
            }
        }

        float dist = Vector2.Distance(transform.position, listener.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
        source.volume = (1f - t) * multiplier;
    }

    private static Transform FindAudioListener()
    {
        AudioListener l = Object.FindObjectOfType<AudioListener>();
        return l != null ? l.transform : null;
    }
}
