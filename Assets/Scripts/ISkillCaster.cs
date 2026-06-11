/// <summary>
/// Common interface for any skill caster component (Healer, Dominator, etc.)
/// UI components reference this instead of a specific class.
/// </summary>
public interface ISkillCaster
{
    SkillType CurrentSelectedSkill { get; }
    void SetSelectedSkill(SkillType skill);
    void BindSkill(UnityEngine.KeyCode key, SkillType skill);
    float GetCooldownRemaining();
    float GetCurrentSkillCooldown();
}
