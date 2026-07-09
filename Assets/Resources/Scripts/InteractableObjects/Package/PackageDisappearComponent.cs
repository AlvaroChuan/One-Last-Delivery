using UnityEngine;
using Mirror;
using System.Collections;

public class PackageDisappearComponent : MonoBehaviour
{
    [SerializeField] ParticleSystem _disappearEffect;

    void OnDisable()
    {
        ParticleSystem effect = Instantiate(_disappearEffect, transform.position, Quaternion.identity);
        effect.Play();
    }
}