using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    [Header("References — set in Inspector")]
    public PlayerSkillManager skillManager;
    public PlayerStats playerStats;

    [Header("UI")]
    public TMP_Text skillPointsText;

    [Header("Dynamic Generation")]
    public GameObject skillButtonPrefab;
    public Transform buttonContainer;

    [Header("Category Tabs")]
    public Button tabMagicAttack;
    public Button tabMelee;
    public Button tabBuff;
    public Button tabDebuff;
    public Button tabControl;

    [Header("Tab Colors")]
    public Color tabActiveColor   = Color.white;
    public Color tabInactiveColor = new Color(0.5f, 0.5f, 0.5f);

    private readonly List<SkillTreeButton> _generatedButtons = new List<SkillTreeButton>();
    private string _lastBuiltClass = "";
    private SkillCategory _activeCategory = SkillCategory.Melee;

    void OnEnable()
    {
        SetupTabListeners();
        BuildSkillButtons();
        SelectTab(_activeCategory);
    }

    void Update()
    {
        string currentClass = GameSession.Instance?.SelectedCharacter?.GetClassName() ?? "";
        if (currentClass != _lastBuiltClass)
        {
            BuildSkillButtons();
            SelectTab(_activeCategory);
        }

        RefreshAll();
    }

    void SetupTabListeners()
    {
        if (tabMagicAttack != null) { tabMagicAttack.onClick.RemoveAllListeners(); tabMagicAttack.onClick.AddListener(() => SelectTab(SkillCategory.MagicAttack)); }
        if (tabMelee       != null) { tabMelee.onClick.RemoveAllListeners();       tabMelee.onClick.AddListener(()       => SelectTab(SkillCategory.Melee));       }
        if (tabBuff        != null) { tabBuff.onClick.RemoveAllListeners();        tabBuff.onClick.AddListener(()        => SelectTab(SkillCategory.Buff));        }
        if (tabDebuff      != null) { tabDebuff.onClick.RemoveAllListeners();      tabDebuff.onClick.AddListener(()      => SelectTab(SkillCategory.Debuff));      }
        if (tabControl     != null) { tabControl.onClick.RemoveAllListeners();     tabControl.onClick.AddListener(()     => SelectTab(SkillCategory.Control));     }
    }

    void SelectTab(SkillCategory category)
    {
        _activeCategory = category;

        // Highlight active tab
        SetTabColor(tabMagicAttack, category == SkillCategory.MagicAttack);
        SetTabColor(tabMelee,       category == SkillCategory.Melee);
        SetTabColor(tabBuff,        category == SkillCategory.Buff);
        SetTabColor(tabDebuff,      category == SkillCategory.Debuff);
        SetTabColor(tabControl,     category == SkillCategory.Control);

        // Show only buttons matching this category
        foreach (SkillTreeButton btn in _generatedButtons)
        {
            if (btn == null) continue;
            SkillData data = skillManager?.GetSkillData(btn.skillType);
            bool show = data != null && data.skillCategory == category;
            btn.gameObject.SetActive(show);
        }
    }

    void SetTabColor(Button tab, bool active)
    {
        if (tab == null) return;
        Image img = tab.GetComponent<Image>();
        if (img != null) img.color = active ? tabActiveColor : tabInactiveColor;
    }

    public void BuildSkillButtons()
    {
        if (skillButtonPrefab == null || buttonContainer == null) return;

        foreach (SkillTreeButton btn in _generatedButtons)
            if (btn != null) Destroy(btn.gameObject);
        _generatedButtons.Clear();

        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character == null) return;

        _lastBuiltClass = character.GetClassName();
        List<SkillType> classSkills = ClassSkillConfig.GetSkillsForCharacter(character);

        foreach (SkillType skillType in classSkills)
        {
            GameObject go = Instantiate(skillButtonPrefab, buttonContainer);
            SkillTreeButton btn = go.GetComponent<SkillTreeButton>();
            if (btn == null) continue;

            btn.skillType    = skillType;
            btn.skillManager = skillManager;
            btn.playerStats  = playerStats;
            btn.skillTreeUI  = this;

            _generatedButtons.Add(btn);
        }
    }

    public void RefreshAll()
    {
        if (skillManager != null && skillPointsText != null)
            skillPointsText.text = "Skill Points: " + skillManager.availableSkillPoints;

        foreach (SkillTreeButton btn in _generatedButtons)
            if (btn != null) btn.Refresh();

        // Reapply tab filter after refresh so buttons don't all turn visible again
        SelectTab(_activeCategory);
    }

    public void WirePlayer(PlayerSkillManager psm, PlayerStats ps)
    {
        skillManager = psm;
        playerStats  = ps;
        BuildSkillButtons();
        SelectTab(_activeCategory);
        RefreshAll();
    }
}
