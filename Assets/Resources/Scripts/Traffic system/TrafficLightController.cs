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
    [HideInInspector] public ushort[] edgeIds = new ushort[0]; // The edges this light controls
    
    [Header("Phase")]
    public LightPhase phase; // Phase A or Phase B

    [Header("Gizmos")]
    public Vector3 searchOffset = Vector3.zero;

    [Header("Visuals")]
    public GameObject[] lights;
    public Material greenMaterial;
    public Material yellowMaterial;
    public Material redMaterial;
    public Material blackMaterial;

    public TrafficLightState CurrentState { get; private set; } = TrafficLightState.Red;

    public void SetState(TrafficLightState newState)
    {
        CurrentState = newState;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        foreach (GameObject light in lights)
        {
            MeshRenderer renderer = light.GetComponent<MeshRenderer>();
            if(renderer != null)
            {
                Material[] mats = renderer.sharedMaterials;
                
                mats[1] = CurrentState == TrafficLightState.Red ? redMaterial : blackMaterial;
                mats[2] = CurrentState == TrafficLightState.Green ? greenMaterial : blackMaterial;
                mats[3] = CurrentState == TrafficLightState.Yellow ? yellowMaterial : blackMaterial;
                
                renderer.sharedMaterials = mats;
            }
        }
    }
}
