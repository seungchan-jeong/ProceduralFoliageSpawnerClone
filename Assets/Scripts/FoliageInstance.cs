using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageInstance
{
    public GameObject HitComponent;
    public Vector3 DrawScale3D { get; set; }
    public float ZOffset { get; set; }
    public Vector3 Location { get; set; }
    public Quaternion Rotation { get; set; }

    public void AlignToNormal(Vector3 hitNormal, object alignMaxAngle)
    {
        throw new System.NotImplementedException();
    }

    public Matrix4x4 GetInstanceWorldTransform()
    {
        return Matrix4x4.TRS(Location, Rotation, DrawScale3D);
    }
}
