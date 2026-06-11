using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to any scene. Call ShowConfirmation() from a Button's OnClick.
/// Works in the Login screen, Character Select, and in-game.
/// </summary>
public class ExitGameButton : MonoBehaviour
{
    [Header("Confirmation Panel")]
    [Tooltip("The confirmation dialog panel — starts hidden")]
    public GameObject confirmationPanel;
    public Button     yesButton;
    public Button     noButton;
    public TMP_Text   messageText;

    [Header("Settings")]
    public string confirmMessage = "Are you sure you want to exit?";

    void Awake()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesClicked);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoClicked);

        if (messageText != null)
            messageText.text = confirmMessage;
    }

    /// <summary>Call this from your Exit Button's OnClick event.</summary>
    public void ShowConfirmation()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    void OnYesClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnNoClicked()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
