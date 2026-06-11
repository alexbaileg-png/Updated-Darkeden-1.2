using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Allows Tab to switch between username/password fields and Enter to submit.
/// </summary>
public class LoginInputHandler : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public Button loginButton;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (usernameField != null && usernameField.isFocused)
            {
                passwordField?.Select();
                passwordField?.ActivateInputField();
            }
            else
            {
                usernameField?.Select();
                usernameField?.ActivateInputField();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (loginButton != null && loginButton.interactable)
                loginButton.onClick.Invoke();
        }
    }
}
