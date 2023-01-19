using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface FoliageImpl
{
    void AddInstance(FoliageInstance NewInstance);
    void Refresh(bool bAsync, bool bForce);
}
