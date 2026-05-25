using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;

public class LobbyListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _hostNameText;
    [SerializeField] private TextMeshProUGUI _pingText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private Image[] _statusIcons; // 0: open entry, 1: password required, 2: full
    private CSteamID _lobbyID;
    private string _actualPassword;
    private SteamLobbyManager _steamLobbyManager;
    private UIManager _uiManager;

    public void Initialize(CSteamID id, SteamLobbyManager steamLobbyManager, UIManager uiManager, string name, string password, int currentPlayers, int maxPlayers, string hostName, int ping)
    {
        _lobbyID = id;
        _actualPassword = password;
        _steamLobbyManager = steamLobbyManager;
        _uiManager = uiManager;
        
        bool hasPassword = !string.IsNullOrEmpty(password);
        _lobbyNameText.text = name;

        _playerCountText.text = $"{currentPlayers}/{maxPlayers}";

        _hostNameText.text = $"Host: {hostName}";
        
        _pingText.text = ping != -1 ? $"{ping} ms" : "N/A ms";

        bool isFull = currentPlayers >= maxPlayers;

        _statusIcons[0].gameObject.SetActive(!hasPassword && !isFull);
        _statusIcons[1].gameObject.SetActive(hasPassword && !isFull);
        _statusIcons[2].gameObject.SetActive(isFull);

        GetComponent<Button>().onClick.AddListener(OnJoinClicked);
    }

    public void OnJoinClicked()
    {
        if(string.IsNullOrEmpty(_actualPassword)) _steamLobbyManager.JoinLobby(_lobbyID);
        else _uiManager.OnPassWordRequired(this);
    }

    public void OnJoinWithPassword(string enteredPassword)
    {
        if (enteredPassword == _actualPassword) _steamLobbyManager.JoinLobby(_lobbyID);
    }
}