using UnityEngine;
using TMPro;
using System.Linq;
using Steamworks;
using Edgegap;

public class UIManager : MonoBehaviour
{
    public SteamLobbyManager steamLobby;

    [Header("Panels")]
    [SerializeField] private GameObject[] _panels;

    [Header("Buttons")]
    [SerializeField] private GameObject _joinLobbyButton;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TMP_InputField _lobbyPasswordInput;
    [SerializeField] private TMP_InputField _joinLobbyPasswordInput;


    public void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in _panels) panel.SetActive(panel == panelToShow);
    }

    public void OnClickRefreshLobbies()
    {
        steamLobby.FetchLobbies();
        ShowPanel(GetPanelByName("LobbyList"));
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickHostLobby()
    {
        steamLobby.HostLobby(_lobbyNameInput.text, _lobbyPasswordInput.text);
        ShowPanel(GetPanelByName("Lobby"));
    }

    public void OnClickInviteFriends()
    {
        steamLobby.InviteFriends();
    }
    public void OnClickAudio()
    {
        ShowPanel(GetPanelByName("AudioSettings"));
    }
    public void OnAudioExit()
    {
        ShowPanel(GetPanelByName("Lobby"));
    }

    public void OnJoinedLobby()
    {
        ShowPanel(GetPanelByName("Lobby"));
    }

    public void OnLobbyExit()
    {
        ShowPanel(GetPanelByName("LobbyList"));
    }

    public GameObject GetPanelByName(string name)
    {
        return _panels.FirstOrDefault(p => p.name == name);
    }

    public void SetJoinLobbyPasswordCallbacks(LobbyListItem lobbyListItem)
    {
        _joinLobbyButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
        _joinLobbyButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { lobbyListItem.OnJoinWithPassword(_joinLobbyPasswordInput.text);});
    }
}