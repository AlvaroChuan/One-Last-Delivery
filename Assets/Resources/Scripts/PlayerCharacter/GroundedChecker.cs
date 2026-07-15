using UnityEngine;

public class PlayerGroundCheckComponent : PlayerComponent
{
    [SerializeField] private float _groundCheckRadius = 0.5f;
    [SerializeField] private float _groundCheckDistance = 0.6f;
    [SerializeField] private float _maxGroundAngle = 45f;
    [SerializeField] private LayerMask _groundLayer = ~0; // Default to everything
    RaycastHit[] _hitBuffer = new RaycastHit[10];
    float _minGroundDotProduct;
    private bool _cachedIsGrounded;
    private bool _useCache;

    void Awake()
    {
        _cachedIsGrounded = false;
        _useCache = false;
        _minGroundDotProduct = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
    }

    void FixedUpdate()
    {
        _useCache = false;
    }

    public bool IsGrounded()
    {
        if (_useCache)
        {
            return _cachedIsGrounded;
        }

        _cachedIsGrounded = CheckGrounded();
        _useCache = true;
        return _cachedIsGrounded;
    }

    private bool CheckGrounded()
    {
        if (!isLocalPlayer) return false;

        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;

        int hits = Physics.SphereCastNonAlloc(origin, _groundCheckRadius, direction, _hitBuffer, _groundCheckDistance, _groundLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits; i++)
        {
            RaycastHit hit = _hitBuffer[i];
            if (hit.collider != null)
            {
                if (hit.collider.gameObject != gameObject)
                {
                    float groundDot = Vector3.Dot(hit.normal, Vector3.up);
                    if (groundDot >= _minGroundDotProduct)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}