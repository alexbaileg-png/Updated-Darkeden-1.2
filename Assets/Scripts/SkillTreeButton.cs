using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkillTreeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public PlayerSkillManager skillManager;
    public PlayerStats playerStats;
    public SkillTreeUI skillTreeUI;

    [Header("Skill")]
    public SkillType skillType;

    [Header("UI")]
    public Button button;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text requirementText;

    [Header("Visuals")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    public Color availableColor = Color.yellow;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(TryUnlockOrLevel);

        Refresh();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (skillManager == null || playerStats == null)
            return;

        // Visibility is controlled by SkillTreeUI tabs — don't override it here
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        bool classCanUseSkill = character != null && ClassSkillConfig.CanUseSkill(character, skillType);
        if (!classCanUseSkill) { gameObject.SetActive(false); return; }

        SkillData data = skillManager.GetSkillData(skillType);
        if (data == null) return;

        bool unlocked = skillManager.IsSkillUnlocked(skillType);
        int skillLevel = skillManager.GetSkillLevel(skillType);
        bool levelRequirementMet = playerStats.level >= data.requiredPlayerLevel;
        bool hasSkillPoints = skillManager.availableSkillPoints > 0;
        bool canUpgrade = hasSkillPoints && levelRequirementMet && skillLevel < data.maxSkillLevel;

        if (iconImage != null)
        {
            iconImage.sprite = data.skillIcon;
            iconImage.color = unlocked ? unlockedColor : lockedColor;

            if (!unlocked && levelRequirementMet && hasSkillPoints)
                iconImage.color = availableColor;
        }

        if (nameText != null)
            nameText.text = data.skillName;

        if (levelText != null)
            levelText.text = unlocked ? "Lv. " + skillLevel + " / " + data.maxSkillLevel : "Locked";

        if (requirementText != null)
            requirementText.text = "Requires Level " + data.requiredPlayerLevel;

        if (button != null)
            button.interactable = canUpgrade;
    }

    void TryUnlockOrLevel()
    {
        if (skillManager == null || playerStats == null)
            return;

        bool success = skillManager.UnlockOrLevelSkill(skillType, playerStats.level);

        if (success && skillTreeUI != null)
            skillTreeUI.RefreshAll();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SkillTooltipUI.Instance == null || skillManager == null)
            return;

        SkillData data = skillManager.GetSkillData(skillType);

        SkillTooltipUI.Instance.ShowTooltip(data, skillManager, playerStats);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.HideTooltip();
    }
}