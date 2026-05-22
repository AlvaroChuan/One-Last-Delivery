using UnityEngine;

public abstract class InputComponent : PlayerComponent
{
    bool _inputsBound = false;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        BindInputsInternal();
    }

    protected virtual void OnEnable()
    {
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
