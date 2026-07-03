using System.Collections;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private BaseVoiceChat _lobbyVoiceChat;
    [SerializeField] private string _gameSceneName = "GameScene";
    [SerializeField] private string _lobbySceneName = "GraphicsMainMenu";
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyList;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdate;

    private const string GAME_ID_KEY = "OneLastDeliveryID_145";
    private const string HOST_ADDRESS_KEY = "HostAddress";
    private CustomNetworkManager _networkManager;
    private CSteamID _currentLobbyID;
    private Coroutine _autoRefreshCoroutine;

    private Coroutine _startGameCoroutine;

    private void Start()
    {
        _networkManager = GetComponent<CustomNetworkManager>();
        if (!SteamManager.Initialized) return;
        SteamNetworkingUtils.InitRelayNetworkAccess();
        SteamNetworkingUtils.CheckPingDataUpToDate(60f);

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyList);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _lobbySceneName)
        {
            if(_lobbyVoiceChat == null) _lobbyVoiceChat = FindAnyObjectByType<BaseVoiceChat>();
            if(_uiManager == null) _uiManager = FindAnyObjectByType<UIManager>();
        }
    }

    public void HostLobby(string lobbyName, string password)
    {
        PlayerPrefs.SetString("lobbyName", lobbyName);
        PlayerPrefs.SetString("lobbyPassword", password);
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, _networkManager.maxConnections);
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

        StartCoroutine(SetPingLocationRoutine());

        string password = PlayerPrefs.GetString("lobbyPassword");
        if (!string.IsNullOrEmpty(password)) SteamMatchmaking.SetLobbyData(_currentLobbyID, "password", password);

        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        if (_currentLobbyID == CSteamID.Nil) return;
        int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(_currentLobbyID);
        int numPlayers = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyID);
        CSteamID[] activePlayers = new CSteamID[numPlayers];

        for (int i = 0; i < numPlayers; i++)
        {
            activePlayers[i] = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyID, i);
        }

        _uiManager.SyncLobbyData(activePlayers, _currentLobbyID, maxPlayers);

        bool allReady = true;
        foreach (CSteamID steamID in activePlayers)
        {
            string readyStr = SteamMatchmaking.GetLobbyMemberData(_currentLobbyID, steamID, "ready");
            if (readyStr != "true")
            {
                allReady = false;
                break;
            }
        }
        if (allReady)
        {
            SteamMatchmaking.SetLobbyJoinable(_currentLobbyID, false);
            if(_startGameCoroutine != null)
            {
                StopCoroutine(_startGameCoroutine);
            }
            _startGameCoroutine = StartCoroutine(StartGameCountdown());
        }
        else
        {
            SteamMatchmaking.SetLobbyJoinable(_currentLobbyID, true);
            if (_startGameCoroutine != null)
            {
                DevLogger.Log("Not all players are ready. Stopping game start countdown.");
                StopCoroutine(_startGameCoroutine);
                _uiManager.UpdateCountdown();
            }
        }
    }

    IEnumerator StartGameCountdown()
    {
        int countdown = 5;
        while (countdown >= 0)
        {
            _uiManager.UpdateCountdown(Mathf.CeilToInt(countdown));
            yield return new WaitForSeconds(1f);
            countdown -= 1;
        }

        yield return new WaitForSeconds(1f);
        StartCoroutine(_uiManager.ShowLoadingScreen());
        yield return new WaitForSeconds(6f);

        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(_gameSceneName);
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        UpdatePlayerList();
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (_currentLobbyID == (CSteamID)callback.m_ulSteamIDLobby)
        {
            UpdatePlayerList();
        }
    }


    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    public void ExitLobby()
    {
        if (_lobbyVoiceChat != null) _lobbyVoiceChat.StopVoiceChat();

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            LeaveAndCloseLobby();
            _networkManager.StopHost();

            _uiManager.OnLobbyExit();
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

        SteamMatchmaking.SetLobbyMemberData(_currentLobbyID, "ready", "false");

        if (NetworkServer.active)
        {
            SteamMatchmaking.SetLobbyMemberData(_currentLobbyID, "ping", "0");
            UpdatePlayerList();
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData((CSteamID)callback.m_ulSteamIDLobby, HOST_ADDRESS_KEY);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();

        if (_lobbyVoiceChat != null) _lobbyVoiceChat.StartVoiceChat();

        _uiManager.OnJoinedLobby();
        UpdatePlayerList();

        StartCoroutine(CalculateAndSetMemberPing());
    }

    private IEnumerator CalculateAndSetMemberPing()
    {
        SteamNetworkingUtils.CheckPingDataUpToDate(60f);
        SteamNetworkPingLocation_t hostLocation, myLocation;
        float pingAge = SteamNetworkingUtils.GetLocalPingLocation(out myLocation);

        while (pingAge < 0)
        {
            yield return new WaitForSeconds(0.5f);
            pingAge = SteamNetworkingUtils.GetLocalPingLocation(out myLocation);
        }

        string hostLocationString = SteamMatchmaking.GetLobbyData(_currentLobbyID, "pingLocation");

        while (string.IsNullOrEmpty(hostLocationString))
        {
            yield return new WaitForSeconds(0.5f);
            hostLocationString = SteamMatchmaking.GetLobbyData(_currentLobbyID, "pingLocation");
        }

        SteamNetworkingUtils.ParsePingLocationString(hostLocationString, out hostLocation);
        int pingValue = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref hostLocation);

        while (pingValue == -1)
        {
            yield return new WaitForSeconds(0.5f);
            pingValue = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref hostLocation);
        }

        SteamMatchmaking.SetLobbyMemberData(_currentLobbyID, "ping", pingValue.ToString());
    }

    public void FetchLobbies()
    {
        SteamNetworkingUtils.CheckPingDataUpToDate(60f);
        SteamMatchmaking.AddRequestLobbyListStringFilter("gameID", GAME_ID_KEY, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.RequestLobbyList();

        SteamNetworkPingLocation_t pingLocation;
        float pingAge = SteamNetworkingUtils.GetLocalPingLocation(out pingLocation);

        if (pingAge < 0 && _autoRefreshCoroutine == null)
        {
            _autoRefreshCoroutine = StartCoroutine(AutoRefreshLobbiesWhenPingReady());
        }
    }

    private IEnumerator AutoRefreshLobbiesWhenPingReady()
    {
        SteamNetworkPingLocation_t pingLocation;
        float pingAge = SteamNetworkingUtils.GetLocalPingLocation(out pingLocation);

        while (pingAge < 0)
        {
            yield return new WaitForSeconds(0.5f);
            pingAge = SteamNetworkingUtils.GetLocalPingLocation(out pingLocation);
        }

        // Ping data is now ready, refresh the list automatically
        FetchLobbies();
        _autoRefreshCoroutine = null;
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
            int ping = -1;
            if (!string.IsNullOrEmpty(locationString))
            {
                SteamNetworkPingLocation_t hostLocation;
                SteamNetworkingUtils.ParsePingLocationString(locationString, out hostLocation);
                ping= SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref hostLocation);
            }
            _uiManager.AddLobbyToList(lobbyID, lobbyName, lobbyPassword, currentPlayers, maxPlayers, hostName, ping);
        }
    }

    public void InviteFriends()
    {
        SteamFriends.ActivateGameOverlayInviteDialog(_currentLobbyID);
    }

    public void ToggleReady()
    {
        if (_currentLobbyID == CSteamID.Nil) return;

        string currentReady = SteamMatchmaking.GetLobbyMemberData(_currentLobbyID, SteamUser.GetSteamID(), "ready");
        bool isReady = (currentReady == "true");
        SteamMatchmaking.SetLobbyMemberData(_currentLobbyID, "ready", (!isReady).ToString().ToLower());
    }

    private IEnumerator SetPingLocationRoutine()
    {
        SteamNetworkingUtils.CheckPingDataUpToDate(60f);
        SteamNetworkPingLocation_t pingLocation;
        float pingAge = SteamNetworkingUtils.GetLocalPingLocation(out pingLocation);

        while (pingAge < 0)
        {
            yield return new WaitForSeconds(0.5f);
            pingAge = SteamNetworkingUtils.GetLocalPingLocation(out pingLocation);
        }

        SteamNetworkingUtils.ConvertPingLocationToString(ref pingLocation, out string locationString, 256);
        if (_currentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.SetLobbyData(_currentLobbyID, "pingLocation", locationString);
            SteamMatchmaking.SetLobbyType(_currentLobbyID, ELobbyType.k_ELobbyTypePublic);
        }
    }
}