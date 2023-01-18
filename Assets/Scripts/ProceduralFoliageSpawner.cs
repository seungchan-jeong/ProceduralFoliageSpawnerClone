using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[CreateAssetMenu(fileName = "ProceduralFoliageSpawner", menuName = "Scriptable Object/ProceduralFoliageSpawner")]
public class ProceduralFoliageSpawner : ScriptableObject
{
    
    public int _randomSeed;

    public float _tileSize;

    public int _numUniqueTiles;

    public float _minimumQuadTreeSize;

    // private List<FoliageTypeObject> _foliageTypes;

    private ProceduralFoliageTile[] _precomputedTiles;

    private RandomStream _randomStream;
    
    [SerializeField]
    private List<FoliageTypeObject> _foliageTypeObjects; //TODO 이거 때문에 ScriptabelObject로 만들어야할 듯?
    
    public void Simulate(int numSteps = -1)
    {
        _randomStream = new RandomStream(_randomSeed);
        CreateProceduralFoliageInstances();

        /*
        //Job에서 Reference Type 참조가 불가능해서 임시로 주석 처리
        
        ProceduralFoliageTileJob[] tileJobs = new ProceduralFoliageTileJob[_numUniqueTiles];
        NativeArray<JobHandle> tileJobHandles = new NativeArray<JobHandle>(_numUniqueTiles, Allocator.TempJob); //TODO Temp Job으로 해도되나? 4 frame 안에 끝나나?
        for (int i = 0; i < _numUniqueTiles; i++)
        {
            ProceduralFoliageTile newTile = new ProceduralFoliageTile();
            int randomNumber = GetRandomNumber();

            ProceduralFoliageTileJob job = new ProceduralFoliageTileJob(newTile, this, randomNumber, numSteps);
            tileJobs[i] = job;
            tileJobHandles[i] = job.Schedule();
        }
        
        JobHandle.CompleteAll(tileJobHandles);
        tileJobHandles.Dispose();
        _precomputedTiles = tileJobs.Select(job => job.Tile).ToArray(); //TODO Job 내부의 Tile을 이렇게 가져오는게 맞나?
        */
        
        _precomputedTiles = new ProceduralFoliageTile[_numUniqueTiles];
        for (int i = 0; i < _numUniqueTiles; i++)
        {
            ProceduralFoliageTile newTile = new ProceduralFoliageTile();
            int randomNumber = GetRandomNumber();
            
            newTile.Simulate(this, randomNumber, numSteps);
            _precomputedTiles[i] = newTile;
        }
    }

    public List<FoliageTypeObject> GetFoliageTypes()
    {
        return _foliageTypeObjects;
    }
    
    private void CreateProceduralFoliageInstances()
    {
        //TODO Refresh the instances contained in the type objects
        foreach (FoliageTypeObject foliageTypeObject in _foliageTypeObjects)
        {
            foliageTypeObject.RefreshInstance();
        }
    }

    private int GetRandomNumber()
    {
        return _randomStream.RandRange(int.MinValue+1, int.MaxValue-1);
    }

    public ProceduralFoliageTile GetRandomTile(int tileLayoutBottomLeftX, int tileLayoutBottomLeftZ)
    {
        if (_precomputedTiles.Length != 0) //TODO Length를 쓰는게 맞는지 모르겠음. (1) _pre..를 List로 바꾸거나 (2) Array로 가되 validation check를 해야할 듯? 
        {
            // Random stream to use as a hash function
            RandomStream HashStream = new RandomStream();	
		
            HashStream.Initialize(tileLayoutBottomLeftX);
            double XRand = HashStream.Rand();
		
            HashStream.Initialize(tileLayoutBottomLeftZ);
            double YRand = HashStream.Rand();

            const int RAND_MAX = 0x7fff; //32767 (stdlib.h)
            int RandomNumber = (int)(RAND_MAX * XRand / (YRand + 0.01));
            int Idx = Mathf.Clamp(RandomNumber % _precomputedTiles.Length, 0, _precomputedTiles.Length - 1);
            return _precomputedTiles[Idx];
        }

        return null;
    }

    public ProceduralFoliageTile CreateTempTile()
    {
        ProceduralFoliageTile tempTile = new ProceduralFoliageTile();
        tempTile.InitSimulation(this, 0);
        
        return tempTile;
    }
}
