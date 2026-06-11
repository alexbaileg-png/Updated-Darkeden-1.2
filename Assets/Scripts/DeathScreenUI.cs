using UnityEngine;

public class DeathScreenUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Wire this to the respawn button's OnClick in the Inspector
    public void OnRespawnButtonClicked()
    {
        PlayerStats stats = PlayerStats.LocalInstance;
        if (stats != null)
            stats.ServerRequestRespawn();
    }
}
