using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TransactionNotification : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _reasonText;
    [SerializeField] private TMPro.TextMeshProUGUI _amountText;
    [SerializeField] private float _displayDuration = 3f;
    [SerializeField] private float _fadeInDuration = .2f;
    [SerializeField] private float _fadeOutDuration = .5f;
    Image _backgroundImage;

    void Awake()
    {
        _backgroundImage = GetComponent<Image>();
        _backgroundImage.color = new Color(_backgroundImage.color.r, _backgroundImage.color.g, _backgroundImage.color.b, 0);
        _amountText.color = new Color(_amountText.color.r, _amountText.color.g, _amountText.color.b, 0);
        _reasonText.color = new Color(_reasonText.color.r, _reasonText.color.g, _reasonText.color.b, 0);
    }

    public void Initialize(Transaction transaction)
    {
        _amountText.text = Math.Sign(transaction.amount) == 1 ? $"+${transaction.amount:0.00}" : $"-${Math.Abs(transaction.amount):0.00}";
        _reasonText.text = transaction.reason;
        DOTween.ToAlpha(() => _amountText.color, x => _amountText.color = x, 1, _fadeInDuration);
        DOTween.ToAlpha(() => _reasonText.color, x => _reasonText.color = x, 1, _fadeInDuration);
        DOTween.ToAlpha(() => _backgroundImage.color, x => _backgroundImage.color = x, 1, _fadeInDuration).OnComplete(InvokeFade);
    }

    void InvokeFade()
    {
        Invoke(nameof(StartFade), _displayDuration);
    }

    void StartFade()
    {
        // Start fading out
        DOTween.ToAlpha(() => _amountText.color, x => _amountText.color = x, 0, _fadeOutDuration);
        DOTween.ToAlpha(() => _reasonText.color, x => _reasonText.color = x, 0, _fadeOutDuration);
        DOTween.ToAlpha(() => _backgroundImage.color, x => _backgroundImage.color = x, 0, _fadeOutDuration).OnComplete(DestroySelf);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}