using UnityEngine;

public class PlayerGrassTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalVector("_Player", transform.position);
        Debug.Log(Shader.GetGlobalVector("_Player"));
    }


}
