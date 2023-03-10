using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;

public class ProceduralFoliageTile
{
    private int _simulationStep = 0;
    private bool _bSimulateOnlyInShade;
    private int _randomSeed;
    private RandomStream _randomStream;
    private ProceduralFoliageBroadphase _broadphase;
    private ProceduralFoliageSpawner _foliageSpawner;
    
    private List<ProceduralFoliageInstance> _instancesArray;
    private HashSet<ProceduralFoliageInstance> _instancesSet;

    public void Simulate(ProceduralFoliageSpawner inFoliageSpawner, in int inRandomSeed, in int maxNumSteps)
    {
        InitSimulation(inFoliageSpawner, inRandomSeed);
        
        RunSimulation(maxNumSteps, false);
        RunSimulation(maxNumSteps, true);

        // ShowDebug();
    }

    private void ShowDebug()
    {
        foreach (ProceduralFoliageInstance inst in _instancesArray)
        {
            DebugDraw.DrawSphere(inst.Location, 2.0f, Color.green, 6);
            Debug.Log(inst.Location);
        }
    }

    public void InitSimulation(in ProceduralFoliageSpawner inFoliageSpawner, in int inRandomSeed)
    {
        _randomSeed = inRandomSeed;
        _randomStream = new RandomStream(inRandomSeed); //TODO thread-safe 하지 않을 듯. 확인 필요.
        _foliageSpawner = inFoliageSpawner;
        _simulationStep = 0;
        _broadphase = new ProceduralFoliageBroadphase(inFoliageSpawner._tileSize, inFoliageSpawner._minimumQuadTreeSize);
        _instancesArray ??= new List<ProceduralFoliageInstance>();
        _instancesSet ??= new HashSet<ProceduralFoliageInstance>();
    }

    private void RunSimulation(in int maxNumSteps, bool bOnlyInShade)
    {
        int MaxSteps = 0;
        
        foreach(FoliageTypeObject foliageTypeObject in _foliageSpawner.GetFoliageTypes())
        {
            FoliageType typeInstance = foliageTypeObject.GetInstance();
            if (typeInstance != null&& typeInstance.GetSpawnsInShade() == bOnlyInShade)
            {
                MaxSteps = Mathf.Max(MaxSteps, typeInstance.NumSteps + 1);       
            }
        }

        if (maxNumSteps >= 0)
        {
            MaxSteps = Mathf.Max(MaxSteps, maxNumSteps);
        }

        _simulationStep = 0;
        _bSimulateOnlyInShade = bOnlyInShade;
        for (int step = 0; step < MaxSteps; step++)
        {
            StepSimulation();
            _simulationStep++;
        }
        
        InstancesToArray();
    }

    private void StepSimulation()
    {
        List<ProceduralFoliageInstance> newInstances = new List<ProceduralFoliageInstance>();
        if (_simulationStep == 0)
        {
            AddRandomSeeds(newInstances);
        }
        else
        {
            AgeSeeds();
            SpreadSeeds(newInstances);
        }
        
        _instancesSet.AddRange(newInstances);
        FlushPendingRemovals();
    }
    
    private void InstancesToArray()
    {
        _instancesArray = _instancesSet.Where(inst => inst._bBlocker == false).ToList();
    }

    private void AddRandomSeeds(List<ProceduralFoliageInstance> outInstances)
    {
        float SizeTenM2 = ( _foliageSpawner._tileSize * _foliageSpawner._tileSize ) / ( 10.0f * 10.0f ); //TODO 1000.0f 가 맞나? 10.0f 해야하나?

        Dictionary<int, float> maxShadeRadii = new Dictionary<int, float>();
        Dictionary<int, float> maxCollisionRadii = new Dictionary<int, float>();
        Dictionary<FoliageType, int> seedsLeftMap = new Dictionary<FoliageType, int>(); //FoliageType 당 심어야 하는 남은 씨앗 갯수
        Dictionary<FoliageType, RandomStream> randomStreamPerType = new Dictionary<FoliageType, RandomStream>();

        List<FoliageType> typesToSeed = new List<FoliageType>();
        foreach(FoliageTypeObject foliageTypeObject in _foliageSpawner.GetFoliageTypes())
        {
            FoliageType typeInstance = foliageTypeObject.GetInstance();
            if (typeInstance != null && typeInstance.GetSpawnsInShade() == _bSimulateOnlyInShade)
            {
                {   //심어야 하는 초기 씨앗 갯수 계산
                    int numSeeds = Mathf.RoundToInt(typeInstance._initialSeedDensity * SizeTenM2);
                    seedsLeftMap.Add(typeInstance, numSeeds);

                    if (numSeeds > 0)
                    {
                        typesToSeed.Add(typeInstance);
                    }
                }

                {   //RandomSeed 저장
                    randomStreamPerType.Add(typeInstance, new RandomStream(typeInstance._distributionSeed + _foliageSpawner._randomSeed + _randomSeed));
                }
                
                {	//compute the needed offsets for initial seed variance
                    int distributionSeed = typeInstance._distributionSeed;
                    float maxScale = typeInstance.GetScaleForAge(typeInstance._maxAge);
                    
                    float typeMaxCollisionRadius = maxScale * typeInstance._collisionRadius;
                    if (maxCollisionRadii.TryGetValue(distributionSeed, out float maxCollisionRadius))
                    {
                        maxCollisionRadii[distributionSeed] = Mathf.Max(maxCollisionRadius, typeMaxCollisionRadius);
                    }
                    else
                    {
                        maxCollisionRadii.Add(distributionSeed, typeMaxCollisionRadius);
                    }

                    float typeMaxShadeRadius = maxScale * typeInstance._shadeRadius;
                    if (maxShadeRadii.TryGetValue(distributionSeed, out float maxShadeRadius))
                    {
                        maxShadeRadii[distributionSeed] = Mathf.Max(maxShadeRadius, typeMaxShadeRadius);
                    }
                    else
                    {
                        maxShadeRadii.Add(distributionSeed, typeMaxShadeRadius);
                    }
                }
            }
        }

        int typeIdx = -1;
        int numTypes = typesToSeed.Count;
        int typeLeftToSeed = numTypes;
        int lastShadeCastingIndex = _instancesArray.Count - 1; //when placing shade growth types we want to spawn in shade if possible
        while (typeLeftToSeed > 0)
        {
            typeIdx = (typeIdx + 1) % numTypes;	//keep cycling through the types that we spawn initial seeds for to make sure everyone gets fair chance
            if (typesToSeed[typeIdx] != null)
            {
                FoliageType type = typesToSeed[typeIdx];
                if (seedsLeftMap.TryGetValue(type, out int seedsLeft))
                {
                    if (seedsLeft == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    Debug.LogError("Invalid type detected."); //TODO 버그 맞나?
                }

                float newAge = type.GetInitAge(_randomStream);
                float scale = type.GetScaleForAge(newAge);

                if (!randomStreamPerType.TryGetValue(type, out RandomStream typeRandomStream))
                {
                    Debug.LogError("Invalid type detected.");//TODO 버그 맞나?, Throw Exception 해야하는거 아님? Error 말고?
                };
                float initX = 0.0f;
                float initZ = 0.0f;
                float neededRadius = 0.0f;

                if (_bSimulateOnlyInShade && lastShadeCastingIndex >= 0)
                {
                    int InstanceSpawnerIdx = typeRandomStream.RandRange(0, lastShadeCastingIndex);
                    ProceduralFoliageInstance foliageInstance = _instancesArray[InstanceSpawnerIdx];
                    initX = foliageInstance.Location.x;
                    initZ = foliageInstance.Location.y;
                    neededRadius = foliageInstance.GetCollisionRadius() * (scale + foliageInstance.Type.GetScaleForAge(foliageInstance.Age));
                }
                else
                {
                    initX = typeRandomStream.RandRange(0, _foliageSpawner._tileSize);
                    initZ = typeRandomStream.RandRange(0, _foliageSpawner._tileSize);
                    maxShadeRadii.TryGetValue(type._distributionSeed, out neededRadius);
                }

                float Rad = _randomStream.RandRange(0, Mathf.PI*2.0f);
                Vector3 GlobalOffset = (_randomStream.RandRange(0, type._maxInitialSeedOffset) + neededRadius) * new Vector3(Mathf.Cos(Rad), Mathf.Sin(Rad), 0.0f);

                float X = initX + GlobalOffset.x;
                float Z = initZ + GlobalOffset.y;

                ProceduralFoliageInstance newInst = NewSeed(new Vector3(X, 0.0f, Z), scale, type, newAge);
                if (newInst != null)
                {
                    outInstances.Add(newInst);
                }

                seedsLeftMap[type] = seedsLeftMap[type] - 1;
                if (seedsLeftMap[type] == 0)
                {
                    --typeLeftToSeed;
                }
            }
        }

    }

    private ProceduralFoliageInstance NewSeed(Vector3 location, float scale, FoliageType type, float inAge, bool bBlocker = false)
    {
        float initRadius = type.GetMaxRadius() * scale;
        ProceduralFoliageInstance newInst = new ProceduralFoliageInstance();
        newInst.Location = location;

        // make a new local random stream to avoid changes to instance randomness changing the position of all other procedural instances
        RandomStream LocalStream = _randomStream; //TODO 복사 생성 따로 만들어야 할 듯.
        _randomStream.GetUnsignedInt(); // advance the parent stream by one
        
        newInst.Rotation = 
            Quaternion.Euler(new Vector3(LocalStream.RandRange(0, type._randomPitchAngle), LocalStream.RandRange(0, type._randomYaw ? 360 : 0), 0.0f));
        newInst.Age = inAge;
        newInst.Type = type;
        newInst.Normal = new Vector3(0, 1, 0);
        newInst.Scale = scale;
        newInst._bBlocker = bBlocker;

        // Don't add the seed if outside the quadtree TreeBox...
        bool bSucceedsAgainstAABBCheck = _broadphase.TestAgainstAABB(newInst);
        if (bSucceedsAgainstAABBCheck)
        {
            // Add the seed if possible
            _broadphase.Insert(newInst);
            bool bSurvived = HandleOverlaps(newInst);
            return bSurvived ? newInst : null;
        }
        else
        {
            return null;
        }
        

        return null;
    }

    private bool HandleOverlaps(ProceduralFoliageInstance newInst)
    {
        return true;
        throw new System.NotImplementedException();
    }

    private void AgeSeeds()
    {
        
    }

    private void SpreadSeeds(List<ProceduralFoliageInstance> newSeeds)
    {
        
    }
    
    private void FlushPendingRemovals()
    {
        
    }

    public void CopyInstancesToTile(ProceduralFoliageTile toTile, Bounds localAABB, Matrix4x4 relativeMat, float tileOverlap)
    {
        List<ProceduralFoliageInstance> InstancesIncludingOverlap = new List<ProceduralFoliageInstance>();

        Bounds outerLocalAABB = new Bounds();
        outerLocalAABB.SetMinMax(localAABB.min, localAABB.max + Vector3.one * tileOverlap);

        GetInstancesInAABB(outerLocalAABB, InstancesIncludingOverlap);

        toTile.AddInstances(InstancesIncludingOverlap, relativeMat, localAABB);
    }

    private void AddInstances(List<ProceduralFoliageInstance> NewInstances, Matrix4x4 relativeMat, Bounds localAABB)
    {
        foreach (ProceduralFoliageInstance Inst in NewInstances)
        {
            Vector3 location = Inst.Location;
            float radius = Inst.GetMaxRadius();

            // Instances in InnerLocalAABB or on the border of the max sides of the AABB will be visible and instantiated by this tile
            // Instances outside of the InnerLocalAABB are only used for rejection purposes. This is needed for overlapping tiles
            // The actual instantiation of the object will be taken care of by a different tile
            bool bIsOutsideInnerLocalAABB = location.x + radius <= localAABB.min.x
                                                  || location.x - radius > localAABB.max.x
                                                  || location.y + radius <= localAABB.min.y
                                                  || location.y - radius > localAABB.max.y;

            Vector3 newLocation = relativeMat.MultiplyPoint(Inst.Location);
            // ProceduralFoliageInstance newInst = NewSeed(newLocation, Inst.Scale, Inst.Type, Inst.Age,
            //     bIsOutsideInnerLocalAABB); //TODO 마지막거가 false여야 추가됨.
            ProceduralFoliageInstance newInst = NewSeed(newLocation, Inst.Scale, Inst.Type, Inst.Age,
                false);
            if (newInst != null)
            {			
                _instancesSet.Add(newInst);
            }
        }

        FlushPendingRemovals();
    }

    private void GetInstancesInAABB(Bounds LocalAABB, List<ProceduralFoliageInstance> OutInstances, bool bFullyContainedOnly = true)
    {
        List<ProceduralFoliageInstance> instancesInAABB = new List<ProceduralFoliageInstance>();
        _broadphase.GetInstancesInBox(LocalAABB, instancesInAABB);
        
        OutInstances.Capacity = OutInstances.Count + instancesInAABB.Count;
        foreach (ProceduralFoliageInstance inst in instancesInAABB)
        {
            float rad = inst.GetMaxRadius();
            Vector3 location = inst.Location;
        
            if (!bFullyContainedOnly ||
                (location.x - rad >= LocalAABB.min.x && location.x + rad <= LocalAABB.max.x && location.y - rad >= LocalAABB.min.y && location.y + rad <= LocalAABB.max.y))
            {
                OutInstances.Add(inst);
            }
        }
        
        OutInstances.Sort((left, right) => //TODO 언리얼이랑 결과 같은지 확인 필요
        {
            return Mathf.Abs(left.Location.x - right.Location.x) < Mathf.Epsilon ? 
                left.Location.y.CompareTo(right.Location.y) : 
                left.Location.x.CompareTo(right.Location.x);
        });
        
        // Debug
        // OutInstances.AddRange(_instancesArray);
        // return;
    }

    public void ExtractDesiredInstances(List<DesiredFoliageInstance> OutInstances, Matrix4x4 WorldTM, Vector3 volumeLocation, float volumeMaxExtent, float HalfHeight, ProceduralFoliageVolume VolumeBodyInstance, bool bEmptyTileInfo)
    {
        InstancesToArray();
        
        // const FCollisionQueryParams Params(NAME_None, FCollisionQueryParams::GetUnknownStatId(), true);
        // FHitResult Hit;
        // 엥 이거 왜있는거야..? 사용도안하는데. 언리얼 PR 각 

        OutInstances.Capacity = _instancesSet.Count;
        foreach (ProceduralFoliageInstance Instance in _instancesArray)
        {
            Vector3 StartRay = Instance.Location + WorldTM.GetPosition();
            StartRay.y += HalfHeight;
            Vector3 EndRay = StartRay;
            EndRay.y -= (HalfHeight * 2.0f + 10.0f);	//add 10cm to bottom position of raycast. This is needed because volume is usually placed directly on geometry and then you get precision issues

            // Apply FoliageType's Falloff
            // TODO
            // Vector2 Position = new Vector2(StartRay.x, StartRay.z);
            // if (Instance.Type.DensityFalloff.IsInstanceFiltered(Position, ActorVolumeLocation, ActorVolumeMaxExtent))
            // {
            //     continue;
            // }
		
            DesiredFoliageInstance DesiredInst = new DesiredFoliageInstance(StartRay, EndRay, Instance.Type, Instance.GetMaxRadius());
            DesiredInst.Rotation = Instance.Rotation;
            // DesiredInst.ProceduralGuid = ProceduralGuid;
            DesiredInst.Age = Instance.Age;
            DesiredInst.ProceduralVolumeBodyInstance = VolumeBodyInstance;
            // DesiredInst.PlacementMode = EFoliagePlacementMode::Procedural;
            
            OutInstances.Add(DesiredInst);
        }

        if (bEmptyTileInfo)
        {
            //TODO
            // Empty();
        }
    }
}

public struct ProceduralFoliageTileJob : IJob
{
    //TODO 굳이 Job을 하나더 만드는게 맞는지, Tile 자체를 Job으로 만드는게 맞는지 모르겠음.
    public ProceduralFoliageTile Tile
    {
        get;
        private set;
    }
    
    private ProceduralFoliageSpawner _foliageSpawner;
    private int _randomSeed;
    private int _maxNumSteps;

    public ProceduralFoliageTileJob(ProceduralFoliageTile tile, ProceduralFoliageSpawner foliageSpawner, int randomSeed, int maxNumSteps)
    {
        _foliageSpawner = foliageSpawner;
        _randomSeed = randomSeed;
        _maxNumSteps = maxNumSteps;
        Tile = tile;
    }

    public void Execute()
    {
        Tile.Simulate(_foliageSpawner, _randomSeed, _maxNumSteps);
    }
}