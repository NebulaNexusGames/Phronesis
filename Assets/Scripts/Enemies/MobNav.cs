using UnityEngine;

/// <summary>
/// Упрощённый навмеш-модуль (runtime, без бейка):
/// проверка пола и протяжённости площадки под мобами.
/// Используйте MobNav.HasFloor / FloorExtent перед прыжком.
/// </summary>
public static class MobNav
{
    /// <summary>True если под точкой есть пол в пределах maxDepth.</summary>
    public static bool HasFloor(Vector2 point, float maxDepth, LayerMask mask)
    {
        if (mask == 0) mask = ~0;
        RaycastHit2D[] hits = Physics2D.RaycastAll(point, Vector2.down, maxDepth, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.distance > maxDepth) continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Возвращает как далеко вдоль dirX от crawlPoint (точка УРОВНЯ ПОЛА чуть выше поверхности)
    /// идёт непрерывный пол. Если void раньше maxDist, вернёт расстояние до последнего твёрдого образца.
    /// step — шаг сэмплирования (0.15–0.2 рекомендуется).
    /// </summary>
    public static float FloorExtent(Vector2 crawlPoint, float dirX, float maxDist,
                                    float maxDepth, LayerMask mask, float step = 0.15f)
    {
        if (mask == 0) mask = ~0;
        if (step <= 0.02f) step = 0.15f;

        float dir = Mathf.Sign(dirX);
        float lastSolid = 0f;

        for (float d = step; d <= maxDist + step; d += step)
        {
            Vector2 p = crawlPoint + new Vector2(dir * d, 0f);
            if (!HasFloor(p, maxDepth, mask))
                return lastSolid;
            lastSolid = d;
        }
        return maxDist;
    }
}
