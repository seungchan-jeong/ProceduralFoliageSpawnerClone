using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdModeFoliage
{
    private const int NUM_INSTANCE_BUCKETS = 10;
    private const string NAME_AddFoliageInstances = "AddFoliageInstances";

    public static void AddInstances(List<DesiredFoliageInstance> DesiredInstances,
        in FoliagePaintingGeometryFilter OverrideGeometryFilter, bool InRebuildFoliageTree)
    {
        //Debug
        foreach (DesiredFoliageInstance desiredFoliageInstance in DesiredInstances)
        {
            DebugDraw.DrawSphere(desiredFoliageInstance.StartTrace, 2.0f, Color.green, 6);
        }
        
        Dictionary<FoliageType, List<DesiredFoliageInstance>> SettingsInstancesMap = DesiredInstances.GroupBy(inst => inst.FoliageType).ToDictionary(keySelector: g => g.Key, elementSelector: g => g.ToList());

        foreach (KeyValuePair<FoliageType, List<DesiredFoliageInstance>> KeyValue in SettingsInstancesMap)
        {
            FoliageType FoliageType = KeyValue.Key;
            List<DesiredFoliageInstance> Instances = KeyValue.Value;

            AddInstancesImp(FoliageType, Instances, new List<int>() ,1.0f, OverrideGeometryFilter, InRebuildFoliageTree);
        }
    }

    private static bool AddInstancesImp(FoliageType FoliageType, List<DesiredFoliageInstance> DesiredInstances, List<int> ExistingInstanceBuckets,
        in float Pressure, in FoliagePaintingGeometryFilter OverrideGeometryFilter, in bool InRebuildFoliageTree)
    {
        if (DesiredInstances.Count == 0)
        {
            return false;
        }

        //CalculatePotentialInstances 부분
        List<PotentialInstance>[] PotentialInstanceBuckets = Enumerable.Range(0, NUM_INSTANCE_BUCKETS)
            .Select((i) => new List<PotentialInstance>()).ToArray();
        CalculatePotentialInstances_ThreadSafe(FoliageType, DesiredInstances, ref PotentialInstanceBuckets, 0,
            DesiredInstances.Count-1, OverrideGeometryFilter);

        Dictionary<InstancedFoliageActor, List<FoliageType>> UpdatedTypesByIFA =
            new Dictionary<InstancedFoliageActor, List<FoliageType>>();
        foreach (List<PotentialInstance> Bucket in PotentialInstanceBuckets)
        {
            foreach (PotentialInstance PotentialInst in Bucket)
            {
                FoliageInstance Inst = new FoliageInstance();
                PotentialInst.PlaceInstance(FoliageType, ref Inst, true);
                
                //TODO
                // InstancedFoliageActor TargetIFA = InstancedFoliageActor.Get();

                // List<FoliageType> UpdateTypes;
                // if (!UpdatedTypesByIFA.TryGetValue(TargetIFA, out UpdateTypes))
                // {
                //     UpdateTypes = new List<FoliageType>();
                //     UpdatedTypesByIFA.Add(TargetIFA, UpdateTypes);
                // }
                //
                // if (!UpdateTypes.Contains(PotentialInst.DesiredInstance.FoliageType))
                // {
                //     UpdateTypes.Add(PotentialInst.DesiredInstance.FoliageType);
                //     TargetIFA.AddFoliageType(PotentialInst.DesiredInstance.FoliageType);
                // }
            }
        }

        //TODO Spawn 하는 부분
        bool bPlacedInstances = false;
        for (int BucketIdx = 0; BucketIdx < NUM_INSTANCE_BUCKETS; BucketIdx++)
        {
            List<PotentialInstance> PotentialInstances = PotentialInstanceBuckets[BucketIdx];
            float bucketFraction = (float)(BucketIdx + 1) / (float)NUM_INSTANCE_BUCKETS;

            int bucketOffset = ExistingInstanceBuckets.Count != 0 ? ExistingInstanceBuckets[BucketIdx] : 0 ;
            int AdditionalInstances =
                Mathf.Clamp(
                    Mathf.RoundToInt(bucketFraction * (float)(PotentialInstances.Count - bucketOffset) * Pressure), 
                    0,
                    PotentialInstances.Count);

            List<FoliageInstance> PlacedInstances = new List<FoliageInstance>(AdditionalInstances);

            for (int i = 0; i < AdditionalInstances; i++)
            {
                PotentialInstance potentialInstance = PotentialInstances[i];
                FoliageInstance inst = new FoliageInstance();
                if (potentialInstance.PlaceInstance(FoliageType, ref inst))
                {
                    //Inst.ProceduralGuid = PotentialInstance.DesiredInstance.ProceduralGuid;
                    inst.HitComponent = potentialInstance.HitComponent;
                    PlacedInstances.Add(inst);
                    bPlacedInstances = true;
                }
            }
            
            SpawnFoliageInstance(FoliageType, PlacedInstances, InRebuildFoliageTree);
            
            // Debug
            foreach (PotentialInstance PotentialInstance in PotentialInstances)
            {
                DesiredFoliageInstance DesiredInstance = PotentialInstance.DesiredInstance;
            
                //Debug
                DebugDraw.DrawSphere(PotentialInstance.HitLocation, 2.0f, Color.red, 6);
            }
        }
        return bPlacedInstances;
    }

    private static void CalculatePotentialInstances_ThreadSafe(FoliageType Settings,
        List<DesiredFoliageInstance> DesiredInstances, ref List<PotentialInstance>[] PotentialInstanceBuckets,
        int StartIdx, int LastIdx, in FoliagePaintingGeometryFilter OverrideGeometryFilter)
    {
        PotentialInstanceBuckets.Select(bucket => bucket.Capacity = DesiredInstances.Count);

        for (int InstancedIdx = StartIdx; InstancedIdx <= LastIdx; InstancedIdx++)
        {
            DesiredFoliageInstance DesiredInst = DesiredInstances[InstancedIdx];
            RaycastHit Hit;

            IFoliageTraceFilter foliageTraceFilter = OverrideGeometryFilter;

            if (InstancedFoliageActor.FoliageTrace(out Hit, DesiredInst, NAME_AddFoliageInstances, true,
                    foliageTraceFilter, true))
            {
                float HitWeight = 1.0f;

                if (Hit.transform == null)
                {
                    continue;
                }

                bool bValidInstance = CheckLocationForPotentialInstance_ThreadSafe(Settings, Hit.point, Hit.normal);
                    // && VertexMaskCheck(Hit, Settings)
                    // && LandscapeLayerCheck(Hit, Settings, HitWeight);

                if (bValidInstance)
                {
                    int BucketIndex = Mathf.RoundToInt(HitWeight * (float)NUM_INSTANCE_BUCKETS - 1);
                    PotentialInstanceBuckets[BucketIndex].Add(new PotentialInstance(Hit.point, Hit.normal, Hit.transform.gameObject, HitWeight, DesiredInst));
                }
            }
        }
    }

    private static void SpawnFoliageInstance(FoliageType Settings, List<FoliageInstance> PlacedInstances,
        bool InRebuildFoliageTree)
    {
        Dictionary<InstancedFoliageActor, List<FoliageInstance>> PerIFAPlacedInstances = new Dictionary<InstancedFoliageActor, List<FoliageInstance>>();
        bool bSpawnInCurrentLevel = true;
        foreach (FoliageInstance PlacedInstance in PlacedInstances)
        {
            InstancedFoliageActor IFA = InstancedFoliageActor.Get(true, PlacedInstance.Location);
            if (IFA != null)
            {
                if (PerIFAPlacedInstances.TryGetValue(IFA, out List<FoliageInstance> instances))
                {
                    instances.Add(PlacedInstance);
                }
                else
                {
                    PerIFAPlacedInstances[IFA] = new List<FoliageInstance>(){ PlacedInstance };
                }
            }
        }

        foreach (var PlacedLevelInstances in PerIFAPlacedInstances)
        {
            InstancedFoliageActor IFA = PlacedLevelInstances.Key;

            CurrentFoliageTraceBrushAffectedIFAs.Add(IFA);

            FoliageInfo Info = null;
            FoliageType FoliageSettings = IFA.AddFoliageType(Settings, out Info);

            Info.AddInstances(FoliageSettings, PlacedLevelInstances.Value);
            if (InRebuildFoliageTree)
            {
                Info.Refresh(true, false);
            }
        }
    }
    
    static HashSet<InstancedFoliageActor> CurrentFoliageTraceBrushAffectedIFAs = new HashSet<InstancedFoliageActor>();

    private static bool CheckLocationForPotentialInstance_ThreadSafe(FoliageType Settings, Vector3 Location, Vector3 Normal)
    {
        return true;
    }

    private static bool VertexMaskCheck()
    {
        return false;
    }

    private static bool LandscapeLayerCheck()
    {
        return false; 
    }
    
    
    
}