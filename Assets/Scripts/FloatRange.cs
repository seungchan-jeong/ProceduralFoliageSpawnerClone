using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FloatRange
{
    //FloatRangeParameter 를 사용하려다가, 새로 만듬.
    public float min;
    public float max;

    public float Interpolate(float alpha)
    {
        return min + (max - min) * alpha;
    }
}
