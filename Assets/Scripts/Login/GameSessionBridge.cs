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

    [Header("Auto Save")]
    public float autoSaveInterval = 60f; // seconds between auto-saves
    private float _nextAutoSave;

    void Awake()
    {
        Instance = this;

        // Register faction BEFORE network starts (Awake runs before Start)
        CharacterData character = GameSession.Instance?.SelectedCharacter;
        if (character != null)
            FactionPlayerSpawner.SetPendingFaction(character.faction, character.GetClassName(), character.gender);
    }

    void Start()
    {
        if (saveExitMenuPanel != null)
            saveExitMenuPanel.SetActive(false);

        _nextAutoSave = Time.time + autoSaveInterval;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleSaveExitMenu();

        if (Time.time >= _nextAutoSave)
        {
            _nextAutoSave = Time.time + autoSaveInterval;
            AutoSave();
        }
    }

    async void AutoSave()
    {
        SaveGameToSession();
        CharacterPersistenceManager.Instance?.SaveCharacter();

        if (GameSession.Instance != null)
            await CloudDataService.SaveAccountAsync(GameSession.Instance.AccountData);

        Debug.Log("[AutoSave] Character progress saved.");
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
