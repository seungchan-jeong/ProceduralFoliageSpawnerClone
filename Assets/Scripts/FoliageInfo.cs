using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageInfo
{
    /*
     * 1. FoliageInfo를 어디서 만드는가? 
     * 2. FoliageInfo의 FoliageImpl.ActorClass는 어디서 넣어주는가?
     *  -> 'FoliageInfo' 는 UFoliageType 으로 부터 생성된다. '생성' 될 때는 ActorClass는 null
     *  -> 'FoliageInfo'의 Implementation (FoliageImpl) 은 ???
     *  -> FoliageImpl의 ActorClass 는 FoliageImpl::PreAddInstances 에 의해서 초기화 됨.
     *      -> FoliageType을 인자로 받고
     *      -> FoliageType을 FoliageType_Actor로 Cast한 후
     *      -> FoliageType_Actor->ActorClass로 초기화. 
     */
    
    //TODO
    private FoliageImpl Implementation = new FoliageDummyActor();
    
    public void AddInstances(FoliageType foliageSettings, List<FoliageInstance> value)
    {
        //TODO
        foreach (FoliageInstance foliageInstance in value)
        {
            // Implementation.AddInstance(foliageInstance);
            
            // Debug
            // Debug.Log(foliageInstance.Location);
            // continue;
            
            Matrix4x4 TM = foliageInstance.GetInstanceWorldTransform();
            
            GameObject instantiatedFoliage = GameObject.Instantiate(foliageSettings.ActorClass, TM.GetPosition(), TM.rotation);
            instantiatedFoliage.transform.localScale = TM.lossyScale;
        }
        
        // AddInstancesImpl(foliageSettings, InNewInstances, [](FFoliageImpl* Impl, AInstancedFoliageActor* LocalIFA, const FFoliageInstance& LocalInstance) { Impl->AddInstance(LocalInstance); });
    
    }

    public void Refresh(bool bAsync, bool bForce)
    {
        return;
        throw new System.NotImplementedException();
    }
}
