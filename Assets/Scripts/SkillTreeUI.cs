using UnityEngine;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerSkillManager skillManager;

    [Header("UI")]
    public TMP_Text skillPointsText;
    public SkillTreeButton[] skillButtons;

    void OnEnable()
    {
        RefreshAll();
    }

    void Update()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (skillManager != null && skillPointsText != null)
        {
            skillPointsText.text = "Skill Points: " + skillManager.availableSkillPoints;
        }

        if (skillButtons == null)
            return;

        foreach (SkillTreeButton button in skillButtons)
        {
            if (button != null)
                button.Refresh();
        }
    }
}