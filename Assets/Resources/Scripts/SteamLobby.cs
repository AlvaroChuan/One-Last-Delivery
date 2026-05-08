using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public GameObject lobbyListItemPrefab;
    public Transform lobbyListContent;
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyList;

    private const string HOST_ADDRESS_KEY = "HostAddress";
    private NetworkManager _networkManager;

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
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyPassword", password);
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _networkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;

        _networkManager.StartHost();

        CSteamID lobbyID = (CSteamID)callback.m_ulSteamIDLobby;
        SteamMatchmaking.SetLobbyData(lobbyID, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(lobbyID, "name", PlayerPrefs.GetString("LobbyName"));
        
        string pwd = PlayerPrefs.GetString("LobbyPassword");
        if (!string.IsNullOrEmpty(pwd))
        {
            SteamMatchmaking.SetLobbyData(lobbyID, "password", pwd);
        }
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
        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData((CSteamID)callback.m_ulSteamIDLobby, HOST_ADDRESS_KEY);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
    }

    public void FetchLobbies()
    {
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnLobbyList(LobbyMatchList_t callback)
    {
        for (int i = lobbyListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(lobbyListContent.GetChild(i).gameObject);
        }

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");
            string lobbyPassword = SteamMatchmaking.GetLobbyData(lobbyID, "password");

            GameObject itemGo = Instantiate(lobbyListItemPrefab, lobbyListContent);
            LobbyListItem itemScript = itemGo.GetComponent<LobbyListItem>();
            itemScript.Initialize(lobbyID, lobbyName, lobbyPassword);

            itemGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => itemScript.OnJoinClicked());
        }
    }

    public void InviteFriends()
    {
        SteamFriends.ActivateGameOverlay("LobbyInvite");
    }
}