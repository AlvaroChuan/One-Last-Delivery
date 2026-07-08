using UnityEngine;
using Mirror;
using DG.Tweening;
using System;

public class TruckDoor : Interactable
{
    [SerializeField] private Vector3 _openPosition;
    [SerializeField] private Vector3 _openRotation;
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private BoxCollider _closeCollider;
    [SerializeField] private TruckSeat _assignedSeat; // Reference to the assigned TruckSeat
    [SerializeField] private AudioEvent _doorOpenAudioEvent;
    [SerializeField] private AudioEvent _doorCloseAudioEvent;
    private Vector3 _closedPosition;
    private Vector3 _closedRotation;
    [SyncVar (hook = nameof(OnIsOpenChanged))] private bool _isOpen = false;
    private Tween _currentTween;

    private void Awake()
    {
        _closedPosition = transform.localPosition;
        _closedRotation = transform.localEulerAngles;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (_closeCollider != null)
            _closeCollider.enabled = _isOpen;
        if (_assignedSeat != null)
            _assignedSeat.CanGetUp = _isOpen; // Allow or prevent getting up from the assigned TruckSeat based on the door's state when the client starts
    }

    public override void ServerInteract(GameObject interactor)
    {
        _isOpen = !_isOpen;
        _currentTween?.Kill();
        if (_isOpen)
        {
            _currentTween = transform.DOLocalMove(_openPosition, _animationDuration);
            _currentTween = transform.DOLocalRotate(_openRotation, _animationDuration);
            RpcOpenDoor();
        }
        else
        {
            _currentTween = transform.DOLocalMove(_closedPosition, _animationDuration);
            _currentTween = transform.DOLocalRotate(_closedRotation, _animationDuration);
            RpcCloseDoor();
        }
    }

    void OnIsOpenChanged(bool oldValue, bool newValue)
    {
        if (_closeCollider != null)
            _closeCollider.enabled = newValue;
        if(_assignedSeat != null)
        {
            _assignedSeat.CanGetUp = newValue; // Allow or prevent getting up from the assigned TruckSeat based on the door's state
        }
    }

    [ClientRpc]
    void RpcOpenDoor()
    {
        _doorOpenAudioEvent.Play(gameObject);

        if (isOwned && !isServer)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOLocalMove(_openPosition, _animationDuration);
            _currentTween = transform.DOLocalRotate(_openRotation, _animationDuration);
        }
    }

    [ClientRpc]
    void RpcCloseDoor()
    {
        _doorCloseAudioEvent.Play(gameObject);

        if (isOwned && !isServer)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOLocalMove(_closedPosition, _animationDuration);
            _currentTween = transform.DOLocalRotate(_closedRotation, _animationDuration);
        }
    }
}
