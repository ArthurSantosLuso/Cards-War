using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Toggle rememberMeToggle;

    private const string RememberMeKey = "RememberMePreference";

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity services Initialized successfully.");

            if (AuthenticationService.Instance.IsSignedIn)
            {
                return;
            }

            // Check if the player previously checked "Remember me"
            bool shouldRemember = PlayerPrefs.GetInt(RememberMeKey, 0) == 1;

            // If they want to be remembered and unity has a saved session token, skip the login screen
            if (shouldRemember && AuthenticationService.Instance.SessionTokenExists)
            {
                await AutoSignInWithToken();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services failed to initialize: {e.Message}");
        }
    }

    private async Task AutoSignInWithToken()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Auto Sign In Successful - Player ID: {AuthenticationService.Instance.PlayerId}");

            SceneManager.LoadScene(1);
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Auto Sign In Failed (Token may have expired): {ex.Message}");
            ClearRememberMe();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Network/Request Failed during auto sign-in: {ex.Message}");
        }
    }

    public async void SignUpPlayer()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            HandleRememberMePreference();
            SceneManager.LoadScene(1);
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Sign Up Failed: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Network/Request Failed: {ex.Message}");
        }
    }

    public async void SignInPlayer()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            HandleRememberMePreference();
            SceneManager.LoadScene(1);
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Sign In Failed: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Network/Request Failed: {ex.Message}");
        }
    }

    private void HandleRememberMePreference()
    {
        if (rememberMeToggle != null)
        {
            if (rememberMeToggle.isOn)
            {
                PlayerPrefs.SetInt(RememberMeKey, 1);
            }
            else
            {
                PlayerPrefs.SetInt(RememberMeKey, 0);

                AuthenticationService.Instance.ClearSessionToken();
            }
            PlayerPrefs.Save();
        }
    }

    private void ClearRememberMe()
    {
        PlayerPrefs.SetInt(RememberMeKey, 0);
        PlayerPrefs.Save();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(clearCredentials: true);
        }
    }

    public void LogOutPlayer()
    {
        ClearRememberMe();

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("LoginRegister");
        Debug.Log("Player logged out and cached credentials cleared.");
    }
}