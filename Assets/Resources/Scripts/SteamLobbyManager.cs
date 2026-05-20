using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject _lobbyListItemPrefab;
    [SerializeField] private Transform _lobbyListContent;
    [SerializeField] private UIManager _uiManager;
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyList;

    private const string GAME_ID_KEY = "OneLastDeliveryID_145";
    private const string HOST_ADDRESS_KEY = "HostAddress";
    private NetworkManager _networkManager;
    private CSteamID _currentLobbyID;

    private void Start()
    {
        _networkManager = GetComponent<NetworkManager>();
        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyList);
    }

    public void HostLobby(string lobbyName, string password)
    {
        PlayerPrefs.SetString("lobbyName", lobbyName);
        PlayerPrefs.SetString("lobbyPassword", password);
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _networkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;

        _networkManager.StartHost();
        _currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;

        SteamMatchmaking.SetLobbyData(_currentLobbyID, "gameID", GAME_ID_KEY);
        SteamMatchmaking.SetLobbyData(_currentLobbyID, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(_currentLobbyID, "name", PlayerPrefs.GetString("lobbyName"));

        string password = PlayerPrefs.GetString("lobbyPassword");
        if (!string.IsNullOrEmpty(password))  SteamMatchmaking.SetLobbyData(_currentLobbyID, "password", password);
    }

    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;

        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData((CSteamID)callback.m_ulSteamIDLobby, HOST_ADDRESS_KEY);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
        _uiManager.OnJoinedLobby();
    }

    public void FetchLobbies()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter("GameID", GAME_ID_KEY, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnLobbyList(LobbyMatchList_t callback)
    {
        for (int i = _lobbyListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_lobbyListContent.GetChild(i).gameObject);
        }

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");
            string lobbyPassword = SteamMatchmaking.GetLobbyData(lobbyID, "password");

            GameObject item = Instantiate(_lobbyListItemPrefab, _lobbyListContent);
            LobbyListItem itemScript = item.GetComponent<LobbyListItem>();
            itemScript.Initialize(lobbyID, this, _uiManager, lobbyName, lobbyPassword);
        }
    }

    public void InviteFriends()
    {
        SteamFriends.ActivateGameOverlayInviteDialog(_currentLobbyID);
    }
}