using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Mirror;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private float _transitionDuration = 1f;
    [SerializeField] private AudioEvent _transitionAudioEvent;
    private AudioLoopMixer _transitionAudioLoopMixer;
    private Animator _sceneTransitionAnimator;
    void Awake()
    {
        gameObject.SetActive(false);
        _sceneTransitionAnimator = GetComponent<Animator>();
        _transitionAudioLoopMixer = GetComponent<AudioLoopMixer>();
        _transitionAudioLoopMixer.SetFadeValue(.5f);
        SceneManager.sceneLoaded += OnSceneLoaded;
        transform.position = new Vector3(-Screen.width/2, Screen.height/2, 0); // Start off-screen to the left
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameObject.SetActive(false);
        transform.position = new Vector3(-Screen.width/2, Screen.height/2, 0); // Reset position to off-screen to the left
        _transitionAudioLoopMixer.StopPlayback();
    }

    public void PlayTransition()
    {
        gameObject.SetActive(true);
        transform.DOMove(new Vector3(Screen.width/2, Screen.height/2, 0), _transitionDuration).SetEase(Ease.InOutQuad).onComplete = NotifySceneTransitionEnded;
        _sceneTransitionAnimator.Play("InTransition");
        _transitionAudioEvent.Play(gameObject);
        StartCoroutine(StartLoop());
    }

    IEnumerator StartLoop()
    {
        yield return new WaitForSeconds(_transitionDuration / 2);
        _transitionAudioLoopMixer.StartPlayback();
    }

    void NotifySceneTransitionEnded()
    {
        if(NetworkClient.active)
        {
            NetworkClient.Send(new CustomNetworkManager.SceneTransitionReceivedMessage());
        }
    }
}