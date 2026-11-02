using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public enum BorderMode
    {
        MoveCamera,
        ClampInsideScreen
    }

    [Header("Камера и поведение у границ")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BorderMode borderMode = BorderMode.MoveCamera;

    [Header("Точка объекта — центр слежения")]
    [SerializeField] private Transform followPoint;

    [SerializeField] private float followSmoothness = 8f;
    [SerializeField] private float screenPadding = 0.1f;

    private Rigidbody2D body;
    private readonly List<Collider2D> colliders = new List<Collider2D>();
    private Bounds combinedBounds;
    private bool boundsInitialized;
    private BorderMode originalMode;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        originalMode = borderMode;
    }

    public void SetTemporaryMode(BorderMode mode)
    {
        borderMode = mode;
    }

    public void RestoreMode()
    {
        borderMode = originalMode;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        if (borderMode == BorderMode.MoveCamera)
        {
            MoveCamera();
        }
    }

    private void FixedUpdate()
    {
        if (targetCamera == null) return;

        if (borderMode == BorderMode.ClampInsideScreen)
        {
            ClampToScreen();
        }
    }

    private Vector3 Anchor
    {
        get { return followPoint != null ? followPoint.position : transform.position; }
    }

    private void MoveCamera()
    {
        Vector3 anchor = Anchor;
        Vector3 goal = new Vector3(anchor.x, anchor.y, targetCamera.transform.position.z);
        float t = followSmoothness <= 0f ? 1f : 1f - Mathf.Exp(-followSmoothness * Time.deltaTime);
        targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, goal, t);
    }

    private void ClampToScreen()
    {
        RefreshBounds();

        Vector3 anchor = Anchor;
        Transform camT = targetCamera.transform;
        Vector3 localAnchor = camT.InverseTransformPoint(anchor);
        float depth = Mathf.Max(Mathf.Abs(localAnchor.z), 0.01f);

        float halfHeight = targetCamera.orthographic
            ? targetCamera.orthographicSize
            : Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * depth;
        float halfWidth = halfHeight * targetCamera.aspect;

        float marginX = boundsInitialized ? combinedBounds.extents.x : 0f;
        float marginY = boundsInitialized ? combinedBounds.extents.y : 0f;
        marginX += screenPadding;
        marginY += screenPadding;

        float minX = camT.position.x - halfWidth + marginX;
        float maxX = camT.position.x + halfWidth - marginX;
        float minY = camT.position.y - halfHeight + marginY;
        float maxY = camT.position.y + halfHeight - marginY;

        Vector2 pos = body != null ? body.position : new Vector2(anchor.x, anchor.y);
        Vector2 vel = body != null ? body.linearVelocity : Vector2.zero;

        if (pos.x < minX) { pos.x = minX; if (vel.x < 0f) vel.x = 0f; }
        else if (pos.x > maxX) { pos.x = maxX; if (vel.x > 0f) vel.x = 0f; }

        if (pos.y < minY) { pos.y = minY; if (vel.y < 0f) vel.y = 0f; }
        else if (pos.y > maxY) { pos.y = maxY; if (vel.y > 0f) vel.y = 0f; }

        if (body != null)
        {
            body.position = pos;
            body.linearVelocity = vel;
        }
        transform.position = new Vector3(pos.x, pos.y, anchor.z);
    }

    private void RefreshBounds()
    {
        GetComponentsInChildren(colliders);
        boundsInitialized = false;
        for (int i = 0; i < colliders.Count; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
            if (!boundsInitialized)
            {
                combinedBounds = col.bounds;
                boundsInitialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(col.bounds);
            }
        }
    }
}
