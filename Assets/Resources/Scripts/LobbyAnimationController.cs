using UnityEngine;

public class LobbyAnimationController : MonoBehaviour
{
    private enum AnimationState
    {
        Drive,
        Pick
    }

    [SerializeField] private AnimationState _state;
    private Animator _animator;
    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_state == AnimationState.Drive)
        {
            _animator.SetTrigger("Driving");
        }
    }
}
