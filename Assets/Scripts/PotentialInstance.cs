using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotentialInstance
{
    public Vector3 HitLocation;
    public Vector3 HitNormal;
    public GameObject HitComponent;
    public float HitWeight;
    public DesiredFoliageInstance DesiredInstance;

    public bool PlaceInstance(FoliageType Settings, ref FoliageInstance inst, bool bSkipCollision = false)
    {
        inst.DrawScale3D = Vector3.one * Settings.GetScaleForAge(DesiredInstance.Age);
        
        RandomStream LocalRandomStream = new RandomStream(FoliagePlacementUtil.GetRandomSeedForPosition(inst.Location)); //TODO 이거 언리얼 버그인듯? inst.Location에 할당하기 전에 inst.Location을 사용함. 0, 0 만 들어갈 거 같은데 
        inst.ZOffset = Settings._zOffset.Interpolate((float)LocalRandomStream.Rand());

        inst.Location = HitLocation;
        
        if (Settings._randomYaw)
        {
            inst.Rotation = DesiredInstance.Rotation;
        }
        else
        {
            inst.Rotation = Quaternion.Euler(DesiredInstance.Rotation.eulerAngles + Vector3.up * (Random.value * 360.0f));
            // inst.Flags |= 
        }

        if (Settings.AlignToNormal)
        {
            inst.AlignToNormal(HitNormal, Settings.AlignMaxAngle);
        }
        
        if (Mathf.Abs(inst.ZOffset) > Mathf.Epsilon)
        {
            inst.Location = inst.GetInstanceWorldTransform().MultiplyPoint(new Vector3(0, 0, inst.ZOffset));
        }
        // ?
        // UModelComponent* ModelComponent = Cast<UModelComponent>(HitComponent);
        // if (ModelComponent)
        // {
        //     ABrush* BrushActor = ModelComponent->GetModel()->FindBrush((FVector3f)HitLocation);
        //     if (BrushActor)
        //     {
        //         HitComponent = BrushActor->GetBrushComponent();
        //     }
        // }
        
        return bSkipCollision || InstancedFoliageActor.CheckCollisionWithWorld(Settings, inst, HitNormal, HitLocation, HitComponent);
    }

    public PotentialInstance(Vector3 hitLocation, Vector3 hitNormal, GameObject hitComponent, float hitWeight, DesiredFoliageInstance desiredInstance, bool bSkipCollision = false)
    {
        HitLocation = hitLocation;
        HitNormal = hitNormal;
        HitComponent = hitComponent;
        HitWeight = hitWeight;
        DesiredInstance = desiredInstance;
    }
}
