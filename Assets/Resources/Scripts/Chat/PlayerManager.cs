using UnityEngine;
using TMPro;
using Mirror;
using Steamworks;

public class PlayerManager : NetworkBehaviour
{
    public string playerName;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        string steamName = SteamFriends.GetPersonaName();

        SteamLobbyManager lobbyManager = FindAnyObjectByType<SteamLobbyManager>();

        CmdSetPlayerName(steamName);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetPlayerName(string newName)
    {
        playerName = newName;
    }

    #region TEXT_CHAT
    [Command(requiresAuthority = false)]
    public void CmdSendMessege(string message)
    {
        string fullMessage = $"<b><color=blue>{playerName}:</color></b> {message}";
        RpcReceiveMessage(fullMessage);
    }

    [ClientRpc]
    private void RpcReceiveMessage(string message)
    {
        FindFirstObjectByType<ChatController>()
            .ReceiveMessage(message);
    }
    #endregion
}