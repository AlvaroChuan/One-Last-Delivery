using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AimIKTarget : MonoBehaviour
{
    [Header("Referencias (¡Asignar en Inspector!)")]
    [SerializeField] private TwoBoneIKConstraint _ikConstraint;
    [SerializeField] private Rig _rigComponent;
    private readonly Vector3 _defaultRotation = new Vector3(17.92f, -351.4f, -56.4f);
    [SerializeField] private RigBuilder _rigBuilder;

    private Transform _lookSource;
    private bool _isAiming = false;

    [Header("Aim Settings")]
    [SerializeField] float _aimDistance = 10f;
    [SerializeField] private Vector3 _handOffset = new Vector3(52.15f, -0.2f, 0f);

    [Header("Smoothing")]
    [SerializeField] private float _followSpeed = 25f;
    [SerializeField] private float _weightTransitionSpeed = 10f;

    private void Start()
    {
        if (Camera.main != null) _lookSource = Camera.main.transform;
        _rigBuilder = GetComponentInParent<RigBuilder>();
    }

    public void SetupReferences(Transform lookSource)
    {
        _lookSource = lookSource;
    }

    void Update()
    {
        float targetWeight = _isAiming ? 1f : 0f;

        if (_ikConstraint != null)
        {
            _ikConstraint.weight = Mathf.MoveTowards(_ikConstraint.weight, targetWeight, Time.deltaTime * _weightTransitionSpeed);
        }

        if (_rigComponent != null)
        {
            _rigComponent.weight = Mathf.MoveTowards(_rigComponent.weight, targetWeight, Time.deltaTime * _weightTransitionSpeed);

            if (_rigBuilder != null)
            {
                if (_rigComponent.weight < 0.001f && _rigBuilder.enabled)
                {
                    _rigBuilder.enabled = false;
                }
                else if (_rigComponent.weight >= 0.001f && !_rigBuilder.enabled)
                {
                    _rigBuilder.enabled = true;
                    _aimDistance = 116.4f;
                    _handOffset = new Vector3(72.4f, 70f, 0f);
                    transform.localEulerAngles = _defaultRotation;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!_isAiming && (_ikConstraint == null || _ikConstraint.weight == 0f)) return;
        if (_lookSource == null) return;

        Vector3 aimPoint = _lookSource.position + _lookSource.forward * _aimDistance;

        Vector3 offset =
            _lookSource.right * _handOffset.x +
            _lookSource.up * _handOffset.y +
            _lookSource.forward * _handOffset.z;

        Vector3 targetPos = aimPoint + offset;

        transform.position = Vector3.Lerp(transform.position, targetPos, _followSpeed * Time.deltaTime);
    }

    public void EnableIK()
    {
        _isAiming = true;
    }

    public void DisableIK()
    {
        _isAiming = false;
    }
}