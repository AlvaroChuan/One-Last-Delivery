using UnityEngine;
using System.Collections;
using System;

public class WreckedCarFader : MonoBehaviour
{
    public static Action OnFadeCompleted;
    [SerializeField] private float _fadeDuration = 3.0f;
    private Material[] _carMaterials;
    private Renderer _renderer;
    
    private void Awake()
    {
        _renderer = transform.GetChild(0).GetComponent<Renderer>();
        _carMaterials = _renderer.materials;
    }

    private void OnEnable()
    {
        WreckedCar.OnCarExploded += Fade;
    }

    private void OnDisable()
    {
        WreckedCar.OnCarExploded -= Fade;
    }

    private void Fade()
    {
        _renderer.materials = _carMaterials;
        StartCoroutine(FadeCorroutine());
    }

    private IEnumerator FadeCorroutine()
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < _fadeDuration)
        {
            float fadeValue = Mathf.Lerp(0.0f, 1.0f, elapsedTime / _fadeDuration);
            foreach (var material in _carMaterials) material.SetFloat("_FadeValue", fadeValue);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        foreach (var material in _carMaterials)
        {
            material.SetFloat("_FadeValue", 0.0f);
        }
        OnFadeCompleted?.Invoke();
    }
}
