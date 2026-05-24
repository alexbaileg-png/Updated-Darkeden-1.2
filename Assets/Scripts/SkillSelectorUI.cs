using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillSelectorUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Player Skills")]
    public PlayerProjectileAttack playerSkills;

    [Header("Main Selector")]
    public Image selectedSkillIcon;

    [Header("Skill Grid")]
    public GameObject skillGridPanel;

    [Header("Skill Icons")]
    public Sprite holyBoltIcon;
    public Sprite holyRainIcon;
    public Sprite holyCircleHealIcon;
    public Sprite healingOrbitIcon;

    private SkillType hoveredSkill;
    private bool hasHoveredSkill = false;

    void Start()
    {
        if (skillGridPanel != null)
            skillGridPanel.SetActive(false);

        UpdateSelectedIcon();
    }

    void Update()
    {
        HandleKeyBinding();
        UpdateSelectedIcon();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleSkillGrid();
    }

    public void ToggleSkillGrid()
    {
        if (skillGridPanel == null)
            return;

        bool newState = !skillGridPanel.activeSelf;
        skillGridPanel.SetActive(newState);

        if (newState)
            RefreshGridButtons();
        else
            ClearHoveredSkill();
    }

    void RefreshGridButtons()
    {
        SkillGridButton[] buttons = skillGridPanel.GetComponentsInChildren<SkillGridButton>(true);

        foreach (SkillGridButton button in buttons)
            button.RefreshVisibility();
    }

    public void SetHoveredSkill(SkillType skill)
    {
        hoveredSkill = skill;
        hasHoveredSkill = true;
        Debug.Log("Hovering skill: " + skill);
    }

    public void ClearHoveredSkill()
    {
        hasHoveredSkill = false;
    }

    public void SelectSkill(SkillType skill)
    {
        if (playerSkills == null)
            return;

        playerSkills.SetSelectedSkill(skill);

        if (skillGridPanel != null)
            skillGridPanel.SetActive(false);

        ClearHoveredSkill();
        UpdateSelectedIcon();
    }

    void HandleKeyBinding()
    {
        if (!hasHoveredSkill || playerSkills == null)
            return;

        if (Input.GetKeyDown(KeyCode.F8))
            BindHoveredSkill(KeyCode.F8);

        if (Input.GetKeyDown(KeyCode.F9))
            BindHoveredSkill(KeyCode.F9);

        if (Input.GetKeyDown(KeyCode.F10))
            BindHoveredSkill(KeyCode.F10);

        if (Input.GetKeyDown(KeyCode.F11))
            BindHoveredSkill(KeyCode.F11);
    }

    void BindHoveredSkill(KeyCode key)
    {
        playerSkills.BindSkill(key, hoveredSkill);
        playerSkills.SetSelectedSkill(hoveredSkill);
        UpdateSelectedIcon();

        Debug.Log("Assigned " + hoveredSkill + " to " + key);
    }

    void UpdateSelectedIcon()
    {
        if (playerSkills == null || selectedSkillIcon == null)
            return;

        selectedSkillIcon.sprite = GetIcon(playerSkills.currentSelectedSkill);
    }

    Sprite GetIcon(SkillType skill)
    {
        if (skill == SkillType.HolyBolt)
            return holyBoltIcon;

        if (skill == SkillType.HolyRain)
            return holyRainIcon;

        if (skill == SkillType.HolyCircleHeal)
            return holyCircleHealIcon;

        if (skill == SkillType.HealingOrbit)
            return healingOrbitIcon;

        return null;
    }
}