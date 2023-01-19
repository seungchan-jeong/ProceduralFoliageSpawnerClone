using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralFoliageComponent : MonoBehaviour
{
    public bool bAllowLandscape;
    public bool bAllowStaticMesh;

    public float TileOverlap = 0.0f;

    [SerializeField]
    private ProceduralFoliageSpawner _proceduralFoliageSpawner;
    
    public void ResimulateProceduralFoliage()
    {
        ResimulateProceduralFoliage((List<DesiredFoliageInstance> DesiredFoliageInstances) =>
        {
            FoliagePaintingGeometryFilter foliagePaintingGeometryFilter = new FoliagePaintingGeometryFilter(true, true);
            EdModeFoliage.AddInstances(DesiredFoliageInstances, foliagePaintingGeometryFilter, true);
        });
    }

    public void GetTileLayout(ref TileLayout outTileLayout)
    {
	    Bounds bounds = GetBounds();
	    if (bounds != default)
	    {
		    Vector3 minPosition = bounds.min + Vector3.one * TileOverlap;
		    outTileLayout.BottomLeftX = Mathf.FloorToInt(minPosition.x / _proceduralFoliageSpawner._tileSize);
		    outTileLayout.BottomLeftZ = Mathf.FloorToInt(minPosition.z / _proceduralFoliageSpawner._tileSize);

		    // Determine the total number of tiles along each active axis
		    Vector3 maxPosition = bounds.max - Vector3.one * TileOverlap;
		    int maxXIdx = Mathf.FloorToInt(maxPosition.x / _proceduralFoliageSpawner._tileSize);
		    int maxZIdx = Mathf.FloorToInt(maxPosition.z / _proceduralFoliageSpawner._tileSize);

		    outTileLayout.NumTilesX = (maxXIdx - outTileLayout.BottomLeftX) + 1;
		    outTileLayout.NumTilesZ = (maxZIdx - outTileLayout.BottomLeftZ) + 1;

		    outTileLayout.HalfHeight = bounds.extents.y;
	    }
    }

    public Bounds GetBounds()
    {
	    Collider collider = GetComponent<Collider>();
	    if (collider != null)
	    {
		    return collider.bounds;
	    }

	    return default;
    }

    public bool ResimulateProceduralFoliage(Action<List<DesiredFoliageInstance>> AddInstancesFunc)
    {
        
#if UNITY_EDITOR
	    if (_proceduralFoliageSpawner == null)
	    {
		    _proceduralFoliageSpawner = new ProceduralFoliageSpawner();
	    }
	    
	    List<DesiredFoliageInstance> desiredFoliageInstances = new List<DesiredFoliageInstance>();
		if (GenerateProceduralContent(desiredFoliageInstances))
		{
			if (desiredFoliageInstances.Count > 0)
			{
				{
					// Remove old foliage instances
					// FScopeChangeDataLayerEditorContext ScopeContext(GetWorld(), LastSimulationDataLayer);
					RemoveProceduralContent(false);
				}

				{
					// Add new foliage instances
					// FScopeChangeDataLayerEditorContext ScopeContext(GetWorld(), DataLayer);
					AddInstancesFunc(desiredFoliageInstances);
					// LastSimulationDataLayer = DataLayer;
				}
			}

			return true;
		}
#endif
	    return false;
        
        
       
    }

    private bool GenerateProceduralContent(List<DesiredFoliageInstance> desiredFoliageInstances)
    {
#if UNITY_EDITOR
        return ExecuteSimulation(desiredFoliageInstances);
#endif
        return false;
    }

    private bool ExecuteSimulation(List<DesiredFoliageInstance> desiredFoliageInstances)
    {
#if UNITY_EDITOR
	    ProceduralFoliageVolume BoundsBodyInstance = GetBoundsBodyInstance();
	    if (_proceduralFoliageSpawner != null)
	    {
		    float tileSize = _proceduralFoliageSpawner._tileSize;
		    Vector3 worldPosition = GetBoundLeftNearCorner();
		    Bounds volumeBound = GetBounds();
		    Vector3 volumeLocation = volumeBound.center;
		    float volumeMaxExtent = Mathf.Max(volumeBound.extents.x, volumeBound.extents.z);
		    TileLayout tileLayout = new TileLayout();
		    GetTileLayout(ref tileLayout);
		    
		    _proceduralFoliageSpawner.Simulate();
		    
		    // TArray<TFuture< TArray<FDesiredFoliageInstance>* >> Futures;
		    for (int X = 0; X < tileLayout.NumTilesX; ++X)
		    {
			    for (int Z = 0; Z < tileLayout.NumTilesZ; ++Z)
			    {
				    // We have to get the simulated tiles and create new ones to build on main thread
				    ProceduralFoliageTile Tile = _proceduralFoliageSpawner.GetRandomTile(X + tileLayout.BottomLeftX, Z + tileLayout.BottomLeftZ);
				    if (Tile == null)
				    {
					    // Simulation was either canceled or failed
					    return false;
				    }

				    // From the pool of simulated tiles, pick the neighbors of this tile
				    ProceduralFoliageTile RightTile = (X + 1 < tileLayout.NumTilesX)
					    ? _proceduralFoliageSpawner.GetRandomTile(X + tileLayout.BottomLeftX + 1, Z + tileLayout.BottomLeftZ)
					    : null;
				    ProceduralFoliageTile TopTile = (Z + 1 < tileLayout.NumTilesZ)
					    ? _proceduralFoliageSpawner.GetRandomTile(X + tileLayout.BottomLeftX, Z + tileLayout.BottomLeftZ + 1)
					    : null;
				    ProceduralFoliageTile TopRightTile = (RightTile != null && TopTile != null)
					    ? _proceduralFoliageSpawner.GetRandomTile(X + tileLayout.BottomLeftX + 1, Z + tileLayout.BottomLeftZ + 1)
					    : null;

				    // Create a temp tile that will contain the composite contents of the tile after accounting for overlap
				    ProceduralFoliageTile CompositeTile = _proceduralFoliageSpawner.CreateTempTile();

				    {
					    //Thread 시작
					    Bounds baseTile = GetTileRegion(X, Z, tileSize, TileOverlap); //Unity는 따로 2D Bounds가 없는 것 같아서. 그냥 Bounds 사용
					    Tile.CopyInstancesToTile(CompositeTile, baseTile, Matrix4x4.identity, TileOverlap);
					    
					    // 이웃 Tile과 겹치는 Foliage에 대한 처리. 
					    if (RightTile != null)
					    {
						    // Add instances from the right overlapping tile
						    Bounds rightBox = new Bounds(new Vector3(0.0f, 0.0f, baseTile.min.z), new Vector3(TileOverlap, 0.0f, baseTile.max.z));
						    Matrix4x4 rightTM = Matrix4x4.Translate(new Vector3(tileSize, 0.0f, 0.0f)); //TODO Translate로 추정해서 넣었음 확인 필요 
						    RightTile.CopyInstancesToTile(CompositeTile, rightBox, rightTM, TileOverlap);
					    }
					    
					    if (TopTile != null)
					    {
						    // Add instances from the top overlapping tile
						    Bounds topBox = new Bounds(new Vector3(baseTile.min.x, 0.0f, -TileOverlap), new Vector3(baseTile.max.x, 0.0f, TileOverlap));
						    Matrix4x4 topTM = Matrix4x4.Translate(new Vector3(0.0f, 0.0f, tileSize));
						    TopTile.CopyInstancesToTile(CompositeTile, topBox, topTM, TileOverlap);
					    }
					    
					    if (TopRightTile != null)
					    {
						    // Add instances from the top-right overlapping tile
						    Bounds topRightBox = new Bounds(new Vector3(-TileOverlap, 0.0f, -TileOverlap), new Vector3(TileOverlap, 0.0f, TileOverlap));
						    Matrix4x4 topRightTM = Matrix4x4.Translate(new Vector3(tileSize, 0.0f, tileSize)); 
						    TopRightTile.CopyInstancesToTile(CompositeTile, topRightBox, topRightTM, TileOverlap);
					    }

					    Vector3 orientedOffset = new Vector3(X, 0.0f, Z) * tileSize;
					    Matrix4x4 tileTM = Matrix4x4.Translate(orientedOffset + worldPosition);

					    List<DesiredFoliageInstance> desiredInstances = new List<DesiredFoliageInstance>();
					    CompositeTile.ExtractDesiredInstances(desiredInstances, tileTM, volumeLocation, volumeMaxExtent, tileLayout.HalfHeight, BoundsBodyInstance,true);
					    
					    desiredFoliageInstances.AddRange(desiredInstances);
				    }
				    
			    }
		    }
			
		    // int FutureIdx = 0;
		    // int OutInstanceGrowth = 0;
		    // for (int X = 0; X < tileLayout.NumTilesX; ++X)
		    // {
			   //  for (int Z = 0; Z < tileLayout.NumTilesZ; ++Z)
			   //  {
				  //   bool bFirstTime = true;
				  //   // while (Futures[FutureIdx].WaitFor(FTimespan::FromMilliseconds(100.0)) == false || bFirstTime)
				  //   // {
					 //   //
				  //   // }
				  //   List<DesiredFoliageInstance> TileOutput; /* = Futures[FutureIdx++].Get();*/
				  //   OutInstanceGrowth += TileOutput.Count;
			   //  }
		    // }
		    //
		    // desiredFoliageInstances.Capacity = desiredFoliageInstances.Count + OutInstanceGrowth;
		    // FutureIdx = 0;
		    // for (int X = 0; X < tileLayout.NumTilesX; ++X)
		    // {
			   //  for (int Z = 0; Z < tileLayout.NumTilesZ; ++Z)
			   //  {
				  //   List<DesiredFoliageInstance> DesiredInstances; /*= Futures[FutureIdx++].Get()*/
				  //   desiredFoliageInstances.AddRange(DesiredInstances);
			   //  }
		    // }
		    return true;
	    }
#endif
        return false;
    }

    private Vector3 GetBoundLeftNearCorner() //CUSTOM
    {
	    Bounds bounds = GetBounds();
	    if (bounds == null)
	    {
		    Debug.LogError("Bound is null");
	    }

	    return bounds.min + Vector3.up * bounds.extents.y;
    }

    private ProceduralFoliageVolume GetBoundsBodyInstance()
    {
	    //TODO
	    return GetComponent<ProceduralFoliageVolume>();
    }

    private Bounds GetTileRegion(int X, int Z, float tileSize, float tileOverlap)
    {
	    float bottomLeftX = X == 0 ? -tileOverlap : tileOverlap;
	    float bottomLeftZ = Z == 0 ? -tileOverlap : tileOverlap;
	    
	    Vector3 min = new Vector3(X, 0.0f, Z);
	    Vector3 max = new Vector3(tileSize + tileOverlap, 0.0f, tileSize + tileOverlap);

	    Bounds outBounds = new Bounds();
	    outBounds.SetMinMax(min, max);
	    return outBounds;
    }

    private bool GenerateDummyContent(List<DesiredFoliageInstance> desiredFoliageInstances)
    {
	    FoliageType type = new FoliageType();
        
	    DesiredFoliageInstance dummy01 = new DesiredFoliageInstance();
	    dummy01.StartTrace = new Vector3(0.0f, 5.0f, 0.0f);
	    dummy01.EndTrace = new Vector3(0.0f, -5.0f, 0.0f);
	    dummy01.Rotation = Quaternion.identity;
	    dummy01.TraceRadius = 1.0f;
	    dummy01.FoliageType = type;
	    dummy01.ProceduralVolumeBodyInstance = GetComponent<ProceduralFoliageVolume>();
        
	    DesiredFoliageInstance dummy02 = new DesiredFoliageInstance();
	    dummy02.StartTrace = new Vector3(22.0f, 5.0f, 2.0f);
	    dummy02.EndTrace = new Vector3(2.0f, -5.0f, 2.0f);
	    dummy02.Rotation = Quaternion.identity;
	    dummy02.TraceRadius = 1.0f;
	    dummy02.FoliageType = type;
	    dummy02.ProceduralVolumeBodyInstance = GetComponent<ProceduralFoliageVolume>();
	    
	    return true;
    }

    private void RemoveProceduralContent(bool _)
    {
	    
    }
    
    
}

public struct TileLayout
{
	// The X coordinate (in whole tiles) of the bottom-left-most active tile
	public int BottomLeftX;

	// The Y coordinate (in whole tiles) of the bottom-left-most active tile
	public int BottomLeftZ;

	// The total number of active tiles along the x-axis
	public int NumTilesX;

	// The total number of active tiles along the y-axis
	public int NumTilesZ;
	
	public float HalfHeight;
};
