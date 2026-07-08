using UnityEngine;
using Mirror;
using System.Collections;

public class PackageDisappearComponent : NetworkBehaviour
{
    [SerializeField] private float _fadeDuration = 1f;

#if UNITY_EDITOR
    [ContextMenu("Disappear Package")]
    void ContextMenuDisappear()
    {
        StartDisappear();
    }
#endif

    [Server]
    public void StartDisappear()
    {
        RpcDisappear();
        Invoke(nameof(DestroyPackage), _fadeDuration);
    }

    void DestroyPackage()
    {
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    public void RpcDisappear()
    {
        StartCoroutine(DisappearCoroutine());
    }

    IEnumerator DisappearCoroutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        float elapsedTime = 0.0f;

        while (elapsedTime < _fadeDuration)
        {
            float fadeValue = Mathf.Lerp(0.0f, 1.0f, elapsedTime / _fadeDuration);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.SetFloat("_FadeValue", fadeValue);
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final value is set to 1.0f
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                material.SetFloat("_FadeValue", 1.0f);
            }
        }
    }
}