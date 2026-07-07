using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioEvent _buttonClickAudioEvent;

    void OnEnable()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlayButtonClickSound);
        }
    }

    void OnDisable()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonClickSound);
        }
    }

    private void PlayButtonClickSound()
    {
        _buttonClickAudioEvent.Play(gameObject);
    }
}