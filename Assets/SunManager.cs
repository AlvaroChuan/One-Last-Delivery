using UnityEngine;

public class SunManager : MonoBehaviour
{
    [ExecuteInEditMode]
    void Update()
    {
        Shader.SetGlobalVector("_SunDirection", transform.forward);
    }
}
