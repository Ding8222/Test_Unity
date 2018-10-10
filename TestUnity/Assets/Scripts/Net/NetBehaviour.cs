using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetBehaviour : MonoBehaviour {

    bool bInit = false;
    void Awake()
    {
        if(!bInit)
        {
            bInit = true;
            NetCore.Init();
            NetSender.Init();
            NetReceiver.Init();
            DontDestroyOnLoad(gameObject);
        }
    }

	// Update is called once per frame
	void Update ()
    {
        NetCore.Dispatch();
    }
}
