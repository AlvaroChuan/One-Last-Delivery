using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Steam")]
    [SerializeField] private SteamLobbyManager _steamLobby;
    
    [Header("UI")]
    [SerializeField] private GameObject _clipboard;
    [SerializeField] private GameObject[] _pages;

    [Header("Lobby List")]
    [SerializeField] private Transform _lobbyListContent;
    [SerializeField] private GameObject _lobbyListItemPrefab;

    [Header("Players List UI")]
    [SerializeField] private Transform _playerListContent;
    [SerializeField] private GameObject _playerListItemPrefab;
}
