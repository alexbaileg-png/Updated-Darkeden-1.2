using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillGridButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public SkillSelectorUI skillSelectorUI;
    public PlayerSkillManager skillManager;
    public PlayerStats playerStats;

    [Header("Skill")]
    public SkillType skillType;

    [Header("Icon")]
    public Image iconImage;
    public Sprite iconSprite;

    void Start()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.raycastTarget = true;
        }

        RefreshVisibility();
    }

    void OnEnable()
    {
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (skillManager == null)
            return;

        bool unlocked = skillManager.IsSkillUnlocked(skillType);
        gameObject.SetActive(unlocked);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skillSelectorUI != null)
            skillSelectorUI.SetHoveredSkill(skillType);

        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (skillSelectorUI != null)
            skillSelectorUI.ClearHoveredSkill();

        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (skillSelectorUI != null)
            skillSelectorUI.SelectSkill(skillType);
    }

    void ShowTooltip()
    {
        if (SkillTooltipUI.Instance == null || skillManager == null)
            return;

        SkillData data = skillManager.GetSkillData(skillType);

        SkillTooltipUI.Instance.ShowTooltip(data, skillManager, playerStats);
    }
}