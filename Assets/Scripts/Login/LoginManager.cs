using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public Button loginButton;
    public Button registerButton;
    public TMP_Text statusText;
    public GameObject loadingOverlay;

    async void Start()
    {
        SetLoading(true);
        SetStatus("");

        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            SetStatus("Failed to connect to services: " + e.Message);
            SetLoading(false);
            return;
        }

        // Resume session if already signed in
        if (AuthenticationService.Instance.IsSignedIn)
        {
            await LoadAccountAndProceed();
            return;
        }

        SetLoading(false);
    }

    public async void OnLoginClicked()
    {
        string username = usernameField.text.Trim();
        string password = passwordField.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please enter a username and password.");
            return;
        }

        SetLoading(true);
        SetStatus("Logging in...");

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            await LoadAccountAndProceed();
        }
        catch (AuthenticationException e)
        {
            SetStatus("Login failed: " + e.Message);
            SetLoading(false);
        }
        catch (Exception e)
        {
            SetStatus("Error: " + e.Message);
            SetLoading(false);
        }
    }

    public async void OnRegisterClicked()
    {
        string username = usernameField.text.Trim();
        string password = passwordField.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please enter a username and password.");
            return;
        }

        if (password.Length < 8)
        {
            SetStatus("Password must be at least 8 characters.");
            return;
        }

        SetLoading(true);
        SetStatus("Creating account...");

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            await LoadAccountAndProceed();
        }
        catch (AuthenticationException e)
        {
            SetStatus("Registration failed: " + e.Message);
            SetLoading(false);
        }
        catch (Exception e)
        {
            SetStatus("Error: " + e.Message);
            SetLoading(false);
        }
    }

    async Task LoadAccountAndProceed()
    {
        SetStatus("Loading account...");

        // Ensure GameSession exists
        if (GameSession.Instance == null)
            new GameObject("GameSession").AddComponent<GameSession>();

        AccountData account = await CloudDataService.LoadAccountAsync();
        GameSession.Instance.SetAccountData(account);

        SceneManager.LoadScene("CharacterSelect");
    }

    void SetStatus(string message) { if (statusText != null) statusText.text = message; }

    void SetLoading(bool loading)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(loading);
        if (loginButton != null) loginButton.interactable = !loading;
        if (registerButton != null) registerButton.interactable = !loading;
    }
}
