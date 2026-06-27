using UnityEngine;

[RequireComponent(typeof(PlayerAttack))]
public class Bat : InventoryItem
{
    [SerializeField] private float _cooldown = 1f; // Cooldown duration in seconds
    private float _timer = 0f;
    private Animator _animator;
    void Awake()
    {
        _animator = GetComponentInParent<Animator>();
        DevLogger.Log($"Bat Awake: {_animator.name}");
    }
    public override void StartUse(GameObject user)
    {
        if (_timer > 0f)
        {
            return; // Still on cooldown
        }
        _animator.SetTrigger("BatAttack");
        _timer = _cooldown;
    }
    void Update()
    {
        if (_timer > 0)
        {
            _timer -= Time.deltaTime;
        }
    }
}
