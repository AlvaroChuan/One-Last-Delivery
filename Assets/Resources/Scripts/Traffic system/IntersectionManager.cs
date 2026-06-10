using UnityEngine;
using System.Collections.Generic;

public class IntersectionManager : MonoBehaviour
{
    [Header("Phases")]
    [Tooltip("Lights that will be green during Phase A")]
    public List<TrafficLightController> phaseALights;
    [Tooltip("Lights that will be green during Phase B")]
    public List<TrafficLightController> phaseBLights;

    [Header("Timers")]
    public float greenTime = 10f;
    public float yellowTime = 3f;
    public float bothRedTime = 1f;

    private float _timer;
    private int _currentStep = 0; // 0=A Green, 1=A Yellow, 2=All Red, 3=B Green, 4=B Yellow, 5=All Red

    void Start()
    {
        SetLightsState(phaseALights, TrafficLightState.Green);
        SetLightsState(phaseBLights, TrafficLightState.Red);
        _timer = greenTime;
        _currentStep = 0;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f) AdvancePhase();
    }

    private void AdvancePhase()
    {
        _currentStep = (_currentStep + 1) % 6;

        switch (_currentStep)
        {
            case 0: // Phase A Green
                SetLightsState(phaseALights, TrafficLightState.Green);
                SetLightsState(phaseBLights, TrafficLightState.Red);
                _timer = greenTime;
                break;
            case 1: // Phase A Yellow
                SetLightsState(phaseALights, TrafficLightState.Yellow);
                _timer = yellowTime;
                break;
            case 2: // All Red
                SetLightsState(phaseALights, TrafficLightState.Red);
                _timer = bothRedTime;
                break;
            case 3: // Phase B Green
                SetLightsState(phaseALights, TrafficLightState.Red);
                SetLightsState(phaseBLights, TrafficLightState.Green);
                _timer = greenTime;
                break;
            case 4: // Phase B Yellow
                SetLightsState(phaseBLights, TrafficLightState.Yellow);
                _timer = yellowTime;
                break;
            case 5: // All Red
                SetLightsState(phaseBLights, TrafficLightState.Red);
                _timer = bothRedTime;
                break;
        }
    }

    private void SetLightsState(List<TrafficLightController> lights, TrafficLightState state)
    {
        foreach (var light in lights)
        {
            if (light != null) light.SetState(state);
        }
    }
}
