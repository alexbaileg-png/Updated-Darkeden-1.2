using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreationManager : MonoBehaviour
{
    [Header("Step Panels")]
    public GameObject factionPanel;
    public GameObject classPanel;
    public GameObject genderPanel;   // NEW — step 2
    public GameObject namePanel;

    [Header("Faction Buttons")]
    public Button vampireButton;
    public Button slayerButton;

    [Header("Vampire Bloodline Buttons")]
    public Button bloodKnightButton;
    public Button shadowButton;
    public Button sorcererButton;
    public Button dominatorButton;

    [Header("Slayer Class Buttons")]
    public Button swordmasterButton;
    public Button soldierButton;
    public Button enchanterButton;
    public Button healerButton;

    [Header("Class Description")]
    public TMP_Text classDescriptionText;

    [Header("Gender Buttons")]
    public Button maleButton;
    public Button femaleButton;

    [Header("Name Step")]
    public TMP_InputField nameInputField;
    public TMP_Text namePreviewText;
    public TMP_Text nameErrorText;
    public Button confirmCreateButton;

    [Header("Navigation")]
    public Button backButton;

    private PlayerFaction    _selectedFaction;
    private PlayerGender     _selectedGender = PlayerGender.Male;
    private VampireBloodline _selectedBloodline;
    private SlayerClass      _selectedSlayerClass;
    private bool _classSelected = false;
    private int  _step = 0; // 0=faction, 1=class, 2=gender, 3=name

    void Start()
    {
        if (GameSession.Instance == null) { SceneManager.LoadScene("Login"); return; }

        if (nameInputField != null)
            nameInputField.onValueChanged.AddListener(OnNameChanged);

        ShowStep(0);
    }

    // ── Faction ──────────────────────────────────────────────────────────────

    public void OnVampireSelected()
    {
        _selectedFaction = PlayerFaction.Vampire;
        _classSelected = false;
        ShowStep(1);
        ShowVampireClasses();
    }

    public void OnSlayerSelected()
    {
        _selectedFaction = PlayerFaction.Slayer;
        _classSelected = false;
        ShowStep(1);
        ShowSlayerClasses();
    }

    void ShowVampireClasses()
    {
        SetActive(bloodKnightButton, true);
        SetActive(shadowButton,      true);
        SetActive(sorcererButton,    true);
        SetActive(dominatorButton,   true);
        SetActive(swordmasterButton, false);
        SetActive(soldierButton,     false);
        SetActive(enchanterButton,   false);
        SetActive(healerButton,      false);
    }

    void ShowSlayerClasses()
    {
        SetActive(bloodKnightButton, false);
        SetActive(shadowButton,      false);
        SetActive(sorcererButton,    false);
        SetActive(dominatorButton,   false);
        SetActive(swordmasterButton, true);
        SetActive(soldierButton,     true);
        SetActive(enchanterButton,   true);
        SetActive(healerButton,      true);
    }

    // ── Class / Bloodline ─────────────────────────────────────────────────────

    public void OnBloodKnightSelected()  => SelectVampire(VampireBloodline.BloodKnight,
        "Blood Knight — A melee tank who drains life with every strike. High armor, relentless in close combat.");
    public void OnShadowSelected()       => SelectVampire(VampireBloodline.Shadow,
        "Shadow — An assassin of the night. Strikes from darkness with burst damage and vanishes before retaliation.");
    public void OnSorcererSelected()     => SelectVampire(VampireBloodline.Sorcerer,
        "Sorcerer — Master of dark magic. Unleashes powerful spells that devastate enemies from range.");
    public void OnDominatorSelected()    => SelectVampire(VampireBloodline.Dominator,
        "Dominator — Bends the will of enemies. Controls the battlefield with debuffs and crowd control.");

    public void OnSwordmasterSelected()  => SelectSlayer(SlayerClass.Swordmaster,
        "Swordmaster — A warrior trained to hunt vampires in close combat. High damage, fast strikes.");
    public void OnSoldierSelected()      => SelectSlayer(SlayerClass.Soldier,
        "Soldier — Armed with firearms and explosives. Keeps vampires at range with sustained ranged damage.");
    public void OnEnchanterSelected()    => SelectSlayer(SlayerClass.Enchanter,
        "Enchanter — Empowers allies and weakens vampires with holy magic. Essential in any group.");
    public void OnHealerSelected()       => SelectSlayer(SlayerClass.Healer,
        "Healer — Keeps the party alive with powerful restoration. Can even bring the fallen back.");

    void SelectVampire(VampireBloodline bloodline, string description)
    {
        _selectedBloodline = bloodline;
        _classSelected = true;
        if (classDescriptionText != null) classDescriptionText.text = description;
        ShowStep(2);
    }

    void SelectSlayer(SlayerClass slayerClass, string description)
    {
        _selectedSlayerClass = slayerClass;
        _classSelected = true;
        if (classDescriptionText != null) classDescriptionText.text = description;
        ShowStep(2);
    }

    // ── Gender ────────────────────────────────────────────────────────────────

    public void OnMaleSelected()
    {
        _selectedGender = PlayerGender.Male;
        HighlightGenderButton(PlayerGender.Male);
        ShowStep(3);
    }

    public void OnFemaleSelected()
    {
        _selectedGender = PlayerGender.Female;
        HighlightGenderButton(PlayerGender.Female);
        ShowStep(3);
    }

    void HighlightGenderButton(PlayerGender gender)
    {
        // Optional visual feedback — tint selected button
        if (maleButton != null)
            maleButton.image.color   = gender == PlayerGender.Male   ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        if (femaleButton != null)
            femaleButton.image.color = gender == PlayerGender.Female ? Color.white : new Color(0.6f, 0.6f, 0.6f);
    }

    // ── Name ──────────────────────────────────────────────────────────────────

    void OnNameChanged(string value)
    {
        if (namePreviewText != null)    namePreviewText.text = value;
        if (nameErrorText != null)      nameErrorText.text   = "";
        if (confirmCreateButton != null) confirmCreateButton.interactable = IsValidName(value);
    }

    bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 3 && name.Trim().Length <= 20;
    }

    public async void OnConfirmCreateClicked()
    {
        string characterName = nameInputField != null ? nameInputField.text.Trim() : "";

        if (!IsValidName(characterName))
        {
            if (nameErrorText != null) nameErrorText.text = "Name must be 3–20 characters.";
            return;
        }

        int slot = GameSession.Instance.GetEmptySlot();
        if (slot < 0)
        {
            if (nameErrorText != null) nameErrorText.text = "No empty character slots.";
            return;
        }

        CharacterData newCharacter = new CharacterData
        {
            characterId      = Guid.NewGuid().ToString(),
            characterName    = characterName,
            faction          = _selectedFaction,
            gender           = _selectedGender,
            vampireBloodline = _selectedBloodline,
            slayerClass      = _selectedSlayerClass,
            level            = 1,
            currentXP        = 0,
            lastPlayedUtc    = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        GameSession.Instance.SaveCharacterToSlot(newCharacter, slot);
        await CloudDataService.SaveAccountAsync(GameSession.Instance.AccountData);

        SceneManager.LoadScene("Character Selection");
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    public void OnBackClicked()
    {
        if (_step > 0)
            ShowStep(_step - 1);
        else
            SceneManager.LoadScene("Character Selection");
    }

    void ShowStep(int step)
    {
        _step = step;
        if (factionPanel != null) factionPanel.SetActive(step == 0);
        if (classPanel   != null) classPanel.SetActive(step == 1);
        if (genderPanel  != null) genderPanel.SetActive(step == 2);
        if (namePanel    != null) namePanel.SetActive(step == 3);
        if (backButton   != null) backButton.gameObject.SetActive(step > 0);
        if (confirmCreateButton != null) confirmCreateButton.interactable = false;
    }

    void SetActive(Button btn, bool active) { if (btn != null) btn.gameObject.SetActive(active); }
}
