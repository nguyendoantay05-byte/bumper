using UnityEngine;

/// <summary>
/// Tạo chuyển động nhẹ cho bọt mép đảo và viền sáng của map.
/// </summary>
public class ArenaFoamAnimator : MonoBehaviour
{
    private SpriteRenderer waterFoam;
    private SpriteRenderer edgeHighlight;
    private Vector3 foamBaseScale;
    private Vector3 edgeBaseScale;

    private void Awake()
    {
        Transform foam = transform.Find("WaterFoam");
        Transform edge = transform.Find("IslandEdgeHighlight");

        waterFoam = foam != null ? foam.GetComponent<SpriteRenderer>() : null;
        edgeHighlight = edge != null ? edge.GetComponent<SpriteRenderer>() : null;

        foamBaseScale = foam != null ? foam.localScale : Vector3.one;
        edgeBaseScale = edge != null ? edge.localScale : Vector3.one;
    }

    private void Update()
    {
        float wave = (Mathf.Sin(Time.time * 1.8f) + 1f) * 0.5f;

        if (waterFoam != null)
        {
            Color color = waterFoam.color;
            color.a = Mathf.Lerp(0.08f, 0.18f, wave);
            waterFoam.color = color;
            waterFoam.transform.localScale = foamBaseScale * Mathf.Lerp(0.995f, 1.02f, wave);
        }

        if (edgeHighlight != null)
        {
            Color color = edgeHighlight.color;
            color.a = Mathf.Lerp(0.12f, 0.22f, wave);
            edgeHighlight.color = color;
            edgeHighlight.transform.localScale = edgeBaseScale * Mathf.Lerp(0.998f, 1.012f, wave);
        }
    }
}
