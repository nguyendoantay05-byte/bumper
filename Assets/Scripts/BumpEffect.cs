using UnityEngine;

/// <summary>
/// Hiệu ứng va chạm ngắn: vòng sáng bung ra rồi mờ dần.
/// Tạo runtime, không cần prefab riêng.
/// </summary>
public class BumpEffect : MonoBehaviour
{
    private static Sprite cachedRingSprite;

    private float lifeTime = 0.22f;
    private float elapsed;
    private Vector3 startScale;
    private Vector3 endScale;
    private SpriteRenderer cachedRenderer;

    public static void Spawn(Vector3 worldPosition, Color color, float scaleMultiplier = 1f)
    {
        GameObject effectObject = new GameObject("BumpEffect");
        effectObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

        BumpEffect effect = effectObject.AddComponent<BumpEffect>();
        effect.Initialize(color, scaleMultiplier);
    }

    private void Initialize(Color color, float scaleMultiplier)
    {
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRingSprite();
        renderer.color = color;
        renderer.sortingOrder = 40;
        cachedRenderer = renderer;

        float clampedScale = Mathf.Clamp(scaleMultiplier, 0.8f, 1.8f);
        startScale = new Vector3(0.45f, 0.45f, 1f) * clampedScale;
        endScale = new Vector3(1.75f, 1.75f, 1f) * clampedScale;
        transform.localScale = startScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifeTime);
        transform.localScale = Vector3.Lerp(startScale, endScale, t);

        if (cachedRenderer != null)
        {
            Color color = cachedRenderer.color;
            color.a = Mathf.Lerp(0.85f, 0f, t);
            cachedRenderer.color = color;
        }

        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetRingSprite()
    {
        if (cachedRingSprite == null)
        {
            cachedRingSprite = CreateRingSprite();
        }

        return cachedRingSprite;
    }

    private static Sprite CreateRingSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Vector2 center = new Vector2(47.5f, 47.5f);
        float outerRadius = 44f;
        float innerRadius = 31f;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool insideRing = distance <= outerRadius && distance >= innerRadius;
                texture.SetPixel(x, y, insideRing ? fill : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
