using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

public class NameTag : NetworkBehaviour
{
    TextMeshProUGUI _nameText;
    [SyncVar(hook = nameof(OnNameChanged))]
    string _name;

    public override void OnStartClient()
    {
        _nameText = GetComponent<TextMeshProUGUI>();
        if (!isLocalPlayer) return;

        CmdSetName(SteamFriends.GetPersonaName());
    }

    [Command]
    void CmdSetName(string name)
    {
        _name = name;
    }

    void OnNameChanged(string oldName, string newName)
    {
        _nameText.text = newName;
    }

    void Update()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Make the name tag face the camera
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // Rotate 180 degrees to face the camera correctly
        }
    }
}
