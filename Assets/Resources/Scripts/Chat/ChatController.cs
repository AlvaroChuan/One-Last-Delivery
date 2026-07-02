using Mirror;
using TMPro;
using UnityEngine;

public class ChatController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _chatHistoryText;
    [SerializeField] private TMP_InputField _chatInputField;

    private PlayerManager _localPlayer;

    private void OnEnable()
    {
        _chatInputField.onSubmit.AddListener(delegate { SendMessage(); });
    }

    private void OnDisable()
    {
        _chatHistoryText.text = "";
        _chatInputField.text = "";
        _chatInputField.onSubmit.RemoveListener(delegate { SendMessage(); });
    }

    public void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_chatInputField.text)) return;

        if (_localPlayer is null)
            _localPlayer = NetworkClient.localPlayer.gameObject.GetComponent<PlayerManager>();

        _localPlayer.CmdSendMessege(_chatInputField.text);

        _chatInputField.text = "";
        _chatInputField.ActivateInputField();
    }

    public void ReceiveMessage(string message)
    {
        _chatHistoryText.text += message + "\n";
    }
}
