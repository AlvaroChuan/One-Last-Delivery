using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Mirror;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private Animator _sceneTransitionAnimator;
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        transform.position = new Vector3(-Screen.width/2, Screen.height/2, 0); // Start off-screen to the left
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        transform.position = new Vector3(-Screen.width/2, Screen.height/2, 0); // Reset position to off-screen to the left
        _sceneTransitionAnimator.gameObject.SetActive(false);
    }

    public void PlayTransition()
    {

        _sceneTransitionAnimator.gameObject.SetActive(true);
        transform.DOMove(new Vector3(Screen.width/2, Screen.height/2, 0), 1f).SetEase(Ease.InOutQuad).onComplete = NotifySceneTransitionEnded;
        _sceneTransitionAnimator.Play("InTransition");
    }

    void NotifySceneTransitionEnded()
    {
        if(NetworkClient.active)
        {
            NetworkClient.Send(new CustomNetworkManager.SceneTransitionReceivedMessage());
        }
    }
}