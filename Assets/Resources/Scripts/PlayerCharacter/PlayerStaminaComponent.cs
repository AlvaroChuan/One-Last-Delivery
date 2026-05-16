using UnityEngine;

public class PlayerStaminaComponent : PlayerComponent
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 5f;
    private float _currentStamina;
    private int _staminaregenDisableCounter = 0;

    protected override void Start()
    {
        base.Start();
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

        _currentStamina -= amount;
        if (_currentStamina < 0)
        {
            _currentStamina = 0;
        }
    }

    public void RegenerateStamina(float amount)
    {
        if (_currentStamina >= _maxStamina) return;

        _currentStamina += amount;
        if (_currentStamina > _maxStamina)
        {
            _currentStamina = _maxStamina;
        }
    }

    public float GetCurrentStamina()
    {
        return _currentStamina;
    }
}