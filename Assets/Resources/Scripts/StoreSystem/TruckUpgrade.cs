using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewTruckUpgrade", menuName = "Store/Truck Upgrade", order = 1)]
public class TruckUpgrade : ScriptableObject
{
    [SerializeField] private TruckStatsStruct _stats;
    [SerializeField] private float _price;
    [SerializeField] private string _upgradeName;
    [SerializeField] private Image _upgradeIcon;

    public TruckStatsStruct Stats => _stats;
    public float Price => _price;
    public string UpgradeName => _upgradeName;
    public Image UpgradeIcon => _upgradeIcon;
}
[System.Serializable]
public struct TruckStatsStruct
{
    [Header("Suspension")]
    [SerializeField] public float suspensionRestLength; //rest position of the suspension springs (height of the vehicle)
    [SerializeField] public float springStrength; //how stiff the springs are
    [SerializeField] public float springDamping; //how much the springs resist oscillation

    [Header("Steering")]
    [SerializeField] public float maxSteeringAngle; //maximum angle the front wheels can turn
    [SerializeField] public float steeringSpeed; //how fast the wheels turn to the target angle
    [SerializeField] public float wheelGripFactor; //how much the wheels resist lateral sliding WIP must implement a curve for grip vs speed
    [SerializeField] public float wheelMass; //mass of each wheel, affects how much force is needed to change its velocity

    [Header("Powertrain")]
    [SerializeField] public float carMaxSpeed; //maximum speed of the car
    [SerializeField] public float maxEngineTorque; //maximum torque output of the engine

    [Header("Braking")]
    [SerializeField] public float engineBrakeTorque; //torque applied when not accelerating
    [SerializeField] public float brakeTorque; //torque applied by the brakes

    public void Add(TruckStatsStruct other)
    {
        suspensionRestLength += other.suspensionRestLength;
        springStrength += other.springStrength;
        springDamping += other.springDamping;

        maxSteeringAngle += other.maxSteeringAngle;
        steeringSpeed += other.steeringSpeed;
        wheelGripFactor += other.wheelGripFactor;
        wheelMass += other.wheelMass;

        carMaxSpeed += other.carMaxSpeed;
        maxEngineTorque += other.maxEngineTorque;

        engineBrakeTorque += other.engineBrakeTorque;
        brakeTorque += other.brakeTorque;
    }
}
