using UnityEngine;

public class RandomDisable : MonoBehaviour
{
    [SerializeField] private float _disableChance = 0.5f; // Chance to disable the object (0 to 1)

    void OnEnable()
    {
        if (Random.value < _disableChance)
        {
            gameObject.SetActive(false);
        }
    }
}
