using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillSelectorUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Main Selector")]
    public Image selectedSkillIcon;

    [Header("Skill Grid")]
    public GameObject skillGridPanel;
    [Tooltip("Prefab with SkillGridButton component — one spawned per unlocked skill.")]
    public GameObject skillGridButtonPrefab;
    [Tooltip("Parent with GridLayoutGroup inside skillGridPanel.")]
    public Transform gridContainer;

    private ISkillCaster _caster;
    private PlayerSkillManager _skillManager;
    private PlayerStats _playerStats;

    private SkillType _hoveredSkill;
    private bool _hasHoveredSkill = false;

    private readonly List<SkillGridButton> _generatedGridButtons = new List<SkillGridButton>();

    public void SetSkillCaster(ISkillCaster caster)
    {
        _caster = caster;
        if (caster is MonoBehaviour mb)
        {
            _skillManager = mb.GetComponent<PlayerSkillManager>();
            _playerStats  = mb.GetComponent<PlayerStats>();
        }

        BuildGridButtons();
        UpdateSelectedIcon();
    }

    void Start()
    {
        if (skillGridPanel != null)
            skillGridPanel.SetActive(false);
    }

    void Update()
    {
        HandleKeyBinding();
        UpdateSelectedIcon();

        // Refresh grid button visibility when skills get unlocked
        foreach (SkillGridButton btn in _generatedGridButtons)
            btn?.RefreshVisibility();
    }

    public void OnPointerClick(PointerEventData eventData) => ToggleSkillGrid();

    public void ToggleSkillGrid()
    {
        if (skillGridPanel == null) return;
        skillGridPanel.SetActive(!skillGridPanel.activeSelf);
    }

    // ── Grid generation ───────────────────────────────────────────────────────

    void BuildGridButtons()
    {
        if (skillGridButtonPrefab == null || gridContainer == null || _skillManager == null) return;

        // Clear old
        foreach (SkillGridButton btn in _generatedGridButtons)
            if (btn != null) Destroy(btn.gameObject);
        _generatedGridButtons.Clear();

        // Spawn one button per skill the character's class can use
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        List<SkillType> classSkills = ClassSkillConfig.GetSkillsForCharacter(character);

        foreach (SkillType skill in classSkills)
        {
            GameObject go  = Instantiate(skillGridButtonPrefab, gridContainer);
            SkillGridButton btn = go.GetComponent<SkillGridButton>();
            if (btn == null) continue;

            btn.skillType       = skill;
            btn.skillManager    = _skillManager;
            btn.playerStats     = _playerStats;
            btn.skillSelectorUI = this;

            _generatedGridButtons.Add(btn);
        }
    }

    // ── Skill selection ───────────────────────────────────────────────────────

    public void SetHoveredSkill(SkillType skill)
    {
        _hoveredSkill    = skill;
        _hasHoveredSkill = true;
    }

    public void ClearHoveredSkill() => _hasHoveredSkill = false;

    public void SelectSkill(SkillType skill)
    {
        if (_caster == null) return;
        _caster.SetSelectedSkill(skill);
        if (skillGridPanel != null) skillGridPanel.SetActive(false);
        ClearHoveredSkill();
        UpdateSelectedIcon();
    }

    void HandleKeyBinding()
    {
        if (!_hasHoveredSkill || _caster == null) return;
        if (Input.GetKeyDown(KeyCode.F8))  BindHoveredSkill(KeyCode.F8);
        if (Input.GetKeyDown(KeyCode.F9))  BindHoveredSkill(KeyCode.F9);
        if (Input.GetKeyDown(KeyCode.F10)) BindHoveredSkill(KeyCode.F10);
        if (Input.GetKeyDown(KeyCode.F11)) BindHoveredSkill(KeyCode.F11);
    }

    void BindHoveredSkill(KeyCode key)
    {
        _caster.BindSkill(key, _hoveredSkill);
        _caster.SetSelectedSkill(_hoveredSkill);
        UpdateSelectedIcon();
    }

    void UpdateSelectedIcon()
    {
        if (_caster == null || selectedSkillIcon == null) return;
        SkillData data = _skillManager?.GetSkillData(_caster.CurrentSelectedSkill);
        selectedSkillIcon.sprite  = data?.skillIcon;
        selectedSkillIcon.enabled = data?.skillIcon != null;
    }
}
