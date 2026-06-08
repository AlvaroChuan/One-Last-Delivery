using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractComponent : InputComponent
{
    [SerializeField] private InputActionReference _interactInput;
    [SerializeField] private float _interactionRange = 3f;
    [SerializeField] private float _interactionSphereRadius = 0.1f;
    [SerializeField] private LayerMask _interactableLayerMask;
    private Camera _playerCamera;

    void Awake()
    {
        _playerCamera = GetComponentInChildren<Camera>();
    }

    protected override void BindInputs()
    {
        if (!isLocalPlayer) return;

        _interactInput.action.Enable();
        _interactInput.action.performed += OnInteractInput;
    }

    protected override void UnbindInputs()
    {
        if (!isLocalPlayer) return;

        _interactInput.action.Disable();
        _interactInput.action.performed -= OnInteractInput;
    }

    private void OnInteractInput(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        RaycastHit hitInfo;
        if (Physics.SphereCast(ray, _interactionSphereRadius, out hitInfo, _interactionRange, _interactableLayerMask))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable == null)
            {
                interactable = hitInfo.collider.GetComponentInParent<Interactable>();
            }
            if (interactable != null)
            {
                interactable.CmdInteract(GetComponent<NetworkIdentity>());
                interactable.LocalInteract(gameObject);
            }
        }
    }
}