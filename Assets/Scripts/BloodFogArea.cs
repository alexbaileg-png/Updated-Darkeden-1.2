using UnityEngine;

/// <summary>
/// Planted fog cloud. Uses distance checks instead of trigger colliders
/// so it works without Rigidbodies on enemies.
/// - Reduces enemy aggro range while inside
/// - Shows tunnel-vision overlay for Slayer players inside
/// - Vampires unaffected
/// </summary>
public class BloodFogArea : MonoBehaviour
{
    public float radius   = 8f;
    public float duration = 20f;
    [HideInInspector] public GameObject casterObject;

    [Range(0f, 1f)]
    public float enemyDetectionMultiplier = 0.15f;

    private float _elapsed = 0f;
    private GameObject _visionOverlay;

    // Track which enemies we already debuffed so we don't spam Apply
    private System.Collections.Generic.HashSet<EnemyAI> _debuffed
        = new System.Collections.Generic.HashSet<EnemyAI>();

    void Update()
    {
        _elapsed += Time.deltaTime;

        UpdateEnemyDebuffs();
        CheckLocalPlayerVision();

        if (_elapsed >= duration)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        foreach (EnemyAI enemy in _debuffed)
            if (enemy != null) enemy.RemoveFogDebuff();
        _debuffed.Clear();
        HideVisionOverlay();
    }

    void UpdateEnemyDebuffs()
    {
        // Only the server runs EnemyAI — but in host mode server == client so this is fine.
        // In a dedicated server build this runs only on the server instance.
        foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            if (dist <= radius && !_debuffed.Contains(enemy))
            {
                _debuffed.Add(enemy);
                enemy.ApplyFogDebuff(enemyDetectionMultiplier);
            }
            else if (dist > radius && _debuffed.Contains(enemy))
            {
                _debuffed.Remove(enemy);
                enemy.RemoveFogDebuff();
            }
        }
    }

    void CheckLocalPlayerVision()
    {
        PlayerStats localPlayer = PlayerStats.LocalInstance;
        if (localPlayer == null) return;

        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null && character.faction == PlayerFaction.Vampire)
        {
            HideVisionOverlay();
            return;
        }

        float dist = Vector3.Distance(transform.position, localPlayer.transform.position);
        if (dist <= radius) ShowVisionOverlay();
        else                HideVisionOverlay();
    }

    void ShowVisionOverlay()
    {
        if (_visionOverlay != null) return;
        BloodFogVisionOverlay overlay = FindObjectOfType<BloodFogVisionOverlay>(true);
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            _visionOverlay = overlay.gameObject;
        }
    }

    void HideVisionOverlay()
    {
        if (_visionOverlay == null) return;
        foreach (BloodFogArea fog in FindObjectsOfType<BloodFogArea>())
        {
            if (fog == this) continue;
            PlayerStats lp = PlayerStats.LocalInstance;
            if (lp != null && Vector3.Distance(fog.transform.position, lp.transform.position) <= fog.radius)
                return; // another fog still covers the player
        }
        _visionOverlay.SetActive(false);
        _visionOverlay = null;
    }
}
