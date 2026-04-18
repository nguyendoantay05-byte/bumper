using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bắt click UI thủ công để đảm bảo Button vẫn hoạt động
/// ngay cả khi EventSystem/InputModule của scene có vấn đề.
/// </summary>
[RequireComponent(typeof(GraphicRaycaster))]
public class UIClickRouter : MonoBehaviour
{
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private PointerEventData pointerEventData;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        raycaster = GetComponent<GraphicRaycaster>();
        eventSystem = FindAnyObjectByType<EventSystem>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (raycaster == null)
        {
            raycaster = GetComponent<GraphicRaycaster>();
        }

        if (eventSystem == null)
        {
            eventSystem = FindAnyObjectByType<EventSystem>();
        }

        if (raycaster == null || eventSystem == null)
        {
            return;
        }

        if (pointerEventData == null)
        {
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.position = Input.mousePosition;
        raycastResults.Clear();
        raycaster.Raycast(pointerEventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            Button button = raycastResults[i].gameObject.GetComponentInParent<Button>();
            if (button == null || !button.interactable || !button.isActiveAndEnabled)
            {
                continue;
            }

            button.onClick.Invoke();
            return;
        }
    }
}
