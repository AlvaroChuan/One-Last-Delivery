using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways] // This makes the script run in the Editor
public class SpeedCamera : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private List<Transform> _boundPoints = new List<Transform>();
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
                if (_boundPoints.IndexOf(_boundPoints[i]) != i)
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

        _triggerColliders = new List<BoxCollider>(GetComponents<BoxCollider>());
        foreach (var collider in _triggerColliders)
        {
            DestroyImmediate(collider);
        }
        _triggerColliders.Clear();

        for (int i = 1; i < BoundPointsInternal.Count; i++)
        {
            _triggerColliders.Add(gameObject.AddComponent<BoxCollider>());
        }

        foreach (var collider in _triggerColliders)
        {
            collider.isTrigger = true;
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
            return; // No change in corner positions, no need to update

        _lastBoundPositions.Clear();
        foreach (var point in BoundPointsInternal)
        {
            _lastBoundPositions.Add(point.position);
        }

        Vector3 currentCorner, nextCorner, center, size;

        for (int i = 0; i < BoundPointsInternal.Count - 1; i++)
        {
            currentCorner = BoundPointsInternal[i].position;
            nextCorner = BoundPointsInternal[i + 1].position;

            center = (currentCorner + nextCorner) / 2;
            center = transform.InverseTransformPoint(center);
            size = new Vector3(Mathf.Abs(nextCorner.x - currentCorner.x), 20, Mathf.Abs(nextCorner.z - currentCorner.z));

            _triggerColliders[i].center = center;
            _triggerColliders[i].size = size;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < BoundPointsInternal.Count - 1; i++)
        {
            Gizmos.DrawLine(BoundPointsInternal[i].position, BoundPointsInternal[i + 1].position);
        }
        Gizmos.color = Color.green;
        foreach (var trigger in _triggerColliders)
        {
            Gizmos.DrawWireCube(trigger.transform.TransformPoint(trigger.center), trigger.size);
        }
    }
#endif
}