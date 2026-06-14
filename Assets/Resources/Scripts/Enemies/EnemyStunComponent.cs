using UnityEngine;
using Mirror;
using System;

public class EnemyStunComponent : NetworkBehaviour
{
    public struct StunChangeInfo
    {
        public bool isStunned;
    }
    private bool _isStunned;
    public bool IsStunned => _isStunned;
    private float _stunDuration = 0f; // Duration of the stun effect in seconds
    public Action<StunChangeInfo> onStunChangedEvent;

    public void Stun(float duration)
    {
        if (duration <= 0f) return; // Ignore non-positive durations
        CmdStun(duration);
    }
    public void Unstun()
    {
        CmdUnstun();
    }

    void OnDisable()
    {
        if (isServer && _isStunned)
        {
            _stunDuration = 0f;
            ServerStopStun();
        }
    }

    [Command(requiresAuthority = false)]
    void CmdStun(float duration)
    {
        if (!_isStunned)
        {
            ServerStartStun();
        }
        _stunDuration += duration;
    }
    [Command(requiresAuthority = false)]
    void CmdUnstun()
    {
        _stunDuration = 0f;
        ServerStopStun();
    }

    void Update()
    {
        if (isServer && _isStunned && _stunDuration > 0f)
        {
            _stunDuration -= Time.deltaTime;
            if (_stunDuration <= 0f)
            {
                _stunDuration = 0f;
                ServerStopStun();
            }
        }
    }

    [Server]
    void ServerStartStun()
    {
        if (_isStunned || !enabled) return;

        _isStunned = true;
        onStunChangedEvent?.Invoke(new StunChangeInfo
        {
            isStunned = true
        });
    }

    [Server]
    void ServerStopStun()
    {
        if (!_isStunned) return;

        _isStunned = false;
        onStunChangedEvent?.Invoke(new StunChangeInfo
        {
            isStunned = false
        });
    }
}