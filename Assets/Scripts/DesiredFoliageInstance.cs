using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesiredFoliageInstance 
{
    public FoliageType FoliageType;
    public Vector3 StartTrace;
    public Vector3 EndTrace;
    public Quaternion Rotation;
    public float TraceRadius;
    public float Age;

    public ProceduralFoliageVolume ProceduralVolumeBodyInstance;

    public DesiredFoliageInstance()
    {
        
    }

    public DesiredFoliageInstance(Vector3 StartRay, Vector3 EndRay, FoliageType Type, float inTraceRadius)
    {
        StartTrace = StartRay;
        EndTrace = EndRay;
        FoliageType = Type;
        TraceRadius = inTraceRadius;
    }
}
