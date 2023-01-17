using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProceduralFoliageVolume : MonoBehaviour
{
    private Collider _collider;
    public bool OverlapTestWithSphere(Vector3 sphereCenter, float radius)
    {
        if (_collider == null)
        {
            _collider = GetComponent<Collider>();
        }
        return IntersectsWithSphere(_collider.bounds, sphereCenter, radius);
    }

    //https://stackoverflow.com/questions/4578967/cube-sphere-intersection-test
    bool IntersectsWithSphere(Bounds bounds, Vector3 sphereCenter, float radius)
    {
        float minDistanceSquared = 0;
        for(int i = 0; i < 3; i++ ) {
            if( sphereCenter[i] < bounds.min[i] ) 
                minDistanceSquared += (sphereCenter[i] - bounds.min[i]) * (sphereCenter[i] - bounds.min[i]);
            else if( sphereCenter[i] > bounds.max[i] ) 
                minDistanceSquared += (sphereCenter[i] - bounds.max[i]) * (sphereCenter[i] - bounds.max[i]);     
        }
        
        float r2 = radius * radius;
        return minDistanceSquared <= r2;
    }
}
