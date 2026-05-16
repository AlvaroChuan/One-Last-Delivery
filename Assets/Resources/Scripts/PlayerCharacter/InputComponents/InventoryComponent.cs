using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryComponent : InputComponent
{
    [SerializeField] private int _inventorySize = 4;
    [SerializeField] private InputActionReference _scrollInput;
    [SerializeField] private InputActionReference _selectInput;
    [SerializeField] private InputActionReference _useInput;
    [SerializeField] private InputActionReference _dropInput;

    // Client-Authoritative: A standard local array.
    // The local client updates this instantly without waiting for the server.
    private InventoryItem[] _inventory;
    private int _selectedIndex = 0;

    void Awake()
    {
        _inventory = new InventoryItem[_inventorySize];
    }

    protected override void BindInputs()
    {
        if (!isLocalPlayer) return;

        _scrollInput.action.Enable();
        _scrollInput.action.performed += OnScrollInput;

        _selectInput.action.Enable();
        _selectInput.action.performed += OnSelectInput;

        _useInput.action.Enable();
        _useInput.action.performed += OnUseInput;

        _dropInput.action.Enable();
        _dropInput.action.performed += OnDropInput;
    }

    protected override void UnbindInputs()
    {
        if (!isLocalPlayer) return;

        _scrollInput.action.Disable();
        _scrollInput.action.performed -= OnScrollInput;

        _selectInput.action.Disable();
        _selectInput.action.performed -= OnSelectInput;

        _useInput.action.Disable();
        _useInput.action.performed -= OnUseInput;

        _dropInput.action.Disable();
        _dropInput.action.performed -= OnDropInput;
    }

    private void OnScrollInput(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();
        if (scrollValue > 0)
        {
            _selectedIndex = (_selectedIndex + 1) % (_inventorySize + 1) - 1;
        }
        else if (scrollValue < 0)
        {
            _selectedIndex = (_selectedIndex - 1 + _inventorySize + 1) % (_inventorySize + 1) - 1;
        }
    }

    private void OnSelectInput(InputAction.CallbackContext context)
    {
        float selectValue = context.ReadValue<float>();
        int index = Mathf.FloorToInt(selectValue) - 1;
        if (index == _selectedIndex)
        {
            _selectedIndex = -1;
        }
        else if (index >= 0 && index < _inventorySize)
        {
            _selectedIndex = index;
        }
    }

    private void OnUseInput(InputAction.CallbackContext context)
    {
        InventoryItem heldItem = GetHeldItem();
        if (heldItem != null)
        {
            // Client runs use functionality instantly
            heldItem.Use(gameObject);
        }
    }

    private void OnDropInput(InputAction.CallbackContext context)
    {
        InventoryItem heldItem = GetHeldItem();
        if (heldItem != null)
        {
            Interactable interactablePrefab = heldItem.GetInteractablePrefab();
            GameObject prefabGameObject = (interactablePrefab as MonoBehaviour)?.gameObject;

            if (prefabGameObject != null)
            {
                // Get the NetworkIdentity of the prefab to pass over the network
                NetworkIdentity networkIdentity = prefabGameObject.GetComponent<NetworkIdentity>();

                if (networkIdentity != null)
                {
                    // 1. Tell the server to spawn the physical object using the prefab asset reference
                    CmdSpawnDroppedItem(networkIdentity, transform.position + transform.forward);

                    // 2. Client immediately clears their slot locally without waiting
                    RemoveAtIndex(_selectedIndex);
                }
                else
                {
                    Debug.LogError("The dropped item prefab must have a NetworkIdentity component attached!");
                }
            }
            else
            {
                Debug.LogError("The Interactable prefab returned by GetInteractablePrefab() must be a GameObject with a NetworkIdentity!");
            }
        }
    }

    // Mirror allows passing NetworkIdentity references of Registered Spawnable Prefabs inside Commands!
    [Command]
    private void CmdSpawnDroppedItem(NetworkIdentity prefabIdentity, Vector3 dropPosition)
    {
        if (prefabIdentity == null) return;

        // Instantiate and spawn the object authoritatively on the server
        GameObject droppedObject = Instantiate(prefabIdentity.gameObject, dropPosition, Quaternion.identity);
        NetworkServer.Spawn(droppedObject);
    }

    public void AddItem(InventoryItem item)
    {
        for (int i = 0; i < _inventorySize; i++)
        {
            if (_inventory[i] == null)
            {
                _inventory[i] = item;
                return;
            }
        }
    }

    public void RemoveAtIndex(int index)
    {
        if (index >= 0 && index < _inventorySize)
        {
            _inventory[index] = null;
            if (_selectedIndex == index)
            {
                _selectedIndex = -1;
            }
        }
    }

    public InventoryItem GetHeldItem()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _inventorySize)
        {
            return _inventory[_selectedIndex];
        }
        return null;
    }
}