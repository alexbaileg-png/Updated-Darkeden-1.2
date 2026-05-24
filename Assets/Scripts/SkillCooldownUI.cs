using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("Player Skills")]
    public PlayerProjectileAttack playerSkills;

    [Header("Cooldown Overlay")]
    public Image cooldownFill;

    void Update()
    {
        if (playerSkills == null || cooldownFill == null)
            return;

        float remaining = playerSkills.GetCooldownRemaining();
        float total = playerSkills.GetCurrentSkillCooldown();

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