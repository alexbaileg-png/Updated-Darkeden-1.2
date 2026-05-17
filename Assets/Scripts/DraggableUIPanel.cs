using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUIPanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || parentCanvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }
}