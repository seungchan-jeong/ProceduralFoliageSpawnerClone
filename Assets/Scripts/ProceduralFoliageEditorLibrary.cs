using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralFoliageEditorLibrary
{
    public void ResimulateProceduralFoliageComponents(params ProceduralFoliageComponent[] proceduralFoliageComponents)
    {
        foreach (ProceduralFoliageComponent Comp in proceduralFoliageComponents)
        {
            if (Comp != null)
            {
                Comp.ResimulateProceduralFoliage((DesiredFoliageInstances) =>
                {
                    FoliagePaintingGeometryFilter foliagePaintingGeometryFilter =
                        new FoliagePaintingGeometryFilter(Comp.bAllowLandscape,
                            Comp.bAllowStaticMesh);

                    EdModeFoliage.AddInstances(DesiredFoliageInstances, foliagePaintingGeometryFilter);
                });
            }
        }
    }
    
}
