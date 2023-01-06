using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FoliagePaintingGeometryFilter : IFoliageTraceFilter
{
    public bool bAllowLandscape;
    public bool bAllowStaticMesh;
    // public bool bAllowBSP;
    // public bool bAllowFoliage;
    // public bool bAllowTranslucent;
    public FoliagePaintingGeometryFilter(bool bAllowLandscape, bool bAllowStaticMesh)
    {
        this.bAllowLandscape = bAllowLandscape;
        this.bAllowStaticMesh = bAllowStaticMesh;
    }

    public bool FilterFoliageTrace(GameObject obj)
    {
        return true;
        throw new System.NotImplementedException();
    }
}
