using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerHealthComponent : HealthComponent
{
    PlayerController _controller;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }
    protected override void Die()
    {
        _controller.Die();
    }
}