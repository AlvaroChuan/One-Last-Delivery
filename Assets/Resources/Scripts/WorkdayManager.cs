using UnityEngine;
using Mirror;

public class WorkdayManager : NetworkBehaviour
{
    [SerializeField] private string _sceneToLoadAfterWorkday = "BalanceScene";
    [SerializeField] private float _workdayDurationMinutes = 15f;
    private float _workdayDurationSeconds;
    private float _workdayTimer;

    public float WorkdayProgress => Mathf.Clamp01(_workdayTimer / _workdayDurationSeconds);

    void Awake()
    {
        _workdayDurationSeconds = _workdayDurationMinutes * 60f;
    }

    void Update()
    {
        _workdayTimer += Time.deltaTime;

        if (_workdayTimer >= _workdayDurationSeconds)
        {
            EndWorkday();
        }
    }

    void EndWorkday()
    {
        if (!NetworkServer.active) return;

        (NetworkManager.singleton as CustomNetworkManager).ServerChangeSceneWithTransition(_sceneToLoadAfterWorkday);
    }

#if UNITY_EDITOR
    [ContextMenu("End Workday (Simulate Workday Progression)")]
#endif
    [Command(requiresAuthority = false)]
    void CmdEndWorkday()
    {
        EndWorkday();
    }
}