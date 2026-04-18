using UnityEngine;

/// <summary>
/// Hiệu ứng rơi xuống nước: splash + vòng sóng ngắn.
/// </summary>
public class WaterSplashEffect : MonoBehaviour
{
    private static Sprite cachedBlobSprite;
    private static Sprite cachedRingSprite;

    private float elapsed;
    private float lifeTime = 0.6f;
    private SpriteRenderer blobRenderer;
    private SpriteRenderer ringRenderer;

    public static void Spawn(Vector3 worldPosition)
    {
        GameObject splashObject = new GameObject("WaterSplashEffect");
        splashObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        WaterSplashEffect effect = splashObject.AddComponent<WaterSplashEffect>();
        effect.Initialize();
    }

    private void Initialize()
    {
        GameObject blobObject = new GameObject("SplashBlob");
        blobObject.transform.SetParent(transform, false);
        blobRenderer = blobObject.AddComponent<SpriteRenderer>();
        blobRenderer.sprite = GetBlobSprite();
        blobRenderer.color = new Color(0.82f, 0.98f, 1f, 0.9f);
        blobRenderer.sortingOrder = 45;
        blobObject.transform.localScale = new Vector3(0.75f, 0.55f, 1f);

        GameObject ringObject = new GameObject("SplashRing");
        ringObject.transform.SetParent(transform, false);
        ringRenderer = ringObject.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = GetRingSprite();
        ringRenderer.color = new Color(1f, 1f, 1f, 0.72f);
        ringRenderer.sortingOrder = 44;
        ringObject.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifeTime);

        if (blobRenderer != null)
        {
            blobRenderer.transform.localScale = Vector3.Lerp(new Vector3(0.75f, 0.55f, 1f), new Vector3(1.45f, 0.28f, 1f), t);
            Color color = blobRenderer.color;
            color.a = Mathf.Lerp(0.9f, 0f, t);
            blobRenderer.color = color;
        }

        if (ringRenderer != null)
        {
            ringRenderer.transform.localScale = Vector3.Lerp(new Vector3(0.55f, 0.55f, 1f), new Vector3(2.15f, 2.15f, 1f), t);
            Color color = ringRenderer.color;
            color.a = Mathf.Lerp(0.72f, 0f, t);
            ringRenderer.color = color;
        }

        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetBlobSprite()
    {
        if (cachedBlobSprite == null)
        {
            cachedBlobSprite = CreateCircleSprite(80, 34f, 34f);
        }

        return cachedBlobSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (cachedRingSprite == null)
        {
            Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(47.5f, 47.5f);
            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool ring = distance <= 44f && distance >= 33f;
                    texture.SetPixel(x, y, ring ? Color.white : clear);
                }
            }

            texture.Apply();
            cachedRingSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return cachedRingSprite;
    }

    private static Sprite CreateCircleSprite(int size, float radiusX, float radiusY)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center.x) / radiusX;
                float ny = (y - center.y) / radiusY;
                texture.SetPixel(x, y, (nx * nx + ny * ny) <= 1f ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
