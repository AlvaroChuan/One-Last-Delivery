using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class CameraNoFog : MonoBehaviour
{
    private Camera _rtCamera;
    private bool _originalFogState;
    private FogMode _originalFogMode; 
    private void Awake()
    {
        _rtCamera = GetComponent<Camera>();
    }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
    }
    private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _rtCamera)
        {
            _originalFogState = RenderSettings.fog;
            _originalFogMode = RenderSettings.fogMode;
            RenderSettings.fog = false;
            Shader.DisableKeyword("FOG_LINEAR");
            Shader.DisableKeyword("FOG_EXP");
            Shader.DisableKeyword("FOG_EXP2");
        }
    }
    private void OnEndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _rtCamera)
        {
            RenderSettings.fog = _originalFogState;
            if (_originalFogState)
            {
                switch (_originalFogMode)
                {
                    case FogMode.Linear:
                        Shader.EnableKeyword("FOG_LINEAR");
                        break;
                    case FogMode.Exponential:
                        Shader.EnableKeyword("FOG_EXP");
                        break;
                    case FogMode.ExponentialSquared:
                        Shader.EnableKeyword("FOG_EXP2");
                        break;
                }
            }
        }
    }
}