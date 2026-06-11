using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Dynamically builds the skill tree at runtime based on the character's class.
/// Drop a SkillTreeButton prefab into skillButtonPrefab and point buttonContainer
/// at a child with a GridLayoutGroup — buttons are generated automatically.
/// No manually placed buttons needed.
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    [Header("References — set in Inspector")]
    public PlayerSkillManager skillManager;
    public PlayerStats playerStats;

    [Header("UI")]
    public TMP_Text skillPointsText;

    [Header("Dynamic Generation")]
    [Tooltip("Prefab with SkillTreeButton component. One will be spawned per skill.")]
    public GameObject skillButtonPrefab;
    [Tooltip("Parent transform with a GridLayoutGroup. Buttons are spawned here.")]
    public Transform buttonContainer;

    // Live list of generated buttons
    private readonly List<SkillTreeButton> _generatedButtons = new List<SkillTreeButton>();
    private string _lastBuiltClass = "";

    void OnEnable()
    {
        BuildSkillButtons();
        RefreshAll();
    }

    void Update()
    {
        // Rebuild if character class changed (e.g. after character swap)
        string currentClass = GameSession.Instance?.SelectedCharacter?.GetClassName() ?? "";
        if (currentClass != _lastBuiltClass)
            BuildSkillButtons();

        RefreshAll();
    }

    /// <summary>
    /// Destroys existing buttons and spawns fresh ones for the current character's class.
    /// </summary>
    public void BuildSkillButtons()
    {
        if (skillButtonPrefab == null || buttonContainer == null) return;

        // Clear old buttons
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

            btn.skillType   = skillType;
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
    }

    /// <summary>
    /// Called after the network player spawns so the skill tree knows which
    /// PlayerSkillManager and PlayerStats to talk to.
    /// </summary>
    public void WirePlayer(PlayerSkillManager psm, PlayerStats ps)
    {
        skillManager = psm;
        playerStats  = ps;
        BuildSkillButtons();
        RefreshAll();
    }
}
