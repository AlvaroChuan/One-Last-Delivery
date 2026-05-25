using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;

public class LobbyPlayerItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private Image _playerAvatarImage;
    [SerializeField] private TextMeshProUGUI _pingText;
    [SerializeField] private Button _muteButton;
    [SerializeField] private Image[] _status;
    private CSteamID _steamID;
    private bool _ready = false;

    public CSteamID SteamID => _steamID;
    private bool _isAvatarFetched = false;

    public void SetupPlayer(CSteamID steamID, CSteamID lobbyID)
    {
        this._steamID = steamID;
        string playerName = SteamFriends.GetFriendPersonaName(steamID);
        _playerNameText.text = playerName;

        string readyStr = SteamMatchmaking.GetLobbyMemberData(lobbyID, steamID, "ready");
        _ready = readyStr == "true";
        UpdateReadyStatus();

        string pingStr = SteamMatchmaking.GetLobbyMemberData(lobbyID, steamID, "ping");
        if (string.IsNullOrEmpty(pingStr) || pingStr == "N/A") _pingText.text = "N/A";
        else _pingText.text = pingStr + "ms";
    }

    public void OnEnable()
    {
        if (!_isAvatarFetched)
        {
            _isAvatarFetched = true;
            StartCoroutine(FetchAvatar(_steamID));
        }
    }

    public void UpdateReadyStatus()
    {
        if (_status != null && _status.Length >= 2)
        {
            _status[0].gameObject.SetActive(!_ready);
            _status[1].gameObject.SetActive(_ready);
        }
    }

    IEnumerator FetchAvatar(CSteamID steamID)
    {
        int avatarHandle = SteamFriends.GetMediumFriendAvatar(steamID);

        float timeout = 5f;
        while (avatarHandle == 0 && timeout > 0)
        {
            avatarHandle = SteamFriends.GetMediumFriendAvatar(steamID);
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (avatarHandle != 0)
        {
            uint width, height;
            if (SteamUtils.GetImageSize(avatarHandle, out width, out height))
            {
                byte[] imageBuffer = new byte[width * height * 4];

                if (SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, (int)(width * height * 4)))
                {
                    Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                    texture.LoadRawTextureData(imageBuffer);
                    texture.Apply();
                    _playerAvatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
    }
}
