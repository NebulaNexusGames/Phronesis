using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Corpse : MonoBehaviour
{
    [Header("Вращение")]
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private Vector3 rotateAxis = Vector3.forward;

    private void Update()
    {
        transform.Rotate(rotateAxis * rotateSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
    }
}
