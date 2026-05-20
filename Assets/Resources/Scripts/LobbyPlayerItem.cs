using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;

public class LobbyPlayerItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private Image _playerAvatarImage;

    public void SetupPlayer(CSteamID steamID)
    {
        string playerName = SteamFriends.GetFriendPersonaName(steamID);
        _playerNameText.text = playerName;

        StartCoroutine(FetchAvatar(steamID));
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
