using UnityEngine;

public abstract class InputComponent : PlayerComponent
{
    [SerializeField] private bool _bindInputsOnStart = true;
    bool _inputsBound = false;
    public override void OnStartLocalPlayer()
    {
        DevLogger.Log($"OnStartLocalPlayer called for {GetType().Name} on {gameObject.name}");
        base.OnStartLocalPlayer();
        if (_bindInputsOnStart)
        {
            BindInputsInternal();
        }
    }

    protected virtual void OnEnable()
    {
        DevLogger.Log($"OnEnable called for {GetType().Name} on {gameObject.name}");
        if (isLocalPlayer && !_inputsBound)
        {
            BindInputsInternal();
        }
    }
    protected virtual void OnDisable()
    {
        if (_inputsBound)
        {
            UnbindInputsInternal();
        }
    }

    private void BindInputsInternal()
    {
        if (!isLocalPlayer) return;

        DevLogger.Log($"Binding inputs for {GetType().Name} on {gameObject.name}");
        BindInputs();
        _inputsBound = true;
    }

    private void UnbindInputsInternal()
    {
        if (!isLocalPlayer) return;

        DevLogger.Log($"Unbinding inputs for {GetType().Name} on {gameObject.name}");
        UnbindInputs();
        _inputsBound = false;
    }

    protected abstract void BindInputs();
    protected abstract void UnbindInputs();
}
