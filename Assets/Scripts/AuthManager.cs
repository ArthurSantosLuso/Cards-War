using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;


    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity services Initialized successfuly.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services failed to initialize: {e.Message}");
        }
    }

    // This method is called the player clicks your "Sign Up" button
    public async void SignUpPlayer()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log($"Sign Up Successful - Player ID: {AuthenticationService.Instance.PlayerId}");
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

    // This method is called when the player clicks your "Log In" button
    public async void SignInPlayer()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log($"Sign In Successful - Player ID: {AuthenticationService.Instance.PlayerId}");
            SceneManager.LoadScene(0);
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

}
