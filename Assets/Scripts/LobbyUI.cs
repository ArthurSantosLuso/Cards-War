using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;

    [Header("Main Menu")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    [Header("Host Panel")]
    [SerializeField] private TextMeshProUGUI joinCodeDisplay;
    [SerializeField] private TextMeshProUGUI hostStatusText;
    [SerializeField] private Button hostCancelButton;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button confirmJoinButton;
    [SerializeField] private TextMeshProUGUI joinErrorText;
    [SerializeField] private Button joinCancelButton;

    [Header("References")]
    [SerializeField] private NetworkSetup networkSetup;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        hostCancelButton.onClick.AddListener(OnCancelClicked);
        joinCancelButton.onClick.AddListener(OnCancelClicked);
        confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);

        codeInputField.onValueChanged.AddListener(OnCodeInputChanged);

        ShowPanel(mainMenuPanel);
        confirmJoinButton.interactable = false;
        joinErrorText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        hostButton.onClick.RemoveListener(OnHostClicked);
        joinButton.onClick.RemoveListener(OnJoinClicked);
        hostCancelButton.onClick.RemoveListener(OnCancelClicked);
        joinCancelButton.onClick.RemoveListener(OnCancelClicked);
        confirmJoinButton.onClick.RemoveListener(OnConfirmJoinClicked);
        codeInputField.onValueChanged.RemoveListener(OnCodeInputChanged);
    }

    private void OnHostClicked()
    {
        ShowPanel(hostPanel);
        joinCodeDisplay.text = "Generating code...";
        hostStatusText.text = "Setting up game...";

        networkSetup.HostGame();
        StartCoroutine(WaitForHostCode());
    }

    private void OnJoinClicked()
    {
        codeInputField.text = "";
        joinErrorText.gameObject.SetActive(false);
        ShowPanel(joinPanel);
    }

    private void OnConfirmJoinClicked()
    {
        string code = codeInputField.text.Trim();
        if (string.IsNullOrEmpty(code)) return;

        confirmJoinButton.interactable = false;
        joinCancelButton.interactable = false;
        codeInputField.interactable = false;
        joinErrorText.gameObject.SetActive(false);

        networkSetup.JoinGame(code);
        StartCoroutine(WaitForJoinResult());
    }

    private void OnCancelClicked()
    {
        ShowPanel(mainMenuPanel);
    }

    private void OnCodeInputChanged(string value)
    {
        confirmJoinButton.interactable = !string.IsNullOrWhiteSpace(value);
        joinErrorText.gameObject.SetActive(false);
    }

    private IEnumerator WaitForHostCode()
    {
        float timeout = 15f;
        float elapsed = 0f;

        while (!networkSetup.IsHostReady)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                hostStatusText.text = "Failed to create game. Try again.";
                joinCodeDisplay.text = "---";
                yield break;
            }
            yield return null;
        }

        joinCodeDisplay.text = networkSetup.CurrentJoinCode;
        hostStatusText.text = "Waiting for player...";

        // Wait until both host + 1 client are connected
        elapsed = 0f;
        float lobbyTimeout = 60f;

        while (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lobbyTimeout)
            {
                hostStatusText.text = "No player joined. Try again.";
                yield break;
            }
            yield return null;
        }

        // Only the host loads
        // Netcode syncs the client automatically
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    //Waits a moment and shows an error if the connection didnt succeed.
    private IEnumerator WaitForJoinResult()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                yield break;
            }

            yield return null;
        }

        // Timed out
        // The connection failed
        joinErrorText.text = "Invalid code or connection failed.";
        joinErrorText.gameObject.SetActive(true);
        confirmJoinButton.interactable = true;
        joinCancelButton.interactable = true;
        codeInputField.interactable = true;
    }

    private void ShowPanel(GameObject panel)
    {
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        hostPanel.SetActive(panel == hostPanel);
        joinPanel.SetActive(panel == joinPanel);
    }
}