using UnityEngine;
using Mirror;

public class WorkdayManager : MonoBehaviour
{
    [SerializeField] private string _sceneToLoadAfterWorkday = "BalanceScene";
    [SerializeField] private float _workdayDurationMinutes = 15f;
    private float _workdayDurationSeconds;
    private float _workdayTimer;

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

#if UNITY_EDITOR
    [ContextMenu("End Workday (Simulate Workday Progression)")]
#endif
    void EndWorkday()
    {
        if (!NetworkServer.active) return;

        NetworkManager.singleton.ServerChangeScene(_sceneToLoadAfterWorkday);
    }
}