using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI fpsText;

    [Header("Settings")]
    public float updateInterval = 0.5f; // Cada cuánto tiempo se actualiza el texto

    private float timer;
    private int frameCount;

    private void Awake()
    {
        Application.targetFrameRate = 200;
    }
    void Update()
    {
        // Sumamos el tiempo real que ha pasado (independiente del Time.timeScale)
        timer += Time.unscaledDeltaTime;
        frameCount++;

        // Actualizamos el texto cuando se cumple el intervalo
        if (timer >= updateInterval)
        {
            // Calculamos el promedio de FPS en este medio segundo
            int fps = Mathf.RoundToInt(frameCount / timer);

            // Actualizamos la UI
            fpsText.text = fps.ToString() + " FPS";

            // Reiniciamos los contadores para el siguiente bloque
            timer = 0f;
            frameCount = 0;
        }
    }
}