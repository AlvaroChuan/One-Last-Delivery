using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public enum TrafficLightState : byte
    {
        Green = 0,
        Yellow = 1,
        Red = 2
    }

    public enum LightPhase
    {
        PhaseA,
        PhaseB
    }

    [HideInInspector] public int lightId = -1; // Assigned by the Baker
    [HideInInspector] public ushort edgeId = 0xFFFF; // The edge this light controls
    
    [Header("Phase")]
    public LightPhase phase; // Phase A or Phase B


    [Header("Visuals")]
    public GameObject greenLight;
    public GameObject yellowLight;
    public GameObject redLight;

    public TrafficLightState CurrentState { get; private set; } = TrafficLightState.Red;

    public void SetState(TrafficLightState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (greenLight) greenLight.SetActive(CurrentState == TrafficLightState.Green);
        if (yellowLight) yellowLight.SetActive(CurrentState == TrafficLightState.Yellow);
        if (redLight) redLight.SetActive(CurrentState == TrafficLightState.Red);
    }
}
