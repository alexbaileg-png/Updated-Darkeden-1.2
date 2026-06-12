using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public AccountData AccountData { get; private set; }
    public CharacterData SelectedCharacter { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AccountData = new AccountData();
    }

    public void SetAccountData(AccountData data) => AccountData = data ?? new AccountData();

    public void SelectCharacter(CharacterData character) => SelectedCharacter = character;

    public void SaveCharacterToSlot(CharacterData character, int slot)
    {
        if (slot < 0 || slot >= 5) return;
        AccountData.characters[slot] = character;
    }

    public int GetEmptySlot()
    {
        for (int i = 0; i < AccountData.characters.Length; i++)
        {
            var c = AccountData.characters[i];
            if (c == null || string.IsNullOrEmpty(c.characterName)) return i;
        }
        return -1;
    }
}
