using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfettiBurst : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Parent under the Canvas where confetti UI Images will be spawned (usually the whole screen).")]
    public RectTransform spawnArea;           // Assign: your Canvas_Quiz root (RectTransform)
    [Tooltip("Confetti sprites (small shapes). If empty, a single-color square will be used.")]
    public Sprite[] confettiSprites;          // Optional: small sprites

    [Header("Burst Settings")]
    public int pieces = 80;                   // number of confetti
    public float minSpeed = 750f;             // px/sec
    public float maxSpeed = 1300f;
    public float minLifetime = 1.2f;
    public float maxLifetime = 2.2f;
    public Vector2 horizontalSpread = new Vector2(-600f, 600f);  // initial x velocity range
    public Vector2 gravity = new Vector2(0f, -2000f);            // px/sec^2 downward
    public Vector2 startScaleRange = new Vector2(0.6f, 1.3f);
    public Vector2 spinRange = new Vector2(-360f, 360f);         // deg/sec
    public bool clampToScreen = true;                             // optional: keep within spawnArea bounds

    [Header("Colors")]
    public Color[] palette = new Color[] {
        new Color(1f, 0.5f, 0f),   // orange
        new Color(1f, 0.84f, 0f),  // gold
        new Color(0.3f, 0.7f, 1f), // sky blue
        new Color(0.45f, 0.9f, 0.5f), // green
        Color.white
    };

    [Header("Optional")]
    public Sprite fallbackSquare;             // assign a tiny white square sprite (16x16) if available
    public bool raycastTarget = false;        // keep false so it doesn't block UI clicks

    public void Play()
    {
        if (!spawnArea) {
            var rt = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (rt) spawnArea = rt;
        }
        StartCoroutine(BurstRoutine());
    }

    IEnumerator BurstRoutine()
    {
        var area = spawnArea ? spawnArea : (RectTransform)transform;
        var size = area.rect.size;

        // Center-top spawn line
        Vector2 origin = new Vector2(0f, size.y * 0.35f); // slightly above center
        for (int i = 0; i < pieces; i++)
        {
            SpawnOne(area, origin, size);
            // slight stagger so it looks lively
            yield return null;
        }
    }

    void SpawnOne(RectTransform parent, Vector2 origin, Vector2 areaSize)
    {
        // Create UI Image
        var go = new GameObject("Confetti", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.layer = parent.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.8f); // start near top-middle
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Pick sprite & color
        var img = go.GetComponent<Image>();
        img.raycastTarget = raycastTarget;
        img.sprite = (confettiSprites != null && confettiSprites.Length > 0) ? confettiSprites[Random.Range(0, confettiSprites.Length)] : fallbackSquare;
        img.color = palette[Random.Range(0, palette.Length)];

        // Random size
        float s = Random.Range(startScaleRange.x, startScaleRange.y) * 1f;
        rt.sizeDelta = Vector2.one * (img.sprite ? Mathf.Max(img.sprite.rect.width, 12f) : 16f) * s;

        // Initial position jitter along the top area
        float xJitter = Random.Range(-areaSize.x * 0.25f, areaSize.x * 0.25f);
        rt.anchoredPosition = new Vector2(origin.x + xJitter, origin.y);

        // Motion params
        float life = Random.Range(minLifetime, maxLifetime);
        float speed = Random.Range(minSpeed, maxSpeed);
        float vx = Random.Range(horizontalSpread.x, horizontalSpread.y);
        float vy = speed * 1f;
        float spin = Random.Range(spinRange.x, spinRange.y);

        StartCoroutine(AnimateOne(go, rt, img, life, vx, vy, spin));
    }

    IEnumerator AnimateOne(GameObject go, RectTransform rt, Image img, float life, float vx, float vy, float spin)
    {
        float t = 0f;
        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        Vector2 vel = new Vector2(vx, vy);
        Vector2 pos = rt.anchoredPosition;
        var grav = gravity;

        while (t < life)
        {
            float dt = Time.unscaledDeltaTime; // unscaled so it plays during pauses
            t += dt;

            // Integrate velocity
            vel += grav * dt;
            pos += vel * dt;

            // Apply position + rotation
            rt.anchoredPosition = pos;
            rt.Rotate(0f, 0f, spin * dt);

            // Fade out at the end
            float remain = Mathf.InverseLerp(life, life * 0.7f, t); // 0 at start, 1 near end
            cg.alpha = 1f - Mathf.Clamp01(remain);

            // Optional clamp
            if (clampToScreen)
            {
                var parentSize = ((RectTransform)rt.parent).rect.size;
                pos.x = Mathf.Clamp(pos.x, -parentSize.x * 0.5f, parentSize.x * 0.5f);
                rt.anchoredPosition = pos;
            }

            yield return null;
        }

        Destroy(go);
    }
}
