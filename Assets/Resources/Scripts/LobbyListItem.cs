using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;

public class LobbyListItem : MonoBehaviour
{
    public TextMeshProUGUI lobbyNameText;
    public CSteamID lobbyID;
    public string actualPassword;

    public void Initialize(CSteamID id, string name, string password)
    {
        lobbyID = id;
        actualPassword = password;

        bool hasPassword = !string.IsNullOrEmpty(password);
        lobbyNameText.text = name + (hasPassword ? " [🔒]" : "");
    }

    public void OnJoinClicked()
    {
        // Nota: Si hasPassword es true, aquí deberías abrir un panel para introducir la contraseña y compararla con 'actualPassword'.
        FindFirstObjectByType<SteamLobby>().JoinLobby(lobbyID);
    }
}