using System;
using System.Collections;
using UnityEngine;

public class TrafficFader : MonoBehaviour
{
    public static Action OnCarsFadedOut;
    [SerializeField] Material[] _carMaterials;
    [SerializeField] private float _fadeDuration = 10.0f; // Duration of the fade effect in seconds
    Coroutine _fadeCoroutine;

    void Awake()
    {
        foreach (var material in _carMaterials)
        {
            material.SetFloat("_FadeValue", 0.0f);
        }

        SunManager.OnNightfall += OnNightfall;
    }

    void OnNightfall()
    {
        _fadeCoroutine = StartCoroutine(FadeOutCars());
    }

    void OnDestroy()
    {
        SunManager.OnNightfall -= OnNightfall;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        foreach (var material in _carMaterials)
        {
            material.SetFloat("_FadeValue", 0.0f);
        }
    }

    IEnumerator FadeOutCars()
    {
        DevLogger.Log("Fading out cars over " + _fadeDuration + " seconds.");
        float elapsedTime = 0.0f;

        while (elapsedTime < _fadeDuration)
        {
            float fadeValue = Mathf.Lerp(0.0f, 1.0f, elapsedTime / _fadeDuration);
            foreach (var material in _carMaterials)
            {
                material.SetFloat("_FadeValue", fadeValue);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final value is set to 1.0f
        foreach (var material in _carMaterials)
        {
            material.SetFloat("_FadeValue", 1.0f);
        }
        DevLogger.Log("Cars faded out completely.");
        OnCarsFadedOut?.Invoke();
    }
}
