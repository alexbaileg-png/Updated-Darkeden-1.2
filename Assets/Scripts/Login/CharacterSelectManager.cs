using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Slot UI — assign 5 in order")]
    public CharacterSlotUI[] slots;

    [Header("Buttons")]
    public Button enterWorldButton;
    public Button createCharacterButton;
    public Button deleteCharacterButton;

    [Header("Selected Info Panel")]
    public TMP_Text selectedNameText;
    public TMP_Text selectedClassText;
    public TMP_Text selectedLevelText;

    private int _selectedSlot = -1;

    void Start()
    {
        if (GameSession.Instance == null)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        RefreshSlots();
        SetSelected(-1);
    }

    void RefreshSlots()
    {
        AccountData account = GameSession.Instance.AccountData;

        for (int i = 0; i < slots.Length; i++)
        {
            CharacterData character = i < account.characters.Length ? account.characters[i] : null;
            slots[i].Setup(character, i, OnSlotClicked);
        }
    }

    void OnSlotClicked(int slotIndex)
    {
        SetSelected(slotIndex);
    }

    void SetSelected(int slotIndex)
    {
        _selectedSlot = slotIndex;

        CharacterData character = GetCharacterAt(slotIndex);
        bool hasCharacter = character != null;

        if (enterWorldButton != null) enterWorldButton.interactable = hasCharacter;
        if (deleteCharacterButton != null) deleteCharacterButton.interactable = hasCharacter;

        if (selectedNameText != null)
            selectedNameText.text = hasCharacter ? character.characterName : "";
        if (selectedClassText != null)
            selectedClassText.text = hasCharacter ? $"{character.GetFactionDisplay()} — {character.GetClassName()}" : "";
        if (selectedLevelText != null)
            selectedLevelText.text = hasCharacter ? $"Level {character.level}" : "";
    }

    public void OnEnterWorldClicked()
    {
        CharacterData character = GetCharacterAt(_selectedSlot);
        if (character == null) return;

        GameSession.Instance.SelectCharacter(character);
        SceneManager.LoadScene("Game");
    }

    public void OnCreateCharacterClicked()
    {
        if (GameSession.Instance.GetEmptySlot() < 0)
        {
            Debug.Log("All character slots are full.");
            return;
        }

        SceneManager.LoadScene("CharacterCreate");
    }

    public async void OnDeleteCharacterClicked()
    {
        if (_selectedSlot < 0) return;

        CharacterData character = GetCharacterAt(_selectedSlot);
        if (character == null) return;

        GameSession.Instance.AccountData.characters[_selectedSlot] = null;
        await CloudDataService.SaveAccountAsync(GameSession.Instance.AccountData);

        RefreshSlots();
        SetSelected(-1);
    }

    CharacterData GetCharacterAt(int slot)
    {
        if (slot < 0 || GameSession.Instance == null) return null;
        CharacterData[] chars = GameSession.Instance.AccountData.characters;
        if (slot >= chars.Length) return null;
        return chars[slot];
    }
}
