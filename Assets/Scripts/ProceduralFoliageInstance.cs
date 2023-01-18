using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralFoliageInstance
{
    public bool _bBlocker;

    public Quaternion Rotation { get; set; }
    public Vector3 Location { get; set; }
    public Vector3 Normal { get; set; }
    public float Scale { get; set; }

    public float Age { get; set; }

    public FoliageType Type { get; set; }

    public float GetCollisionRadius()
    {
        //TODO
        return 0.0f;
    }

    public float GetMaxRadius()
    {
        //TODO
        return 0.0f;
    }
}
