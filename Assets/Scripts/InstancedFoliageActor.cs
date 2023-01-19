using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedFoliageActor
{
    public static bool FoliageTrace(out RaycastHit OutHit,  DesiredFoliageInstance DesiredInstance, string InTraceTag, bool InbReturnFaceIndex, IFoliageTraceFilter FilterFunc, bool bAverageNormal)
    {
        Vector3 Dir = (DesiredInstance.EndTrace - DesiredInstance.StartTrace).normalized;
        Vector3 StartTrace = DesiredInstance.StartTrace - (Dir * DesiredInstance.TraceRadius);
        
        RaycastHit[] Hits = Physics.SphereCastAll(StartTrace, DesiredInstance.TraceRadius, Dir,
            (DesiredInstance.EndTrace - StartTrace).magnitude);

        foreach (RaycastHit Hit in Hits)
        {
            bool bOutDiscardHit = false;
            bool bOutInsideProceduralVolumeOrArentUsingOne = false;
            if (!ValidateHit(Hit, out OutHit, FilterFunc, DesiredInstance, ref bOutDiscardHit, ref bOutInsideProceduralVolumeOrArentUsingOne))
            {
                return false;
            }

            if (bOutDiscardHit)
            {
                continue;
            }

            if (bAverageNormal && DesiredInstance.FoliageType != null
                /* && DesiredInstance.FoliageType.AverageNormal */)
            {
                // 이 부분은 Normal을 보정해주는 부분임. 현재는 크게 의미 없으므로 넘어감.
                // OutHit.normal = 
            }

            return bOutInsideProceduralVolumeOrArentUsingOne;
        }

        OutHit = new RaycastHit();
        return false;
    }

    private static bool ValidateHit(RaycastHit Hit, out RaycastHit OutHit, IFoliageTraceFilter FilterFunc, DesiredFoliageInstance DesiredInstance, ref bool bOutDiscardHit,
        ref bool bOutInsideProceduralVolumeOrArentUsingOne)
    {
        //Hit가 유효한 Primitive에 충돌한 것인지 확인하는 부분.
        OutHit = new RaycastHit();
        bOutDiscardHit = false;
        bOutInsideProceduralVolumeOrArentUsingOne = false;
        
        /*
         * TODO ProceduralFoliageBlockingVolume 부분
         * TODO ProceduralFoliageVVolume에 RayCast가 hit한 경우를 걸러내는 부분.
         */

        if (Hit.transform.gameObject.GetComponent<ProceduralFoliageVolume>() != null)
        {
            bOutDiscardHit = true;
            return true;
        }

        if (FilterFunc != null && !FilterFunc.FilterFoliageTrace(Hit.transform.gameObject))
        {
            bOutDiscardHit = true;
            return true;
        }

        bOutInsideProceduralVolumeOrArentUsingOne = true;
        if (DesiredInstance.ProceduralVolumeBodyInstance != null)
        {
            // We have a procedural volume, so lets make sure we are inside it.
            bOutInsideProceduralVolumeOrArentUsingOne =
                DesiredInstance.ProceduralVolumeBodyInstance.OverlapTestWithSphere(Hit.point, 0.01f);
        }

        OutHit = Hit;
        /*
         * TODO Foliage위에 다른 Foliage를 생성하는 경우에 대한 처리.
         */
        
        return true;
    }
    
    public static InstancedFoliageActor Get(bool bCreate, Vector3 position)
    {
        return new InstancedFoliageActor();
    }

    public FoliageType AddFoliageType(FoliageType foliageType, out FoliageInfo info)
    {
        //TODO
        info = new FoliageInfo();
        return foliageType;
    }

    public static bool CheckCollisionWithWorld(FoliageType settings, FoliageInstance inst, Vector3 hitNormal, Vector3 hitLocation, GameObject hitComponent)
    {
        return true;
        throw new System.NotImplementedException();
    }
}
