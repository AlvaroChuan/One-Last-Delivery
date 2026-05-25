using UnityEngine;
using Mirror;
using Steamworks;
using Adrenak.UniVoice.Samples;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject _lobbyListItemPrefab;
    [SerializeField] private Transform _lobbyListContent;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private LobbyVoiceChat _lobbyVoiceChat;
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyList;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;

    private const string GAME_ID_KEY = "OneLastDeliveryID_145";
    private const string HOST_ADDRESS_KEY = "HostAddress";
    private NetworkManager _networkManager;
    private CSteamID _currentLobbyID;

    private void Start()
    {
        _networkManager = GetComponent<NetworkManager>();
        if (!SteamManager.Initialized) return;
        SteamNetworkingUtils.InitRelayNetworkAccess();
        SteamNetworkingUtils.CheckPingDataUpToDate(0);

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyList);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        if(_lobbyVoiceChat == null) _lobbyVoiceChat = _networkManager.GetComponent<LobbyVoiceChat>();
    }

    private void OnDestroy()
    {
        StopNetworkConnections();
    }

    private void StopNetworkConnections()
    {
        try
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error trying to shutdown Mirror: {e.Message}");
        }
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

        if (_lobbyVoiceChat != null)
            _lobbyVoiceChat.StartVoiceChat();

        _currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;

        SteamMatchmaking.SetLobbyData(_currentLobbyID, "gameID", GAME_ID_KEY);
        SteamMatchmaking.SetLobbyData(_currentLobbyID, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(_currentLobbyID, "name", PlayerPrefs.GetString("lobbyName"));
        SteamMatchmaking.SetLobbyData(_currentLobbyID, "hostName", SteamFriends.GetPersonaName());
        SteamNetworkPingLocation_t pingLocation;
        float pingAge = SteamNetworkingUtils.GetLocalPingLocation(out pingLocation);
        if (pingAge >= 0)
        {
            SteamNetworkingUtils.ConvertPingLocationToString(ref pingLocation, out string locationString, 256);
            SteamMatchmaking.SetLobbyData(_currentLobbyID, "pingLocation", locationString);
        }

        string password = PlayerPrefs.GetString("lobbyPassword");
        if (!string.IsNullOrEmpty(password))  SteamMatchmaking.SetLobbyData(_currentLobbyID, "password", password);

        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        _uiManager.ClearPlayerList();

        int numPlayers = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyID);

        for (int i = 0; i < numPlayers; i++)
        {
            CSteamID playerSteamID = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyID, i);
            _uiManager.AddPlayerToList(playerSteamID);
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        UpdatePlayerList();
    }


    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    public void ExitLobby()
    {
        if (_lobbyVoiceChat != null)
            _lobbyVoiceChat.StopVoiceChat();

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            LeaveAndCloseLobby();
            _networkManager.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            LeaveLobbyOnly();
            _networkManager.StopClient();
        }
        _uiManager.OnLobbyExit();
    }

    public void LeaveAndCloseLobby()
    {
        if (_currentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.SetLobbyData(_currentLobbyID, "gameID", "CLOSED");
            SteamMatchmaking.SetLobbyData(_currentLobbyID, "name", "Lobby Cerrada");

            SteamMatchmaking.SetLobbyJoinable(_currentLobbyID, false);
            SteamMatchmaking.SetLobbyType(_currentLobbyID, ELobbyType.k_ELobbyTypePrivate);

            SteamMatchmaking.LeaveLobby(_currentLobbyID);
            _currentLobbyID = CSteamID.Nil;
        }
    }

    public void LeaveLobbyOnly()
    {
        if (_currentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(_currentLobbyID);
            _currentLobbyID = CSteamID.Nil;
        }
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

        if (_lobbyVoiceChat != null)
            _lobbyVoiceChat.StartVoiceChat();

        _uiManager.OnJoinedLobby();
        UpdatePlayerList();
    }

    public void FetchLobbies()
    {
        SteamNetworkingUtils.CheckPingDataUpToDate(0);
        SteamMatchmaking.AddRequestLobbyListStringFilter("gameID", GAME_ID_KEY, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnLobbyList(LobbyMatchList_t callback)
    {
        _uiManager.ClearLobbyList();

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");
            string lobbyPassword = SteamMatchmaking.GetLobbyData(lobbyID, "password");
            int currentPlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
            string hostName = SteamMatchmaking.GetLobbyData(lobbyID, "hostName");
            string locationString = SteamMatchmaking.GetLobbyData(lobbyID, "pingLocation");
            string ping = "N/A";
            if (!string.IsNullOrEmpty(locationString))
            {
                Debug.Log($"Lobby {lobbyName} has ping location string: {locationString}");
                SteamNetworkPingLocation_t hostLocation;
                SteamNetworkingUtils.ParsePingLocationString(locationString, out hostLocation);
                int pingValue = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref hostLocation);
                ping = pingValue != -1 ? $"{pingValue} ms" : "N/A";
            }
            _uiManager.AddLobbyToList(lobbyID, lobbyName, lobbyPassword, currentPlayers, maxPlayers, hostName, ping);
        }
    }

    public void InviteFriends()
    {
        SteamFriends.ActivateGameOverlayInviteDialog(_currentLobbyID);
    }
}