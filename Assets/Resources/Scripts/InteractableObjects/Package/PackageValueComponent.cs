using UnityEngine;

[RequireComponent(typeof(PackageHealthComponent))]
public class PackageValueComponent : MonoBehaviour
{
    [SerializeField] private float _maxValue = 100f;
    [SerializeField] private float _minValue = 50f;
    [SerializeField] private float _knockPenalty = 0.1f;

    PackageHealthComponent _packageHealthComponent;
    void Awake()
    {
        _packageHealthComponent = GetComponent<PackageHealthComponent>();
        if(_packageHealthComponent == null)
        {
            Debug.LogError("Package is missing a PackageHealthComponent, please add one to the package.");
        }
    }
    public float GetValue()
    {
        float value = Mathf.Lerp(_minValue, _maxValue, _packageHealthComponent.CurrentHealth / _packageHealthComponent.MaxHealth);
        return value;
    }
    public float GetValueWithKnockPenalty()
    {
        float value = GetValue();
        float knockPenaltyAmount = value * _knockPenalty;
        return Mathf.Max(0, value - knockPenaltyAmount);
    }
    public float GetKnockPenalty()
    {
        float value = GetValue();
        return value * _knockPenalty;
    }
}