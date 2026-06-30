using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI fpsText;

    [Header("Settings")]
    public float updateInterval = 0.5f;

    private float _timer;
    private int _frameCount;

    private void Awake()
    {
        Application.targetFrameRate = -1;
    }
    void Update()
    {
        _timer += Time.unscaledDeltaTime;
        _frameCount++;
        if (_timer >= updateInterval)
        {
            int fps = Mathf.RoundToInt(_frameCount / _timer);
            fpsText.text = fps.ToString() + " FPS";
            _timer = 0f;
            _frameCount = 0;
        }
    }
}