using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillDisplayUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public PlayerProjectileAttack playerSkills;

    [Header("Skill Images")]
    public Image skillImage;

    public Sprite holyBoltSprite;
    public Sprite holyRainSprite;

    private RectTransform rectTransform;
    private Canvas parentCanvas;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        UpdateSkillDisplay();
    }

    void Update()
    {
        UpdateSkillDisplay();
    }

    void UpdateSkillDisplay()
    {
        if (playerSkills == null || skillImage == null)
            return;

        if (playerSkills.selectedSkill == PlayerProjectileAttack.SelectedSkill.HolyBolt)
        {
            skillImage.sprite = holyBoltSprite;
        }
        else if (playerSkills.selectedSkill == PlayerProjectileAttack.SelectedSkill.HolyRain)
        {
            skillImage.sprite = holyRainSprite;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || parentCanvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }
}