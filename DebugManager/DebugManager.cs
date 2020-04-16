using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : Singleton<DebugManager>
{
    protected override void Awake()
    {
        base.Awake();

        Debug.unityLogger.logEnabled = Debug.isDebugBuild;
    }

}
