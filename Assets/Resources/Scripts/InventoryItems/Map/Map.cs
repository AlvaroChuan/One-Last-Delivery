using UnityEngine;
using Mirror;

public class Map : InventoryItem
{
    [SerializeField] private GameObject _model;
    [SerializeField] private Transform _openPosition;
    [SerializeField] private float _positionLerpSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 5f;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    bool _isMapOpen = false;
    bool _isMapResetting = false;

    void Awake()
    {
        _originalPosition = _model.transform.localPosition;
        _originalRotation = _model.transform.localRotation;
    }

    public override void StartUse(GameObject user)
    {
        if (!isLocalPlayer) return; // Only allow local player to use the map

        _isMapOpen = true;
    }

    public override void EndUse(GameObject user)
    {
        if (!isLocalPlayer) return; // Only allow local player to stop using the map

        _isMapOpen = false;
        _isMapResetting = true;
    }
    void LateUpdate()
    {
        if (!isLocalPlayer) return; // Only allow local player to update the map

        if (_isMapOpen)
        {
            // Position the map in front of the camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _model.transform.localPosition = Vector3.Lerp(_model.transform.localPosition, _openPosition.localPosition, Time.deltaTime * _positionLerpSpeed);
                _model.transform.localRotation = Quaternion.Lerp(_model.transform.localRotation, _openPosition.localRotation, Time.deltaTime * _rotationSpeed);
            }
        }
        else if (_isMapResetting)
        {
            // Return the map to its original position and rotation
            _model.transform.localPosition = Vector3.Lerp(_model.transform.localPosition, _originalPosition, Time.deltaTime * _positionLerpSpeed);
            _model.transform.localRotation = Quaternion.Lerp(_model.transform.localRotation, _originalRotation, Time.deltaTime * _rotationSpeed);
            if (Vector3.Distance(_model.transform.localPosition, _originalPosition) < 0.01f && Quaternion.Angle(_model.transform.localRotation, _originalRotation) < 1f)
            {
                _model.transform.localPosition = _originalPosition;
                _model.transform.localRotation = _originalRotation;
                _isMapResetting = false;
            }
        }
    }
}