using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    [SerializeField] private InputActionReference action;

    void OnEnable()
    {
        action.action.Enable();
        action.action.performed += _ => Debug.LogError("Funcionaaaa");
    }

}
