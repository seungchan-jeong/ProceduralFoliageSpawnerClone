using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageDummyActor : FoliageImpl
{
    private List<GameObject> ActorInstances = new List<GameObject>();
    public void AddInstance(FoliageInstance NewInstance)
    {
        //TODO
        // if (ActorClass == nullptr)
        // {
        //     return nullptr;
        // }
        //
        // AInstancedFoliageActor* IFA = GetIFA();
        //
        // FEditorScriptExecutionGuard ScriptGuard;
        // FActorSpawnParameters SpawnParameters;
        // SpawnParameters.ObjectFlags = RF_Transactional;
        // SpawnParameters.bHideFromSceneOutliner = true;
        // SpawnParameters.bCreateActorPackage = false; // No OFPA because that would generate tons of files
        // SpawnParameters.OverrideLevel = IFA->GetLevel();
        // AActor* NewActor = IFA->GetWorld()->SpawnActor(ActorClass, nullptr, nullptr, SpawnParameters);
        // if (NewActor)
        // {
        //     NewActor->SetActorTransform(Instance.GetInstanceWorldTransform());
        //     FFoliageHelper::SetIsOwnedByFoliage(NewActor);
        // }
        // return NewActor;
    }

    public void Refresh(bool bAsync, bool bForce)
    {
        return;
        throw new System.NotImplementedException();
    }
}
