using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;

public class LobbyListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    private CSteamID _lobbyID;
    private string _actualPassword;
    private SteamLobbyManager _steamLobbyManager;
    private UIManager _uiManager;

    public void Initialize(CSteamID id, SteamLobbyManager steamLobbyManager, UIManager uiManager, string name, string password)
    {
        _lobbyID = id;
        _actualPassword = password;
        _steamLobbyManager = steamLobbyManager;
        _uiManager = uiManager;
        bool hasPassword = !string.IsNullOrEmpty(password);
        _lobbyNameText.text = name + (hasPassword ? " [🔒]" : "");
        GetComponent<Button>().onClick.AddListener(OnJoinClicked);
    }

    public void OnJoinClicked()
    {
        if(string.IsNullOrEmpty(_actualPassword)) _steamLobbyManager.JoinLobby(_lobbyID);
        else 
        {
            _uiManager.ShowPanel(_uiManager.GetPanelByName("JoinPassword"));
            _uiManager.SetJoinLobbyPasswordCallbacks(this);
        }
    }

    public void OnJoinWithPassword(string enteredPassword)
    {
        if (enteredPassword == _actualPassword) _steamLobbyManager.JoinLobby(_lobbyID);
    }
}