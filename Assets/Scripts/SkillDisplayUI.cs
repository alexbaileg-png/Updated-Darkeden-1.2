using UnityEngine;
using UnityEngine.UI;

public class SkillDisplayUI : MonoBehaviour
{
    [Header("Skill Display")]
    public Image skillIcon;

    private ISkillCaster _caster;
    private PlayerSkillManager _skillManager;

    public void SetSkillCaster(ISkillCaster caster)
    {
        _caster = caster;
        // Grab the skill manager from the same GameObject
        if (caster is UnityEngine.MonoBehaviour mb)
            _skillManager = mb.GetComponent<PlayerSkillManager>();
    }

    void Update()
    {
        if (_caster == null || skillIcon == null) return;

        Sprite icon = GetIconForSkill(_caster.CurrentSelectedSkill);
        skillIcon.sprite = icon;
        skillIcon.enabled = icon != null;
    }

    Sprite GetIconForSkill(SkillType skill)
    {
        if (_skillManager == null) return null;
        SkillData data = _skillManager.GetSkillData(skill);
        return data?.skillIcon;
    }
}
