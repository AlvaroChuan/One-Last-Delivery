using UnityEngine;
using System.Collections.Generic;

public class TrafficObstacle : MonoBehaviour
{
    public static HashSet<TrafficObstacle> ActiveObstacles = new HashSet<TrafficObstacle>();

    private void OnEnable()
    {
        ActiveObstacles.Add(this);
    }

    private void OnDisable()
    {
        ActiveObstacles.Remove(this);
    }
}
