using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralFoliageComponent : MonoBehaviour
{
    public bool bAllowLandscape;
    public bool bAllowStaticMesh;

    [SerializeField]
    private ProceduralFoliageSpawner _proceduralFoliageSpawner;
    
    public void ResimulateProceduralFoliage()
    {
        // Debug.Log("Resimulate");
        
        ResimulateProceduralFoliage((List<DesiredFoliageInstance> DesiredFoliageInstances) =>
        {
            FoliagePaintingGeometryFilter foliagePaintingGeometryFilter = new FoliagePaintingGeometryFilter(true, true);
            EdModeFoliage.AddInstances(DesiredFoliageInstances, foliagePaintingGeometryFilter);
        });
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
	    if (_proceduralFoliageSpawner != null)
	    {
		    _proceduralFoliageSpawner.Simulate();
		    
		    // TODO
		    /*
		     *
		     *	TArray<TFuture< TArray<FDesiredFoliageInstance>* >> Futures;
				for (int32 X = 0; X < TileLayout.NumTilesX; ++X)
				{
					for (int32 Y = 0; Y < TileLayout.NumTilesY; ++Y)
					{
						// We have to get the simulated tiles and create new ones to build on main thread
						const UProceduralFoliageTile* Tile = FoliageSpawner->GetRandomTile(X + TileLayout.BottomLeftX, Y + TileLayout.BottomLeftY);
						if (Tile == nullptr)	
						{
							// Simulation was either canceled or failed
							return false;
						}

						// From the pool of simulated tiles, pick the neighbors of this tile
						const UProceduralFoliageTile* RightTile = (X + 1 < TileLayout.NumTilesX) ? FoliageSpawner->GetRandomTile(X + TileLayout.BottomLeftX + 1, Y + TileLayout.BottomLeftY) : nullptr;
						const UProceduralFoliageTile* TopTile = (Y + 1 < TileLayout.NumTilesY) ? FoliageSpawner->GetRandomTile(X + TileLayout.BottomLeftX, Y + TileLayout.BottomLeftY + 1) : nullptr;
						const UProceduralFoliageTile* TopRightTile = (RightTile && TopTile) ? FoliageSpawner->GetRandomTile(X + TileLayout.BottomLeftX + 1, Y + TileLayout.BottomLeftY + 1) : nullptr;

						// Create a temp tile that will contain the composite contents of the tile after accounting for overlap
						UProceduralFoliageTile* CompositeTile = FoliageSpawner->CreateTempTile();

						Futures.Add(Async(EAsyncExecution::ThreadPool, [=]()
						{
							if (LastCancelPtr->GetValue() != LastCanelInit)
							{
								// The counter has changed since we began, meaning the user canceled the operation
								return new TArray<FDesiredFoliageInstance>();
							}

							//@todo proc foliage: Determine the composite contents of the tile (including overlaps) without copying everything to a temp tile

							// Copy the base tile contents
							const FBox2D BaseTile = GetTileRegion(X, Y, TileSize, TileOverlap);
							Tile->CopyInstancesToTile(CompositeTile, BaseTile, FTransform::Identity, TileOverlap);


							if (RightTile)
							{
								// Add instances from the right overlapping tile
								const FBox2D RightBox(FVector2D(0.f, BaseTile.Min.Y), FVector2D(TileOverlap, BaseTile.Max.Y));
								const FTransform RightTM(FVector(TileSize, 0.f, 0.f));
								RightTile->CopyInstancesToTile(CompositeTile, RightBox, RightTM, TileOverlap);
							}

							if (TopTile)
							{
								// Add instances from the top overlapping tile
								const FBox2D TopBox(FVector2D(BaseTile.Min.X, -TileOverlap), FVector2D(BaseTile.Max.X, TileOverlap));
								const FTransform TopTM(FVector(0.f, TileSize, 0.f));
								TopTile->CopyInstancesToTile(CompositeTile, TopBox, TopTM, TileOverlap);
							}

							if (TopRightTile)
							{
								// Add instances from the top-right overlapping tile
								const FBox2D TopRightBox(FVector2D(-TileOverlap, -TileOverlap), FVector2D(TileOverlap, TileOverlap));
								const FTransform TopRightTM(FVector(TileSize, TileSize, 0.f));
								TopRightTile->CopyInstancesToTile(CompositeTile, TopRightBox, TopRightTM, TileOverlap);
							}

							const FVector OrientedOffset = FVector(X, Y, 0.f) * TileSize;
							const FTransform TileTM(OrientedOffset + WorldPosition);

							TArray<FDesiredFoliageInstance>* DesiredInstances = new TArray<FDesiredFoliageInstance>();
							CompositeTile->ExtractDesiredInstances(*DesiredInstances, TileTM, ActorVolumeLocation, ActorVolumeMaxExtent, ProceduralGuid, TileLayout.HalfHeight, BoundsBodyInstance, true);

							return DesiredInstances;
							
						})
						);
					}
				}

				const FText StatusMessage = LOCTEXT("PlaceProceduralFoliage", "Placing ProceduralFoliage...");
				const FText CancelMessage = LOCTEXT("PlaceProceduralFoliageCancel", "Canceling ProceduralFoliage...");
				GWarn->BeginSlowTask(StatusMessage, true, true);


				int32 FutureIdx = 0;
				bool bCancelled = false;
				uint32 OutInstanceGrowth = 0;
				for (int X = 0; X < TileLayout.NumTilesX; ++X)
				{
					for (int Y = 0; Y < TileLayout.NumTilesY; ++Y)
					{
						bool bFirstTime = true;
						while (Futures[FutureIdx].WaitFor(FTimespan::FromMilliseconds(100.0)) == false || bFirstTime)
						{
							if (GWarn->ReceivedUserCancel() && bCancelled == false)
							{
								// Increment the thread-safe counter. Tiles compare against the original count and cancel if different.
								LastCancel.Increment();
								bCancelled = true;
							}

							if (bCancelled)
							{
								GWarn->StatusUpdate(Y + X * TileLayout.NumTilesY, TileLayout.NumTilesX * TileLayout.NumTilesY, CancelMessage);
							}
							else
							{
								GWarn->StatusUpdate(Y + X * TileLayout.NumTilesY, TileLayout.NumTilesX * TileLayout.NumTilesY, StatusMessage);
							}

							bFirstTime = false;
						}

						TArray<FDesiredFoliageInstance>* DesiredInstances = Futures[FutureIdx++].Get();
						OutInstanceGrowth += DesiredInstances->Num();
					}
				}

				OutInstances.Reserve(OutInstances.Num() + OutInstanceGrowth);
				FutureIdx = 0;
				for (int X = 0; X < TileLayout.NumTilesX; ++X)
				{
					for (int Y = 0; Y < TileLayout.NumTilesY; ++Y)
					{
						TArray<FDesiredFoliageInstance>* DesiredInstances = Futures[FutureIdx++].Get();
						OutInstances.Append(*DesiredInstances);
						delete DesiredInstances;
					}
				}

				GWarn->EndSlowTask();

				return !bCancelled;
		
		     */
		    return true;
	    }
#endif
        return false;
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
