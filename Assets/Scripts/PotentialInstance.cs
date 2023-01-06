using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotentialInstance
{
    public Vector3 HitLocation;
    public Vector3 HitNormal;
    public GameObject HitComponent;
    public float HitWeight;
    public DesiredFoliageInstance DesiredInstance;

    public void PlaceInstance()
    {
        
    }

    public PotentialInstance(Vector3 hitLocation, Vector3 hitNormal, GameObject hitComponent, float hitWeight, DesiredFoliageInstance desiredInstance)
    {
        HitLocation = hitLocation;
        HitNormal = hitNormal;
        HitComponent = hitComponent;
        HitWeight = hitWeight;
        DesiredInstance = desiredInstance;
    }
}
