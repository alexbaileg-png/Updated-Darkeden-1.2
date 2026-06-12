using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlotUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject occupiedPanel;
    public GameObject emptyPanel;
    public TMP_Text nameText;
    public TMP_Text classText;
    public TMP_Text levelText;
    public Button slotButton;

    private int _slotIndex;
    private Action<int> _onClicked;

    public void Setup(CharacterData character, int slotIndex, Action<int> onClicked)
    {
        _slotIndex = slotIndex;
        _onClicked = onClicked;

        bool hasCharacter = character != null && !string.IsNullOrEmpty(character.characterName);

        if (occupiedPanel != null) occupiedPanel.SetActive(hasCharacter);
        if (emptyPanel != null) emptyPanel.SetActive(!hasCharacter);

        if (hasCharacter)
        {
            if (nameText != null) nameText.text = character.characterName;
            if (classText != null) classText.text = $"{character.GetFactionDisplay()} · {character.GetClassName()}";
            if (levelText != null) levelText.text = $"Lv. {character.level}";
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => _onClicked?.Invoke(_slotIndex));
        }
    }
}
