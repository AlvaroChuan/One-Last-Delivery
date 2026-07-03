using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways] // This makes the script run in the Editor
public class SpeedCamera : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private List<Transform> _boundPoints = new List<Transform>();
    [SerializeField] private float _boundRadius = 5f; // Radius for the sphere handles at the bound points
    [SerializeField] private GameObject _colliderHolder; // Parent object to hold the trigger colliders
    [SerializeField] private string _triggerLayer = "SpeedCamera"; // Layer for the trigger colliders
    [SerializeField] private bool _updateTriggers = false;
    private List<BoxCollider> _triggerColliders = new List<BoxCollider>();
    private List<Vector3> _lastBoundPositions = new List<Vector3>();
    private List<Transform> BoundPointsInternal
    {
        get
        {
            if (_boundPoints == null)
                _boundPoints = new List<Transform>();

            bool hasNullOrDuplicate = false;
            for(int i = 0; i < _boundPoints.Count; i++)
            {
                if (_boundPoints[i] == null)
                {
                    _boundPoints.RemoveAt(i);
                    i--; // Adjust index after removal
                    hasNullOrDuplicate = true;
                }
                if (_boundPoints.Count > 0 && _boundPoints.IndexOf(_boundPoints[i]) != i)
                {
                    DevLogger.LogWarning($"Duplicate bound point found: {_boundPoints[i].name}. Removing duplicate.");
                    _boundPoints.RemoveAt(i);
                    i--; // Adjust index after removal
                    hasNullOrDuplicate = true;
                }
            }

            if (hasNullOrDuplicate)
            {
                InstantiateTriggerColliders();
            }

            return _boundPoints;
        }
        set
        {
            _boundPoints = value;
        }
    }

    private Coroutine _instantiateCoroutine;

    void Awake()
    {
        foreach (var boundPoint in BoundPointsInternal)
        {
            _lastBoundPositions.Add(boundPoint.position);
        }
    }

    void OnValidate()
    {
        _updateTriggers = false; // Mark for update when values change in the Inspector
        if (!gameObject.activeInHierarchy)
            return;
        InstantiateTriggerColliders();
    }

    void InstantiateTriggerColliders()
    {
        if (_instantiateCoroutine != null)
        {
            StopCoroutine(_instantiateCoroutine);
        }
        _instantiateCoroutine = StartCoroutine(InstantiateTriggerCollidersCoroutine());
    }

    IEnumerator InstantiateTriggerCollidersCoroutine()
    {
        yield return null; // Wait for the end of the frame

        _triggerColliders = new List<BoxCollider>(GetComponentsInChildren<BoxCollider>());
        foreach (var collider in _triggerColliders)
        {
            DestroyImmediate(collider.gameObject);
        }
        _triggerColliders.Clear();

        for (int i = 1; i < BoundPointsInternal.Count; i++)
        {
            _triggerColliders.Add(new GameObject($"TriggerCollider_{i - 1}").AddComponent<BoxCollider>());
        }

        foreach (var collider in _triggerColliders)
        {
            collider.isTrigger = true;
            collider.transform.SetParent(_colliderHolder.transform);
            collider.gameObject.layer = LayerMask.NameToLayer(_triggerLayer);
        }

        _lastBoundPositions.Clear();

        UpdateTriggerBounds(); // Initial update to set the correct positions and sizes
        _instantiateCoroutine = null; // Reset the coroutine reference
    }

    void Update()
    {
        if (_triggerColliders.Count == 0 || BoundPointsInternal.Count == 0 || _instantiateCoroutine != null)
            return;

        UpdateTriggerBounds();
    }

    private void UpdateTriggerBounds()
    {
        bool boundsChanged = false;
        if (_lastBoundPositions.Count != BoundPointsInternal.Count)
        {
            boundsChanged = true;
        }
        else
        {
            for (int i = 0; i < BoundPointsInternal.Count; i++)
            {
                if (BoundPointsInternal[i].position != _lastBoundPositions[i])
                {
                    boundsChanged = true;
                    break;
                }
            }
        }
        if (!boundsChanged)
            return; // No change in bound positions, no need to update

        _lastBoundPositions.Clear();
        foreach (var point in BoundPointsInternal)
        {
            _lastBoundPositions.Add(point.position);
        }

        Vector3 currentBound, nextBound, position, scale;

        for (int i = 0; i < BoundPointsInternal.Count - 1; i++)
        {
            currentBound = BoundPointsInternal[i].position;
            nextBound = BoundPointsInternal[i + 1].position;

            position = (currentBound + nextBound) / 2;
            scale = new Vector3(_boundRadius * 2, _boundRadius * 2, Vector3.Distance(currentBound, nextBound) + _boundRadius * 2);
            _triggerColliders[i].transform.position = position;
            _triggerColliders[i].transform.localScale = scale;
            _triggerColliders[i].transform.rotation = Quaternion.LookRotation(nextBound - currentBound);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < BoundPointsInternal.Count - 1; i++)
        {
            Gizmos.DrawLine(BoundPointsInternal[i].position, BoundPointsInternal[i + 1].position);
        }

        foreach (var trigger in _triggerColliders)
        {
            if (trigger != null)
            {
                Gizmos.color = Color.green;
                Gizmos.matrix = trigger.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }
#endif
}