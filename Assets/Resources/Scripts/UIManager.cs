using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public SteamLobby steamLobby;

    [Header("Paneles")]
    public GameObject mainMenuPanel;
    public GameObject lobbyListPanel;
    public GameObject createLobbyPanel;
    public GameObject inLobbyPanel;

    [Header("Inputs")]
    public TMP_InputField lobbyNameInput;
    public TMP_InputField lobbyPasswordInput;

    public void ShowPanel(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        lobbyListPanel.SetActive(false);
        createLobbyPanel.SetActive(false);
        inLobbyPanel.SetActive(false);
        
        panel.SetActive(true);
    }

    public void OnClickRefreshLobbies()
    {
        steamLobby.FetchLobbies();
        ShowPanel(lobbyListPanel);
    }

    public void OnClickHostLobby()
    {
        steamLobby.HostLobby(lobbyNameInput.text, lobbyPasswordInput.text);
        ShowPanel(inLobbyPanel);
    }

    public void OnClickInviteFriends()
    {
        steamLobby.InviteFriends();
    }
}