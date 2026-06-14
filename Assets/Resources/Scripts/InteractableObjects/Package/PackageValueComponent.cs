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
        float value = Mathf.Lerp(_minValue, _maxValue, _packageHealthComponent.CurrentHealth / _packageHealthComponent.MaxHealth);
        return value;
    }
}