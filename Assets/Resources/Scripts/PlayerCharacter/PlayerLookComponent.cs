using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class PlayerLookComponent : PlayerComponent
{
    [SerializeField] SkinnedMeshRenderer[] _meshesToHideForLocalPlayer;
    [SerializeField] private SkinnedMeshRenderer _bodyMeshRenderer;
    [SerializeField] private Mesh _headlessMesh;
    [SerializeField] private GameObject _model;
    [SerializeField] private GameObject _head;
    [SerializeField] private Transform _eyes;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Bone Corrections")]
    [SerializeField] private Vector3 _headRotationOffset = new Vector3(0f, -90f, 0f);

    [Header("Dynamic Model Offsets")]
    [SerializeField] private Vector3 _emptyHandOffset = Vector3.zero;
    [SerializeField] private Vector3 _equippedItemOffset = new Vector3(0f, 25f, 0f);
    [SerializeField] private float _offsetTransitionSpeed = 10f;

    [SerializeField] private string[] _cameraTags = new string[] { "PlayerCamera", "SpectatorCamera" };
    [SerializeField] private string _mainCameraTag = "PlayerCamera";
    private List<CinemachineCamera> _cinemachineCameras = new List<CinemachineCamera>();
    CinemachineCamera _currentCamera;
    private Vector3 _eyesInitialLocalPosition;
    private float _bobTimer = 0f;
    private float _currentAmplitude = 0f;

    private PlayerMovementComponent _movementComponent;
    private PlayerGroundCheckComponent _groundCheckComponent;
    private PlayerSprintComponent _sprintComponent;
    private PlayerInventoryComponent _inventoryComponent;

    private Vector3 _currentModelOffset;
    private Vector3 _targetModelOffset;

    [Header("Head Bobbing")]
    [SerializeField] private float _bobFrequency = 12f;
    [SerializeField] private float _sprintBobFrequency = 18f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [SerializeField] private float _sprintBobAmplitude = 0.1f;
    [SerializeField] private float _bobReturnSpeed = 5f;
    [SerializeField] private float _bobTransitionSpeed = 10f;

    Vector3 _eyeOffset;
    Quaternion _initialHeadRotation;

    public GameObject Model => _model;
    public Transform Eyes => _eyes;

    private CinemachineBasicMultiChannelPerlin _cameraNoise;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }

        _eyeOffset = _eyes.position - _head.transform.position;
        _initialHeadRotation = _head.transform.rotation;

        _bodyMeshRenderer.sharedMesh = _headlessMesh;

        foreach (var renderer in _meshesToHideForLocalPlayer)
        {
            renderer.enabled = false;
        }

        _currentModelOffset = _emptyHandOffset;
        _targetModelOffset = _emptyHandOffset;

        _eyesInitialLocalPosition = _eyes.localPosition;
        _currentAmplitude = _bobAmplitude;
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
        _sprintComponent = GetComponent<PlayerSprintComponent>();
        _inventoryComponent = GetComponent<PlayerInventoryComponent>();

        if (_inventoryComponent != null)
        {
            _inventoryComponent.onInventorySlotChangedOwner -= HandleInventoryChange;
            _inventoryComponent.onInventorySlotChangedOwner += HandleInventoryChange;
        }

        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            if (System.Array.Exists(_cameraTags, tag => camera.CompareTag(tag)))
            {
                _cinemachineCameras.Add(camera);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SwitchCamera(_mainCameraTag);
    }

    void OnEnable()
    {
        if (!isLocalPlayer) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_currentCamera != null)
        {
            _currentCamera.enabled = true;
            _currentCamera.Follow = _eyes;
            _currentCamera.LookAt = _eyes;
        }

        if (_inventoryComponent == null) _inventoryComponent = GetComponent<PlayerInventoryComponent>();
        if (_inventoryComponent != null)
        {
            _inventoryComponent.onInventorySlotChangedOwner -= HandleInventoryChange;
            _inventoryComponent.onInventorySlotChangedOwner += HandleInventoryChange;
        }
    }

    void OnDisable()
    {
        if (!isLocalPlayer) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_currentCamera != null)
        {
            _currentCamera.enabled = false;
            _currentCamera.Follow = null;
            _currentCamera.LookAt = null;
        }

        if (_inventoryComponent != null)
            _inventoryComponent.onInventorySlotChangedOwner -= HandleInventoryChange;
    }

    private void HandleInventoryChange(PlayerInventoryComponent.SlotChangeInfo info)
    {

        if (info.newSlotIndex == -1 || info.newItemData.itemID == ItemID.None)
        {
            _targetModelOffset = _emptyHandOffset;
        }
        else
        {
            _targetModelOffset = _equippedItemOffset;
        }
    }

    public void SwitchCamera(string cameraTag)
    {
        if (!isLocalPlayer) return;

        foreach (var camera in _cinemachineCameras)
        {
            if (camera.CompareTag(cameraTag))
            {
                _currentCamera = camera;
                camera.enabled = true;
                camera.Follow = _eyes;
                camera.LookAt = _eyes;
            }
            else
            {
                camera.enabled = false;
                camera.Follow = null;
                camera.LookAt = null;
            }
        }

        _cameraNoise = _currentCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        HandleHeadBob();
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;
        
        HandleRotation();
        Quaternion headRotation = _head.transform.rotation;
        Vector3 rotatedEyeOffset = headRotation * _eyeOffset;

        _eyes.position = _head.transform.position + rotatedEyeOffset;
        
        
    }

    private void HandleRotation()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;

        TruckSeat seat = transform.parent?.GetComponentInParent<TruckSeat>();
        bool isSitting = seat != null;

        if (!isSitting)
        {
            _currentModelOffset = Vector3.Lerp(_currentModelOffset, _targetModelOffset, Time.deltaTime * _offsetTransitionSpeed);

            if (flatForward.sqrMagnitude > 0.001f)
            {
                Quaternion baseModelRotation = Quaternion.LookRotation(flatForward);
                Quaternion modelTargetRotation = baseModelRotation * Quaternion.Euler(_currentModelOffset);

                _model.transform.rotation = Quaternion.Slerp(_model.transform.rotation, modelTargetRotation, _rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            _model.transform.localRotation = Quaternion.Slerp(_model.transform.localRotation, Quaternion.identity, _rotationSpeed * Time.deltaTime);
        }

        if (forward.sqrMagnitude > 0.001f)
        {
            Quaternion baseHeadRotation = Quaternion.LookRotation(forward);
            Quaternion correctedHeadRotation = baseHeadRotation * Quaternion.Euler(_headRotationOffset);

            _head.transform.rotation = correctedHeadRotation;
        }
    }

    private void HandleHeadBob()
    {
        /*
        if (_movementComponent == null || _groundCheckComponent == null) return;
        bool isSprinting = _sprintComponent != null && _sprintComponent.IsSprinting;

        if (_movementComponent.IsMoving && _groundCheckComponent.IsGrounded())
        {
            float targetFrequency = isSprinting ? _sprintBobFrequency : _bobFrequency;
            float targetAmplitude = isSprinting ? _sprintBobAmplitude : _bobAmplitude;

            _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, Time.fixedDeltaTime * _bobTransitionSpeed);
            _bobTimer += Time.fixedDeltaTime * targetFrequency;

            float verticalOffset = Mathf.Sin(_bobTimer) * _currentAmplitude;
            Vector3 targetPosition = _eyesInitialLocalPosition + new Vector3(0f, verticalOffset, 0f);

            _eyes.localPosition = Vector3.Lerp(_eyes.localPosition, targetPosition, Time.fixedDeltaTime * 15f);
        }
        else
        {
            _bobTimer = 0f;
            _currentAmplitude = _bobAmplitude;
            _eyes.localPosition = Vector3.Lerp(_eyes.localPosition, _eyesInitialLocalPosition, Time.fixedDeltaTime * _bobReturnSpeed);
        }
        */
        if (_movementComponent.IsMoving && _groundCheckComponent.IsGrounded())
        {
            bool isSprinting = _sprintComponent != null && _sprintComponent.IsSprinting;
            _cameraNoise.AmplitudeGain = isSprinting ? 1f : 0.7f;
            _cameraNoise.FrequencyGain = isSprinting ? 1f : 0.7f;
        }
        else
        {
            _cameraNoise.AmplitudeGain = 0f;
            _cameraNoise.FrequencyGain = 0f;
        }
    }
}