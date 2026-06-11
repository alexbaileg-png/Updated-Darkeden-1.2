using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("Cooldown Overlay")]
    public Image cooldownFill;

    private ISkillCaster _caster;

    public void SetSkillCaster(ISkillCaster caster) => _caster = caster;

    void Update()
    {
        if (_caster == null || cooldownFill == null) return;

        float remaining = _caster.GetCooldownRemaining();
        float total     = _caster.GetCurrentSkillCooldown();

        if (remaining <= 0f || total <= 0f)
        {
            cooldownFill.fillAmount = 0f;
            cooldownFill.gameObject.SetActive(false);
            return;
        }

        cooldownFill.gameObject.SetActive(true);
        cooldownFill.fillAmount = remaining / total;
    }
}
