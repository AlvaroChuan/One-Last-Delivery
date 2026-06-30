using System;
using UnityEngine;

public class PlayerStaminaComponent : PlayerComponent
{
    public struct StaminaChangeInfo
    {
        public float oldStamina;
        public float newStamina;
        public float maxStamina;
    }
    public Action<StaminaChangeInfo> onStaminaChangedEvent;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 5f;
    private float _currentStamina;
    private int _staminaregenDisableCounter = 0;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _currentStamina = _maxStamina;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (_staminaregenDisableCounter > 0) return;

        if (_currentStamina < _maxStamina)
        {
            RegenerateStamina(_staminaRegenRate * Time.fixedDeltaTime);
        }
    }

    public bool HasEnoughStamina(float amount)
    {
        return _currentStamina >= amount;
    }

    public void DisableStaminaRegen(float duration = -1f)
    {
        _staminaregenDisableCounter += 1;
        if (duration > 0)
        {
            Invoke(nameof(EnableStaminaRegenInternal), duration);
        }
    }

    public void EnableStaminaRegen(float delay =  -1f)
    {
        if (delay > 0)
        {
            Invoke(nameof(EnableStaminaRegenInternal), delay);
        }
        else
        {
            EnableStaminaRegenInternal();
        }
    }

    void EnableStaminaRegenInternal()
    {
        _staminaregenDisableCounter -= 1;
        if (_staminaregenDisableCounter < 0)
        {
            _staminaregenDisableCounter = 0;
        }
    }

    public void ConsumeStamina(float amount)
    {
        if (_currentStamina <= 0) return;

        float oldStamina = _currentStamina;

        _currentStamina -= amount;
        if (_currentStamina < 0)
        {
            _currentStamina = 0;
        }

        onStaminaChangedEvent?.Invoke(new StaminaChangeInfo
        {
            oldStamina = oldStamina,
            newStamina = _currentStamina,
            maxStamina = _maxStamina
        });
    }

    public void RegenerateStamina(float amount)
    {
        if (_currentStamina >= _maxStamina) return;

        float oldStamina = _currentStamina;

        _currentStamina += amount;
        if (_currentStamina > _maxStamina)
        {
            _currentStamina = _maxStamina;
        }

        onStaminaChangedEvent?.Invoke(new StaminaChangeInfo
        {
            oldStamina = oldStamina,
            newStamina = _currentStamina,
            maxStamina = _maxStamina
        });
    }

    public float GetCurrentStamina()
    {
        return _currentStamina;
    }
}