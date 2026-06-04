using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class StatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string tooltipText;

    public GameObject tooltipRoot;
    public TMP_Text tooltipBodyText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(true);

        if (tooltipBodyText != null)
            tooltipBodyText.text = tooltipText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }
}