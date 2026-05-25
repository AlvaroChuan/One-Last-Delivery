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

    public void SetupPlayer(CSteamID steamID, CSteamID lobbyID)
    {
        string playerName = SteamFriends.GetFriendPersonaName(steamID);
        _playerNameText.text = playerName;
        this._steamID = steamID;

        string readyStr = SteamMatchmaking.GetLobbyMemberData(lobbyID, steamID, "ready");
        _ready = readyStr == "true";
        UpdateReadyStatus();

        string pingStr = SteamMatchmaking.GetLobbyMemberData(lobbyID, steamID, "ping");
        if (string.IsNullOrEmpty(pingStr) || pingStr == "N/A") _pingText.text = "N/A";
        else _pingText.text = pingStr + "ms";
    }

    public void UpdateReadyStatus()
    {
        if (_status != null && _status.Length >= 2)
        {
            _status[0].gameObject.SetActive(!_ready);
            _status[1].gameObject.SetActive(_ready);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(FetchAvatar(_steamID));
    }

    IEnumerator FetchAvatar(CSteamID steamID)
    {
        int avatarHandle = SteamFriends.GetMediumFriendAvatar(steamID);

        while (avatarHandle == -1)
        {
            yield return null;
        }

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
