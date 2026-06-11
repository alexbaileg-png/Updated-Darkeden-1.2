using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillGridButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References — set by SkillSelectorUI at runtime")]
    public SkillSelectorUI skillSelectorUI;
    public PlayerSkillManager skillManager;
    public PlayerStats playerStats;

    [Header("Skill — set by SkillSelectorUI at runtime")]
    public SkillType skillType;

    [Header("Icon")]
    public Image iconImage;

    void Start()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        ApplyIcon();
        RefreshVisibility();
    }

    void OnEnable() => RefreshVisibility();

    void ApplyIcon()
    {
        if (iconImage == null || skillManager == null) return;
        SkillData data = skillManager.GetSkillData(skillType);
        if (data?.skillIcon != null)
            iconImage.sprite = data.skillIcon;
    }

    public void RefreshVisibility()
    {
        if (skillManager == null) return;
        gameObject.SetActive(skillManager.IsSkillUnlocked(skillType));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        skillSelectorUI?.SetHoveredSkill(skillType);
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        skillSelectorUI?.ClearHoveredSkill();
        SkillTooltipUI.Instance?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        skillSelectorUI?.SelectSkill(skillType);
    }

    void ShowTooltip()
    {
        if (SkillTooltipUI.Instance == null || skillManager == null) return;
        SkillData data = skillManager.GetSkillData(skillType);
        SkillTooltipUI.Instance.ShowTooltip(data, skillManager, playerStats);
    }
}
