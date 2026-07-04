using UnityEngine;
using Mirror;
using DG.Tweening;

public class TruckDoor : Interactable
{
    [SerializeField] private Vector3 _openPosition;
    [SerializeField] private Vector3 _openRotation;
    [SerializeField] private float _animationDuration = 0.5f;
    private Vector3 _closedPosition;
    private Vector3 _closedRotation;
    private bool _isOpen;
    private Tween _currentTween;

    private void Awake()
    {
        _closedPosition = transform.localPosition;
        _closedRotation = transform.localEulerAngles;
    }

    public override void ServerInteract(GameObject interactor)
    {
        _isOpen = !_isOpen;
        _currentTween?.Kill();
        if (_isOpen)
        {
            _currentTween = transform.DOLocalMove(_openPosition, _animationDuration);
            _currentTween = transform.DOLocalRotate(_openRotation, _animationDuration);
        }
        else
        {
            _currentTween = transform.DOLocalMove(_closedPosition, _animationDuration);
            _currentTween = transform.DOLocalRotate(_closedRotation, _animationDuration);
        }
    }
}
