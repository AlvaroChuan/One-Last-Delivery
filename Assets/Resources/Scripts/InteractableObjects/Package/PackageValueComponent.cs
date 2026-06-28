using UnityEngine;

[RequireComponent(typeof(PackageHealthComponent))]
public class PackageValueComponent : MonoBehaviour
{
    [SerializeField] private float _maxValue = 100f;
    [SerializeField] private float _minValue = 50f;

    PackageHealthComponent _packageHealthComponent;
    void Awake()
    {
        _packageHealthComponent = GetComponent<PackageHealthComponent>();
        if(_packageHealthComponent == null)
        {
            DevLogger.LogError("Package is missing a PackageHealthComponent, please add one to the package.");
        }
    }
    public float GetValue()
    {
        return _maxValue;
    }
    public float GetPenalty()
    {
        float healthPercentage = _packageHealthComponent.CurrentHealth / _packageHealthComponent.MaxHealth;
        float penalty = Mathf.Lerp(_maxValue, _minValue, 1 - healthPercentage);
        return penalty;
    }
}