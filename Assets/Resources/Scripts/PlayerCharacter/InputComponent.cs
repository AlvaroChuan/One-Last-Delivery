using UnityEngine;
using Mirror;

public abstract class InputComponent : NetworkBehaviour
{
    bool _inputsBound = false;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        BindInputsInternal();
    }

    protected virtual void Start()
    {
        if (!isLocalPlayer)
        {
            enabled = false; // Disable this component for non-local players
        }
    }

    protected virtual void OnEnable()
    {
        Debug.Log("Component " + GetType().Name + " enabled. isLocalPlayer: " + isLocalPlayer);
        if (isLocalPlayer && !_inputsBound)
        {
            BindInputsInternal();
        }
    }
    protected virtual void OnDisable()
    {
        Debug.Log("Component " + GetType().Name + " disabled. isLocalPlayer: " + isLocalPlayer);
        if (_inputsBound)
        {
            UnbindInputsInternal();
        }
    }

    private void BindInputsInternal()
    {
        if (!isLocalPlayer) return;

        BindInputs();
        _inputsBound = true;
    }

    private void UnbindInputsInternal()
    {
        if (!isLocalPlayer) return;

        UnbindInputs();
        _inputsBound = false;
    }

    protected abstract void BindInputs();
    protected abstract void UnbindInputs();
}
