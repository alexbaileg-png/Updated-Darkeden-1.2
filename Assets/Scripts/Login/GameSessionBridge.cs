using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sits in the Game scene. Syncs GameSession <-> in-game player data.
/// Attach to any persistent GameObject in the game scene.
/// </summary>
public class GameSessionBridge : MonoBehaviour
{
    public static GameSessionBridge Instance { get; private set; }

    [Header("Save & Exit UI")]
    public GameObject saveExitMenuPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (saveExitMenuPanel != null)
            saveExitMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleSaveExitMenu();
    }

    // ── Menu ─────────────────────────────────────────────────────────────────

    public void ToggleSaveExitMenu()
    {
        if (saveExitMenuPanel != null)
            saveExitMenuPanel.SetActive(!saveExitMenuPanel.activeSelf);
    }

    public void OnResumeClicked()
    {
        if (saveExitMenuPanel != null)
            saveExitMenuPanel.SetActive(false);
    }

    public async void OnSaveAndExitClicked()
    {
        SaveGameToSession();

        CharacterPersistenceManager.Instance?.SaveCharacter();
        InventoryPersistenceManager.Instance?.SaveInventory();

        if (GameSession.Instance != null)
            await CloudDataService.SaveAccountAsync(GameSession.Instance.AccountData);

        SceneManager.LoadScene("Character Selection");
    }

    // ── Sync player state → GameSession ──────────────────────────────────────

    public void SaveGameToSession()
    {
        if (GameSession.Instance?.SelectedCharacter == null) return;

        CharacterData ch = GameSession.Instance.SelectedCharacter;
        PlayerStats ps = PlayerStats.LocalInstance;

        if (ps != null)
        {
            ch.level      = ps.level;
            ch.currentXP  = ps.currentXP;
            ch.lastPlayedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // Persist updated character back into the account slot
        CharacterData[] chars = GameSession.Instance.AccountData.characters;
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i]?.characterId == ch.characterId)
            {
                chars[i] = ch;
                break;
            }
        }
    }

    void OnApplicationQuit()
    {
        SaveGameToSession();
    }
}
