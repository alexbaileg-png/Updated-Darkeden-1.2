using UnityEngine;
using UnityEngine.UI;

public class MenuTabController : MonoBehaviour
{
    public static bool IsMenuOpen;

    [Header("Menu Root")]
    public GameObject menuRoot;

    [Header("Top Menu Buttons")]
    public GameObject topbar;

    [Header("Panels")]
    public GameObject inventoryPanel;
    public GameObject statsPanel;
    public GameObject skillsPanel;
    public GameObject optionsPanel;

    [Header("Buttons")]
    public Button inventoryButton;
    public Button statsButton;
    public Button skillsButton;
    public Button optionsButton;

    void Start()
    {
        if (inventoryButton != null) inventoryButton.onClick.AddListener(ToggleInventory);
        if (statsButton != null) statsButton.onClick.AddListener(ToggleStats);
        if (skillsButton != null) skillsButton.onClick.AddListener(ToggleSkills);
        if (optionsButton != null) optionsButton.onClick.AddListener(ToggleOptions);

        CloseEverything();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();

        if (Input.GetKeyDown(KeyCode.P))
            ToggleStats();

        if (Input.GetKeyDown(KeyCode.K))
            ToggleSkills();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsAnythingOpen())
                CloseEverything();
            else
                OpenMainMenu();
        }

        IsMenuOpen = IsAnythingOpen();
    }

    public void OpenMainMenu()
    {
        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (topbar != null)
            topbar.SetActive(true);

        IsMenuOpen = true;
    }

    public void ToggleInventory()
    {
        TogglePanel(inventoryPanel);
    }

    public void ToggleStats()
    {
        TogglePanel(statsPanel);
    }

    public void ToggleSkills()
    {
        TogglePanel(skillsPanel);
    }

    public void ToggleOptions()
    {
        TogglePanel(optionsPanel);
    }

    void TogglePanel(GameObject panel)
    {
        if (menuRoot == null || panel == null)
            return;

        menuRoot.SetActive(true);

        if (topbar != null)
            topbar.SetActive(false);

        panel.SetActive(!panel.activeSelf);

        IsMenuOpen = IsAnythingOpen();

        if (!IsMenuOpen)
            menuRoot.SetActive(false);
    }

    public void CloseEverything()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (skillsPanel != null) skillsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (topbar != null)
            topbar.SetActive(false);

        if (menuRoot != null)
            menuRoot.SetActive(false);

        IsMenuOpen = false;
    }

    bool IsAnythingOpen()
    {
        return
            (menuRoot != null && menuRoot.activeSelf) &&
            (
                (topbar != null && topbar.activeSelf) ||
                (inventoryPanel != null && inventoryPanel.activeSelf) ||
                (statsPanel != null && statsPanel.activeSelf) ||
                (skillsPanel != null && skillsPanel.activeSelf) ||
                (optionsPanel != null && optionsPanel.activeSelf)
            );
    }
}