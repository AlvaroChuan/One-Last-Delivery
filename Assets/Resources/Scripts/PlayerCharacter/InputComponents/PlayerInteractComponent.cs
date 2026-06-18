using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractComponent : InputComponent
{
    public struct InteractInfo
    {
        public Interactable interactable;
        public bool isSuccessful;
    }
    public Action<InteractInfo> onInteractEvent;
    [SerializeField] private InputActionReference _interactInput;
    [SerializeField] private float _interactionRange = 3f;
    [SerializeField] private float _interactionSphereRadius = 0.1f;
    [SerializeField] private LayerMask _interactableLayerMask;
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

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
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
                onInteractEvent?.Invoke(new InteractInfo
                {
                    interactable = interactable,
                    isSuccessful = true
                });
            }
            else
            {
                onInteractEvent?.Invoke(new InteractInfo
                {
                    interactable = null,
                    isSuccessful = false
                });
            }
        }
    }
}